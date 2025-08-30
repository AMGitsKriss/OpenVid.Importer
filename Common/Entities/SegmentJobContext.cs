using Common;
using Database.Models;

namespace OpenVid.Importer.Entities
{
    public class SegmentJobContext
    {
        private readonly CatalogImportOptions _configuration;

        private readonly string _ingest = "01_ingest";
        private readonly string _queued = "02_queued";
        private readonly string _transcoded = "03_transcode_complete";
        private readonly string _packager = "04_shaka_packager";

        public SegmentJobContext(CatalogImportOptions configuration, VideoSegmentQueue videoEncodeQueue)
        {
            _configuration = configuration;
            SegmentJob = videoEncodeQueue;
        }

        public VideoSegmentQueue SegmentJob { get; set; }

        // INGEST
        public string WorkingDirectory
        {
            get
            {
                return Path.Combine(_configuration.ImportDirectory, SegmentJob.VideoSegmentQueueItem.First().ArgInputFolder);
            }
        }

        public bool HasAudioTracks => SegmentJob.VideoSegmentQueueItem.Any(i => i.ArgStream == "audio");
        public string ManifestDirectory => Path.Combine(WorkingDirectory, "dash.mpd");
        public string PlaylistDirectory => Path.Combine(WorkingDirectory, "hls.m3u8");
    }
}
