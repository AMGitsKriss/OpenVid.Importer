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

namespace OpenVid.Importer
{
    public class EncoderContainer
    {
        private readonly IVideoRepository _repository;
        private readonly IEncoder _encoder;
        private readonly IFindMetadata _metadata;
        private readonly IGenerateThumbnails _generateThumbnails;
        private readonly CatalogImportOptions _configuration;

        public EncoderContainer(IVideoRepository repository, IEncoder encoder, IFindMetadata metadata, IGenerateThumbnails generateThumbnails, IOptions<CatalogImportOptions> configuration)
        {
            _repository = repository;
            _encoder = encoder;
            _metadata = metadata;
            _generateThumbnails = generateThumbnails;
            _configuration = configuration.Value;
        }

        public void Run(VideoEncodeQueue queueItem)
        {
            // TODO - Kaichou wa Maid-sama fails quietly
            _encoder.Execute(queueItem);

            // If getting metadata works, then we know the file exists
            var inputDir = Path.Combine(_configuration.ImportDirectory, "02_queued", queueItem.InputDirectory);
            var crunchedDir = Path.Combine(_configuration.ImportDirectory, "03_transcode_complete", queueItem.OutputDirectory);
            var metadata = _metadata.Execute(crunchedDir);

            // Remove the old file
            if (!_repository.IsFileStillNeeded(queueItem.VideoId))
            {
                try
                {
                    File.Delete(inputDir);
                }
                catch (Exception ex)
                {
                    var msg = ex.Message;
                }
            }

            CreateThumbnail(queueItem);

            var jobsWithThisQuality = _repository.GetSimilarEncodeJobs(queueItem);


            foreach (var job in jobsWithThisQuality)
            {
                // Write the new source entry
                if (job.PlaybackFormat == "mp4")
                {
                    SaveMp4Video(job, metadata);
                }
                else if (job.PlaybackFormat == "dash")
                {
                    CopyDashVideoAwaitingPackager(job);
                    AddToSegmentQueue(job);
                }

                // Mark as done before looping
                job.IsDone = true;
                _repository.SaveEncodeJob(job);
            }
            File.Delete(crunchedDir);
        }


        private void CreateThumbnail(VideoEncodeQueue queueItem)
        {
            string thumbSubFolder = queueItem.VideoId.ToString().PadLeft(2, '0').Substring(0, 2);
            string thumbDirectory = Path.Combine(_configuration.BucketDirectory, "thumbnail", thumbSubFolder);
            FileHelpers.TouchDirectory(thumbDirectory);

            string thumbPath = Path.Combine(thumbDirectory, $"{queueItem.VideoId.ToString().PadLeft(2, '0')}.jpg");
            if (!File.Exists(thumbPath))
                _generateThumbnails.Execute(queueItem.OutputDirectory, thumbPath, _configuration.ThumbnailFramesIntoVideo);
        }

        private void SaveMp4Video(VideoEncodeQueue queueItem, VideoMetadata metadata)
        {
            var crunchedDir = Path.Combine(_configuration.ImportDirectory, queueItem.OutputDirectory);
            var md5 = FileHelpers.GenerateHash(crunchedDir);
            var videoSource = new VideoSource()
            {
                VideoId = queueItem.VideoId,
                Md5 = md5,
                Width = metadata.Width,
                Height = metadata.Height,
                Size = new FileInfo(crunchedDir).Length,
                Extension = Path.GetExtension(crunchedDir).Replace(".", "")
            };
            _repository.SaveVideoSource(videoSource);

            // Move the new file to the bucket
            string vidSubFolder = md5.Substring(0, 2);
            string videoDirectory = Path.Combine(_configuration.BucketDirectory, "video", vidSubFolder);
            FileHelpers.TouchDirectory(videoDirectory);
            string videoBucketDirectory = Path.Combine(videoDirectory, $"{md5}{Path.GetExtension(queueItem.OutputDirectory)}");
            File.Copy(crunchedDir, videoBucketDirectory);
        }

        private void CopyDashVideoAwaitingPackager(VideoEncodeQueue queueItem)
        {
            var crunchedDir = Path.Combine(_configuration.ImportDirectory, "03_transcode_complete", queueItem.OutputDirectory);
            var segmentedDirectory = Path.Combine(_configuration.ImportDirectory, "04_shaka_packager", Path.GetFileNameWithoutExtension(queueItem.InputDirectory));
            var segmentedFullName = Path.Combine(segmentedDirectory, Path.GetFileName(queueItem.OutputDirectory));
            FileHelpers.TouchDirectory(segmentedDirectory);
            File.Copy(crunchedDir, segmentedFullName);
        }

        private void AddToSegmentQueue(VideoEncodeQueue queueItem)
        {
            var segmentedDirectory = Path.Combine(_configuration.ImportDirectory, "04_shaka_packager", Path.GetFileNameWithoutExtension(queueItem.InputDirectory));

            var segmentJob = _repository.GetSegmentJobsForVideo(queueItem.VideoId)
                .Where(j => !j.IsDone && !j.IsReady).FirstOrDefault();

            if (segmentJob == null)
            {
                segmentJob = new VideoSegmentQueue()
                {
                    VideoId = queueItem.VideoId
                };
                _repository.SaveSegmentJob(segmentJob);
            }

            var job = new VideoSegmentQueueItem()
            {
                VideoId = queueItem.VideoId,
                VideoSegmentQueueId = segmentJob.Id,
                ArgStreamFolder = queueItem.MaxHeight.ToString(),
                ArgInputFile = Path.GetFileName(queueItem.OutputDirectory),
                ArgInputFolder = segmentedDirectory,
                ArgStream = "video"
            };

            _repository.SaveSegmentItem(job);
        }

    }
}