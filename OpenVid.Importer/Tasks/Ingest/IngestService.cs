using Common;
using Database;
using Database.Models;
using Ffmpeg.Handler;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace OpenVid.Importer.Tasks.Ingest
{
    public class IngestService
    {
        private readonly CatalogImportOptions _configuration;
        private readonly IVideoRepository _repository;
        private readonly MetadataExtractor _metadata;
        private readonly SubtitleExtractor _subtitles;
        private readonly ILogger _logger;

        public IngestService(ILogger logger, IOptions<CatalogImportOptions> configuration, IVideoRepository repository, MetadataExtractor metadata, SubtitleExtractor subtitles)
        {
            _configuration = configuration.Value;
            _repository = repository;
            _metadata = metadata;
            _subtitles = subtitles;
            _logger = logger;
        }

        public async Task IngestFiles()
        {
            var pendingFiles = FindFiles();
            Console.WriteLine($"Step 1 - Importing {pendingFiles.Count()} videos.");

            var queuedDirectory = Path.Combine(_configuration.ImportDirectory, "02_queued");


            foreach (var pending in pendingFiles)
            {
                if (pending.FileName == "Thumbs.db")
                    continue;

                // DATABASE
                var mediaInfo = await _metadata.Extract(pending.FullName);
                var metaData = _metadata.GetMetadata(mediaInfo);

                var video = await CreateVideoInDatabase(pending, metaData);
                var videoId = await QueueVideoEncodes(pending, mediaInfo, metaData, video);

                if (videoId == 0)
                    continue;

                var newFileName = SanitiseFileName(pending.FileName);
                if (!MoveFileToDirectory(pending.FullName, queuedDirectory, newFileName))
                    _repository.DeleteVideo(videoId);
            }
        }

        public List<ImportableVideo> FindFiles()
        {
            var importDir = Path.Combine(_configuration.ImportDirectory, "01_ingest");
            Directory.CreateDirectory(importDir);
            return FindFiles(importDir, importDir);
        }

        public List<ImportableVideo> FindFiles(string dir, string prefix)
        {
            try
            {
                var allTags = _repository.GetAllTags().Select(t => t.Replace("_", string.Empty)).Where(t => t.Length > 5);
                Regex rgx = new Regex("[^a-zA-Z0-9]");

                var result = new List<ImportableVideo>();
                var suggestedTags = dir.Replace(prefix, "").Split(new[] { @"\", " " }, StringSplitOptions.RemoveEmptyEntries).ToList();

                // Files
                foreach (var file in Directory.GetFiles(dir))
                {
                    var truncatedName = rgx.Replace(Path.GetFileNameWithoutExtension(file), "");
                    var tagsFromFileName = allTags.Where(t => truncatedName.Contains(t));
                    var mergedTags = suggestedTags.Concat(tagsFromFileName);

                    var info = new FileInfo(file);
                    var video = new ImportableVideo()
                    {
                        FileName = info.Name,
                        FullName = info.FullName,
                        FileLocation = info.DirectoryName,
                        SuggestedTags = mergedTags.ToList(),
                        Size = info.Length
                    };
                    result.Add(video);
                }

                // Directories
                foreach (var folder in Directory.GetDirectories(dir))
                {
                    result.AddRange(FindFiles(folder, prefix));
                }

                return result;
            }
            catch (DirectoryNotFoundException)
            {

                return FindFiles(dir, prefix);
            }
        }

        private async Task<Video> CreateVideoInDatabase(ImportableVideo pending, VideoMetadata metaData)
        {
            var tags = _repository.DefineTags(pending.SuggestedTags);

            var toSave = new Video()
            {
                Name = Path.GetFileNameWithoutExtension(pending.FileName),
                Length = metaData.Duration,
                VideoEncodeQueue = new List<VideoEncodeQueue>(),
                VideoSegmentQueue = new List<VideoSegmentQueue>(),
                VideoTag = tags.Select(t => new VideoTag()
                {
                    Tag = t
                }).ToList()
            };

            toSave = _repository.SaveVideo(toSave);

            return toSave;
        }

        private async Task<int> QueueVideoEncodes(ImportableVideo pending, IMediaInfo mediaInfo, VideoMetadata metaData, Video toSave)
        {
            // If our source is 720p, don't bother trying to use the 1080p preset.
            var presets = await GetPresets(mediaInfo, pending.FullName);

            foreach (var preset in presets)
            {
                var newFileName = SanitiseFileName(pending.FileName);
                var newFileNameWithoutExtension = Path.GetFileNameWithoutExtension(newFileName);
                toSave.VideoEncodeQueue.Add(new VideoEncodeQueue()
                {
                    VideoId = toSave.Id,
                    InputDirectory = newFileName,
                    OutputDirectory = $"{newFileNameWithoutExtension}_{preset.MaxHeight}.mp4",
                    Encoder = preset.Encoder,
                    RenderSpeed = preset.RenderSpeed,
                    VideoFormat = preset.VideoFormat,
                    PlaybackFormat = preset.PlaybackFormat,
                    Quality = preset.Quality,
                    MaxHeight = preset.MaxHeight,
                    IsVertical = metaData.Height > metaData.Width
                });
            }

            // TODO - If I have a 720p Dash and a 720p mp4, I only need to encode once.

            if (presets.Any(p => p.PlaybackFormat == "dash"))
            {
                var segmentJob = new VideoSegmentQueue()
                {
                    VideoId = toSave.Id,
                    VideoSegmentQueueItem = new List<VideoSegmentQueueItem>()
                };
                toSave.VideoSegmentQueue.Add(segmentJob);

                // TODO - Only touch the folder if we have subtitles
                var subtitleSaveDir = Path.Combine(_configuration.ImportDirectory, "04_shaka_packager", Path.GetFileNameWithoutExtension(SanitiseFileName(pending.FileName)));
                Helpers.FileHelpers.TouchDirectory(subtitleSaveDir);

                var subtitleBackupDir = Path.Combine(_configuration.BucketDirectory, "Subtitles", toSave.Id.ToString().PadLeft(4, '0'));
                Helpers.FileHelpers.TouchDirectory(subtitleBackupDir);

                // TODO - This is file system work. Should not be in database function.
                var subtitleStreams = _subtitles.FindSubtitles(mediaInfo, pending.FullName);

                foreach (var subtitle in subtitleStreams)
                {
                    var outputFileName = $"{subtitle.Index}_{subtitle.Language}";
                    await _subtitles.SaveSubtitles(subtitle, outputFileName, subtitleSaveDir);
                    await _subtitles.SaveSubtitles(subtitle, outputFileName, subtitleBackupDir, subtitle.Codec); // TODO - Can we remove this?

                    segmentJob.VideoSegmentQueueItem.Add(new VideoSegmentQueueItem()
                    {
                        VideoId = toSave.Id,
                        ArgStream = "text",
                        ArgInputFile = $"{outputFileName}.vtt",
                        ArgInputFolder = Path.Combine("04_shaka_packager", Path.GetFileNameWithoutExtension(SanitiseFileName(pending.FileName))),
                        ArgStreamFolder = $"subtitle_{subtitle.Language}"
                    });
                }
            }

            toSave = _repository.SaveVideo(toSave);

            return toSave.Id;
        }

        private string SanitiseFileName(string fileName)
        {
            string extension = Path.GetExtension(fileName);
            fileName = Path.GetFileNameWithoutExtension(fileName);
            fileName = Regex.Replace(fileName, @" ", "_");
            fileName = Regex.Replace(fileName, @"[,~!?\-]|\[(.*?)\]|\((.*?)\)", string.Empty);
            fileName = Regex.Replace(fileName, @"_+", "_");
            //fileName = fileName.Trim('_');
            return $"{fileName}{extension}";
        }

        private bool MoveFileToDirectory(string sourceFullName, string targetDirectory, string destinationFileName)
        {
            try
            {
                if (File.Exists(sourceFullName))
                {
                    if (!Directory.Exists(targetDirectory))
                        Directory.CreateDirectory(targetDirectory);
                    File.Move(sourceFullName, Path.Combine(targetDirectory, destinationFileName));
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        private async Task<List<EncoderPresetOptions>> GetPresets(IMediaInfo mediaInfo, string fileFullName)
        {
            // TODO - This is gnarly. Is there a nicer way to do it?
            // TODO - Base this on width instead of height. Movies seem to be 1920x800, not *really* 1080p
            var results = new List<EncoderPresetOptions>();
            var metadata = _metadata.GetMetadata(mediaInfo);

            // 1080p ultrawide is usually more like 800p with a 1920 width. This way we'll pretend it has black bars so we get the width consistent
            int heightIfSixteenNine = (int)Math.Ceiling(metadata.Width * 0.5625d);

            // Find MP4 Presets
            var mp4Presets = _configuration.EncoderPresets.Where(v => v.MaxHeight <= heightIfSixteenNine && v.PlaybackFormat == "mp4").ToList(); // skip bigger presets
            var smallestmp4Preset = _configuration.EncoderPresets.Where(v => v.PlaybackFormat == "mp4").OrderBy(v => v.MaxHeight).FirstOrDefault();

            // Find MPD PResets
            var mpdPresets = _configuration.EncoderPresets.Where(v => v.MaxHeight <= heightIfSixteenNine && v.PlaybackFormat == "dash").ToList();
            var smallestmpdPreset = _configuration.EncoderPresets.Where(v => v.PlaybackFormat == "dash").OrderBy(v => v.MaxHeight).FirstOrDefault();

            // If the video is smaller than all of the given presets
            if (!mp4Presets.Any() && smallestmp4Preset != null)
            {
                if (mpdPresets.Any() && smallestmp4Preset.MaxHeight > mpdPresets.Select(m => m.MaxHeight).Max())
                    smallestmp4Preset.MaxHeight = mpdPresets.Select(m => m.MaxHeight).Max();
                else if (smallestmpdPreset != null && smallestmp4Preset.MaxHeight > smallestmpdPreset.MaxHeight)
                    smallestmp4Preset.MaxHeight = smallestmpdPreset.MaxHeight;
                results.Add(smallestmp4Preset);
            }
            else
            {
                results.AddRange(mp4Presets);
            }

            // Set MPD
            if (!mpdPresets.Any() && smallestmpdPreset != null)
                results.Add(smallestmpdPreset);
            else
                results.AddRange(mpdPresets);

            return results;
        }
    }
}
