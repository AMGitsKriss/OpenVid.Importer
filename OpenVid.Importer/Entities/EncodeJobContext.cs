using Database.Models;

namespace OpenVid.Importer.Entities
{
    public class EncodeJobContext
    {
        private readonly CatalogImportOptions _configuration;
        private VideoEncodeQueue _videoEncodeQueue;

        private readonly string _ingest = "01_ingest";
        private readonly string _queued = "02_queued";
        private readonly string _transcoded = "03_transcode_complete";
        private readonly string _packager = "04_shaka_packager";

        public EncodeJobContext(CatalogImportOptions configuration, VideoEncodeQueue videoEncodeQueue)
        {
            _configuration = configuration;
            _videoEncodeQueue = videoEncodeQueue;
        }

        public string FolderIngest { get; }
        public string FolderQueued { get; }
        public string FolderTranscoded { get; }
        public string FolderPackager { get; }

        public string FileIngest { get; }
        public string FileQueued { get; }
        public string FileTranscoded { get; }
        public string FilePackager { get; }
    }
}
