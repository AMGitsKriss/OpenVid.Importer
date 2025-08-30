using Common;
using Serilog;
using Xabe.FFmpeg;

namespace Ffmpeg.Handler
{
    public class MetadataExtractor
    {
        private readonly ILogger _logger;

        public MetadataExtractor(ILogger logger)
        {
            _logger = logger;
        }
        public async Task<IMediaInfo> Extract(string location)
        {
            _logger.Information("Finding MetaData for file {0}", Path.GetFileNameWithoutExtension(location));

            IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(location);

            return mediaInfo;
        }

        public VideoMetadata GetMetadata(IMediaInfo mediaInfo)
        {
            VideoMetadata properties = new VideoMetadata()
            {
                Width = mediaInfo.VideoStreams.First().Width,
                Height = mediaInfo.VideoStreams.First().Height,
                Duration = mediaInfo.Duration
            };

            return properties;
        }
    }
}