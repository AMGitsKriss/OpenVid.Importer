using Database;
using Database.Models;
using Microsoft.Extensions.Options;
using System.IO;
using System.Linq;
using System;
using OpenVid.Importer.Tasks.Thumbnails;
using OpenVid.Importer.Tasks.Metadata;
using OpenVid.Importer.Models;
using OpenVid.Importer.Helpers;
using OpenVid.Importer.Tasks.Encoder;
using OpenVid.Importer.Entities;

namespace OpenVid.Importer
{
    public class HandbrakeEncoder
    {
        private readonly IVideoRepository _repository;
        private readonly IEncoder _encoder;
        private readonly IFindMetadata _metadata;
        private readonly IGenerateThumbnails _generateThumbnails;
        private readonly CatalogImportOptions _configuration;

        public HandbrakeEncoder(IVideoRepository repository, IEncoder encoder, IFindMetadata metadata, IGenerateThumbnails generateThumbnails, IOptions<CatalogImportOptions> configuration)
        {
            _repository = repository;
            _encoder = encoder;
            _metadata = metadata;
            _generateThumbnails = generateThumbnails;
            _configuration = configuration.Value;
        }

        public void Run(EncodeJobContext jobContext)
        {
            FileHelpers.TouchDirectory(jobContext.FolderTranscoded);

            var srcMetaData = _metadata.Execute(jobContext.FileQueued);
            jobContext.SourceWidth = srcMetaData.Width;
            jobContext.SourceHeight = srcMetaData.Height;

            // TODO - Kaichou wa Maid-sama fails quietly
            _encoder.Execute(jobContext);

            // If getting metadata works, then we know the file exists
            var metadata = _metadata.Execute(jobContext.FileTranscoded);

            // Remove the old file
            if (!_repository.IsFileStillNeeded(jobContext.QueueItem.VideoId))
            {
                try
                {
                    File.Delete(jobContext.FileQueued);
                }
                catch (Exception ex)
                {
                    var msg = ex.Message;
                }
            }

            CreateThumbnail(jobContext);

            var jobsWithThisQuality = _repository.GetSimilarEncodeJobs(jobContext.QueueItem);


            foreach (var job in jobsWithThisQuality.Select(j => new EncodeJobContext(_configuration, j)))
            {
                // Write the new source entry
                if (job.QueueItem.PlaybackFormat == "mp4")
                {
                    SaveMp4Video(job, metadata);
                }
                else if (job.QueueItem.PlaybackFormat == "dash")
                {
                    CopyDashVideoAwaitingPackager(job);
                    AddToSegmentQueue(job);
                }

                // Mark as done before looping
                job.QueueItem.IsDone = true;
                _repository.SaveEncodeJob(job.QueueItem);
            }
            File.Delete(jobContext.FileTranscoded);
        }


        private void CreateThumbnail(EncodeJobContext jobContext)
        {
            string thumbSubFolder = jobContext.QueueItem.VideoId.ToString().PadLeft(2, '0').Substring(0, 2);
            string thumbDirectory = Path.Combine(_configuration.BucketDirectory, "thumbnail", thumbSubFolder);
            FileHelpers.TouchDirectory(thumbDirectory);

            string thumbPath = Path.Combine(thumbDirectory, $"{jobContext.QueueItem.VideoId.ToString().PadLeft(2, '0')}.jpg");
            if (!File.Exists(thumbPath))
                _generateThumbnails.Execute(jobContext.FileTranscoded, thumbPath);
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
            string vidSubFolder = md5.Substring(0, 2);
            string videoDirectory = Path.Combine(_configuration.BucketDirectory, "video", vidSubFolder);
            FileHelpers.TouchDirectory(videoDirectory);
            string videoBucketDirectory = Path.Combine(videoDirectory, $"{md5}{jobContext.OutputExtension}");
            File.Copy(jobContext.FileTranscoded, videoBucketDirectory);
        }

        private void CopyDashVideoAwaitingPackager(EncodeJobContext jobContext)
        {
            FileHelpers.TouchDirectory(jobContext.FolderPackager);
            File.Copy(jobContext.FileTranscoded, jobContext.FilePackager);
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