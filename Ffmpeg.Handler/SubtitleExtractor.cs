using Common;
using Serilog;
using System.Text.RegularExpressions;
using Xabe.FFmpeg;

namespace Ffmpeg.Handler
{
    public class SubtitleExtractor
    {
        private readonly ILogger _logger;

        internal List<Subtitle> Extensions = new()
        {
            new Subtitle("srt", "subrip", "srt"),
            new Subtitle("ass", "ass", "ass"),
            new Subtitle("vtt", "webvtt", "webvtt")
        };

        public SubtitleExtractor(ILogger logger)
        {
            _logger = logger;
        }

        public List<ISubtitleStream> FindSubtitles(IMediaInfo mediaInfo, string filePath)
        {
            return mediaInfo.SubtitleStreams.ToList();
        }

        public async Task SaveSubtitles(ISubtitleStream subtitles, string outputFileName, string outputDirectory, string codec = "webvtt")
        {
            var targetSubType = Extensions.First(e => e.Codec == codec.ToLower());

            var outputDir = Path.Combine(outputDirectory, $"{outputFileName}.{targetSubType.Extension}");
            if (File.Exists(outputDir))
                File.Delete(outputDir);

            try
            {
                await FFmpeg.Conversions.New()
                    .AddStream(subtitles)
                    .SetOutputFormat(targetSubType.Format)
                    .SetOutput(outputDir)
                    .Start();

                if (codec == "webvtt")
                {
                    // Get rid of everything inside of curly braces {this is a comment}
                    var subtitleString = File.ReadAllText(outputDir);
                    subtitleString = Regex.Replace(subtitleString, @"{[^}]*}", string.Empty);
                    File.WriteAllText(outputDir, subtitleString);
                }

                _logger.Information($"Subtitle extration succeeded: {outputDir}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Subtitle extration failed: {outputDir}");
            }
        }

        internal class Subtitle
        {
            internal Subtitle(string extension, string codec, string format)
            {
                Extension = extension;
                Codec = codec;
                Format = format;
            }

            internal string Extension { get; set; }
            internal string Codec { get; set; }
            internal string Format { get; set; }
        }
    }
}