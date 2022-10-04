﻿using Database.Models;
using System.IO;

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
            QueueItem = videoEncodeQueue;
        }

        public VideoSegmentQueue QueueItem { get; set; }

        // INGEST
        public string FolderIngest
        {
            get
            {
                return Path.Combine(_configuration.ImportDirectory, _ingest);
            }
        }
        public string FileIngest
        {
            get
            {
                return Path.Combine(FolderIngest, QueueItem.InputDirectory);
            }
        }

        // QUEUED
        public string FolderQueued
        {
            get
            {
                return Path.Combine(_configuration.ImportDirectory, _queued);
            }
        }
        public string FileQueued
        {
            get
            {
                return Path.Combine(FolderQueued, QueueItem.InputDirectory);
            }
        }

        // TRANSCODED
        public string FolderTranscoded
        {
            get
            {
                return Path.Combine(_configuration.ImportDirectory, _transcoded);
            }
        }
        public string FileTranscoded
        {
            get
            {
                return Path.Combine(FolderTranscoded, QueueItem.OutputDirectory);
            }
        }

        // PENDING PACKAGING
        public string FolderPackager
        {
            get
            {
                return Path.Combine(_configuration.ImportDirectory, _packager);
            }
        }
        public string FilePackager
        {
            get
            {
                return Path.Combine(FolderPackager, QueueItem.OutputDirectory);
            }
        }

        public string InputFileName => Path.GetFileNameWithoutExtension(QueueItem.InputDirectory);
        public string InputExtension => Path.GetExtension(QueueItem.InputDirectory);

        public string OutputFileName => Path.GetFileNameWithoutExtension(QueueItem.OutputDirectory);
        public string OutputExtension => Path.GetExtension(QueueItem.OutputDirectory);
    }
}
