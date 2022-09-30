using CatalogManager.Segment;
using Database;
using Database.Models;
using Microsoft.Extensions.Options;
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
        private volatile bool _continueJob;

        public SegmenterContainer(IVideoRepository repository, ISegmenter segmenter, IOptions<CatalogImportOptions> configuration)
        {
            _repository = repository;
            _segmenter = segmenter;
            _configuration = configuration.Value;
        }

        public void Run(VideoSegmentQueue selectedJob)
        {
            var workingDirectory = selectedJob.VideoSegmentQueueItem.First().ArgInputFolder;
            _segmenter.Execute(selectedJob.VideoSegmentQueueItem.ToList());
            // TODO - Validate that the DASH and HLS files exist

            string md5;
            string videoManifestDir = Path.Combine(workingDirectory, "dash.mpd");
            string videoPlaylistDir = Path.Combine(workingDirectory, "hls.m3u8");

            DirectoryInfo dirInfo = new DirectoryInfo(workingDirectory);
            long dirSize = dirInfo.GetFiles().Sum(f => f.Length);

            // Create a source for MPD
            md5 = FileHelpers.GenerateHash(videoManifestDir);
            var dashSource = new VideoSource()
            {
                VideoId = selectedJob.VideoId,
                Md5 = md5,
                Extension = "mpd",
                Size = dirSize
            };

            // Create a source for M3U8
            var hlsSource = new VideoSource()
            {
                VideoId = selectedJob.VideoId,
                Md5 = md5,
                Extension = "m3u8",
                Size = dirSize
            };

            _repository.SaveVideoSource(dashSource);
            _repository.SaveVideoSource(hlsSource);

            foreach (var item in selectedJob.VideoSegmentQueueItem)
            {
                try
                {
                    // TODO - Only removed while testing
                    // TODO - Make configurable?
                    //File.Delete(item.InputFileFullName); 
                }
                catch (Exception ex)
                {
                    string message = ex.Message;
                }
            }

            string vidSubFolder = md5.Substring(0, 2);
            string finalDirectory = Path.Combine(_configuration.BucketDirectory, "video", vidSubFolder, md5);
            FileHelpers.TouchDirectory(Path.Combine(_configuration.BucketDirectory, "video", vidSubFolder));
            Directory.Move(workingDirectory, finalDirectory);

            _repository.SetPendingSegmentingDone(selectedJob.VideoId);
        }
    }
}
