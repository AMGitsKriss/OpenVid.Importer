using Database;
using Database.Models;
using Microsoft.Extensions.Options;
using System.IO;
using System.Linq;
using System;
using OpenVid.Importer.Tasks.Thumbnails;
using OpenVid.Importer.Helpers;
using Common;
using Common.Entities;
using Handbrake.Handler;
using Ffmpeg.Handler;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using System.Security.Cryptography;
using Serilog;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace OpenVid.Importer
{
    public class HandbrakeHandler
    {
        private readonly ILogger _logger;
        private readonly IVideoRepository _repository;
        private readonly IEncoder _encoder;
        private readonly MetadataExtractor _metadata;
        private readonly IGenerateThumbnails _generateThumbnails;
        private readonly CatalogImportOptions _configuration;

        public HandbrakeHandler(ILogger logger, IVideoRepository repository, IEncoder encoder, MetadataExtractor metadata, IGenerateThumbnails generateThumbnails, IOptions<CatalogImportOptions> configuration)
        {
            _logger = logger;
            _repository = repository;
            _encoder = encoder;
            _metadata = metadata;
            _generateThumbnails = generateThumbnails;
            _configuration = configuration.Value;
        }

        public async Task<bool> Run(EncodeJobContext jobContext)
        {
            Console.WriteLine($"Step 2 - Transcoding {jobContext.FileIngest} to {jobContext.FileTranscoded}.");

            FileHelpers.TouchDirectory(jobContext.FolderTranscoded);

            var mediaInfo = await _metadata.Extract(jobContext.FileQueued);
            var srcMetaData = _metadata.GetMetadata(mediaInfo);
            jobContext.SourceWidth = srcMetaData.Width;
            jobContext.SourceHeight = srcMetaData.Height;

            var startTime = DateTime.Now;
            var inputSize = new System.IO.FileInfo(jobContext.FileQueued).Length;
            var inputMPixels = srcMetaData.Width * srcMetaData.Height;

            // TODO - Kaichou wa Maid-sama fails quietly
            if (!await _encoder.Execute(jobContext))
                return false;

            // Metadata for the new transcoded video
            var transcodedMediaInfo = await _metadata.Extract(jobContext.FileTranscoded);
            var transcodedMetadata = _metadata.GetMetadata(transcodedMediaInfo);

            CreateThumbnail(jobContext, transcodedMetadata);

            // Mark as done before looping
            jobContext.QueueItem.IsDone = true;
            _repository.SaveEncodeJob(jobContext.QueueItem);
            SaveMp4Video(jobContext, transcodedMetadata);

            var jobsWithThisQuality = _repository.GetSimilarEncodeJobs(jobContext.QueueItem);

            foreach (var job in jobsWithThisQuality.Select(j => new EncodeJobContext(_configuration, j)))
            {
                // Write the new source entry
                if (job.QueueItem.PlaybackFormat == "mp4")
                {
                    SaveMp4Video(job, transcodedMetadata);
                }
                else if (job.QueueItem.PlaybackFormat == "dash")
                {
                    CopyDashVideoAwaitingPackager(job);
                    AddToSegmentQueue(job);
                }

                jobContext.QueueItem.IsDone = true;
                _repository.SaveEncodeJob(jobContext.QueueItem);
            }

            try
            {
                // Remove the old file
                var outputPath = FileHelpers.FileDirPath(_configuration.BucketDirectory, FileHelpers.GenerateHash(jobContext.FileTranscoded), jobContext.OutputExtension);
                if (!_repository.IsSourceFileStillNeeded(jobContext.QueueItem.VideoId, jobContext.QueueItem.Id)
                    && File.Exists(outputPath))
                {
                    FileSystem.DeleteFile(jobContext.FileQueued, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    //File.Delete(jobContext.FileQueued);
                }
                else if(!File.Exists(outputPath))
                {
                    _logger.Error($"The file {jobContext.InputFileName} was expected to be copied to {outputPath}, but it was not found in the destination. Deletion of WIP file skipped.");
                }
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
            }

            // We don't need the temp one anymore, we'e copied the temp file to another folder for segmenting
            var outputSize = new System.IO.FileInfo(jobContext.FileTranscoded).Length;
            var outputMPixels = transcodedMetadata.Width * transcodedMetadata.Height;
            var timeTaken = DateTime.Now - startTime;
            _logger.Information($"Video {jobContext.QueueItem.VideoId} | Duration: {transcodedMetadata.Duration} | Size: {inputSize} to {outputSize} | MPixels: {inputMPixels} to {outputMPixels} | Took: {timeTaken}");

            File.Delete(jobContext.FileTranscoded);

            return true;
        }


        private void CreateThumbnail(EncodeJobContext jobContext, VideoMetadata transcodedMetadata)
        {
            string thumbSubFolder = jobContext.QueueItem.VideoId.ToString().PadLeft(2, '0').Substring(0, 2);
            string thumbDirectory = Path.Combine(_configuration.BucketDirectory, "thumbnail", thumbSubFolder);
            FileHelpers.TouchDirectory(thumbDirectory);

            var start = 0d;
            if (transcodedMetadata.Duration.TotalMinutes > 1)
                start = 5 + transcodedMetadata.Duration.TotalMinutes;
            else
            {
                start = transcodedMetadata.Duration.TotalSeconds / 15d;
            }

            var thumbTimespan = TimeSpan.FromSeconds(start);

            string thumbPath = Path.Combine(thumbDirectory, $"{jobContext.QueueItem.VideoId.ToString().PadLeft(2, '0')}.jpg");
            if (!File.Exists(thumbPath))
                _generateThumbnails.Execute(jobContext.FileTranscoded, thumbPath, thumbTimespan);
        }

        private void SaveMp4Video(EncodeJobContext jobContext, VideoMetadata metadata)
        {
            var md5 = FileHelpers.GenerateHash(jobContext.FileTranscoded);
            var videoSource = new VideoSource()
            {
                VideoId = jobContext.QueueItem.VideoId,
                Md5 = md5,
                Width = metadata.Width,
                Height = metadata.Height,
                Size = new FileInfo(jobContext.FileTranscoded).Length,
                Extension = jobContext.OutputExtension.Replace(".", "")
            };
            _repository.SaveVideoSource(videoSource);

            // Move the new file to the bucket
            var videoBucketDirectory = FileHelpers.FileDirPath(_configuration.BucketDirectory, md5, jobContext.OutputExtension);
            File.Copy(jobContext.FileTranscoded, videoBucketDirectory);
        }

        private void CopyDashVideoAwaitingPackager(EncodeJobContext jobContext)
        {
            FileHelpers.TouchDirectory(jobContext.FolderPackager);
            File.Copy(jobContext.FileTranscoded, jobContext.FilePackager, true);
        }

        private void AddToSegmentQueue(EncodeJobContext jobContext)
        {
            var segmentJob = _repository.GetSegmentJobsForVideo(jobContext.QueueItem.VideoId)
                .Where(j => !j.IsDone && !j.IsReady).FirstOrDefault();

            if (segmentJob == null)
            {
                segmentJob = new VideoSegmentQueue()
                {
                    VideoId = jobContext.QueueItem.VideoId
                };
                _repository.SaveSegmentJob(segmentJob);
            }

            var job = new VideoSegmentQueueItem()
            {
                VideoId = jobContext.QueueItem.VideoId,
                VideoSegmentQueueId = segmentJob.Id,
                ArgStreamFolder = jobContext.QueueItem.MaxHeight.ToString(),
                ArgInputFile = jobContext.QueueItem.OutputDirectory,
                ArgInputFolder = jobContext.FolderRelativePackager,
                ArgStream = "video"
            };

            _repository.SaveSegmentItem(job);
        }

    }
}