using CatalogManager.Segment;
using Database;
using Database.Extensions;
using Database.Models;
using Microsoft.Extensions.Options;
using OpenVid.Importer.Entities;
using OpenVid.Importer.Helpers;
using System;
using System.IO;
using System.Linq;

namespace OpenVid.Importer
{
    public class SegmenterContainer
    {
        private readonly IVideoRepository _repository;
        private readonly ISegmenter _segmenter;
        private readonly CatalogImportOptions _configuration;

        public SegmenterContainer(IVideoRepository repository, ISegmenter segmenter, IOptions<CatalogImportOptions> configuration)
        {
            _repository = repository;
            _segmenter = segmenter;
            _configuration = configuration.Value;
        }

        public void Run(SegmentJobContext jobContext)
        {
            _segmenter.Execute(jobContext.SegmentJob.VideoSegmentQueueItem.DistinctBy(i => new { i.ArgStream, i.ArgLanguage}).ToList());
            // TODO - Validate that the DASH and HLS files exist

            string md5;
            DirectoryInfo dirInfo = new DirectoryInfo(jobContext.WorkingDirectory);
            long dirSize = dirInfo.GetFiles().Sum(f => f.Length);

            // Create a source for MPD
            md5 = FileHelpers.GenerateHash(jobContext.ManifestDirectory);
            var dashSource = new VideoSource()
            {
                VideoId = jobContext.SegmentJob.VideoId,
                Md5 = md5,
                Extension = "mpd",
                Size = dirSize
            };

            // Create a source for M3U8
            var hlsSource = new VideoSource()
            {
                VideoId = jobContext.SegmentJob.VideoId,
                Md5 = md5,
                Extension = "m3u8",
                Size = dirSize
            };

            _repository.SaveVideoSource(dashSource);
            _repository.SaveVideoSource(hlsSource);

            foreach (var item in jobContext.SegmentJob.VideoSegmentQueueItem)
            {
                try
                {
                    // TODO - Only removed while testing
                    // TODO - Make configurable?
                    File.Delete(item.InputFileFullName); 
                }
                catch (Exception ex)
                {
                    string message = ex.Message;
                }
            }

            string vidSubFolder = md5.Substring(0, 2);
            string finalDirectory = Path.Combine(_configuration.BucketDirectory, "video", vidSubFolder, md5);
            FileHelpers.TouchDirectory(Path.Combine(_configuration.BucketDirectory, "video", vidSubFolder));
            FileHelpers.CopyDirectory(jobContext.WorkingDirectory, finalDirectory);
            Directory.Delete(jobContext.WorkingDirectory, true);

            _repository.SetPendingSegmentingDone(jobContext.SegmentJob.VideoId);
        }
    }
}
