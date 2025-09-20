using Common;
using Common.Entities;
using HandbrakeCliWrapper;
using Microsoft.Extensions.Options;
using Serilog;
using ShellProgressBar;

namespace Handbrake.Handler
{
    public class HandbrakeLibraryEncoder : IEncoder
    {
        private readonly ComponentOptions _options;
        private readonly ILogger _logger;
        private readonly ITranscodeWrapper _transcoder;

        public HandbrakeLibraryEncoder(IOptions<ComponentOptions> options, ILogger logger, ITranscodeWrapper transcoder)
        {
            _options = options.Value;
            _logger = logger;
            _transcoder = transcoder;
        }

        public async Task<bool> Execute(EncodeJobContext jobContext)
        {
            _logger.Information("Converting file {FileName}", jobContext.InputFileName);
            using ProgressBar progressBar = new ProgressBar(10000, jobContext.InputFileName);
            var progress = progressBar.AsProgress<float>();

            GetDimensions(jobContext.QueueItem.MaxHeight, jobContext.SourceWidth, jobContext.SourceHeight, jobContext.SourceAspectRatio, !jobContext.QueueItem.IsVertical, out var width, out var height);

            var config = new CustomHandbrakeConfiguration()
            {
                MaxHeight = height,
                MaxWidth = width,
                Encoder = (Encoder)Enum.Parse(typeof(Encoder), jobContext.QueueItem.Encoder),
                Format = (Format)Enum.Parse(typeof(Format), jobContext.QueueItem.VideoFormat),
                AudioTracks = AudioTracks.all_audio,
                WebOptimize = true,
                VideoQuality = (float)jobContext.QueueItem.Quality
            };

            try
            {
                File.Delete(Path.Combine(jobContext.FolderTranscoded, jobContext.QueueItem.OutputDirectory));
                var transcode = _transcoder.RunTranscode(config, jobContext);
                _logger.Information("Converting file {FileName} with command {Command}", jobContext.InputFileName, $"{_options.Handbrake} -i \"{jobContext.FileQueued}\" -o \"{Path.Combine(jobContext.FolderTranscoded, jobContext.QueueItem.OutputDirectory)}\" " + config.ToString());

                while (transcode.Status != TaskStatus.RanToCompletion)
                {
                    Thread.Sleep(1000);

                    // TODO - Add my progress bar in from the Firestore deleter.
                    progress.Report(_transcoder.GetStatus() / 100);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Attempted to start encode {jobContext.QueueItem.OutputDirectory}, but the file was locked.");
                return false;
            }
        }

        public void GetDimensions(int resolution, double srcWidth, double srcHeight, double aspectRatio, bool isHorizontal, out int width, out int height)
        {
            // Where resolution is the narrow edge like 720 or 1080
            // Thus horizontal wants to be 1280 or 1920. 
            int horizontalResolution = (int)Math.Round(resolution * 1.7777);

            double scale = isHorizontal ? horizontalResolution / srcWidth : horizontalResolution / srcHeight;

            width = (int)Math.Round(srcWidth * scale);
            height = (int)Math.Round(srcHeight * scale);
        }
    }

    public class TranscodeWrapper : ITranscodeWrapper
    {
        private readonly HandbrakeCliWrapper.Handbrake _conv;
        public TranscodeWrapper(IOptions<ComponentOptions> options)
        {
            _conv = new HandbrakeCliWrapper.Handbrake(options.Value.Handbrake);
        }

        public Task RunTranscode(CustomHandbrakeConfiguration config, EncodeJobContext jobContext)
        {
            return Task.Run(async () => await _conv.Transcode(config, jobContext.FileQueued, jobContext.FolderTranscoded, jobContext.QueueItem.OutputDirectory));
        }

        public float GetStatus()
        {
            return _conv.Status.Percentage;
        }

        public float GetAverageFPS()
        {
            return _conv.Status.AverageFps;
        }
    }

    public interface ITranscodeWrapper
    {
        Task RunTranscode(CustomHandbrakeConfiguration config, EncodeJobContext jobContext);
        float GetStatus();
        float GetAverageFPS();
    }
}