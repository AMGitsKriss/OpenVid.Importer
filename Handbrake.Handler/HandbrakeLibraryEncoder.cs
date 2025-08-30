using Common.Entities;
using HandbrakeCliWrapper;
using Serilog;
using ShellProgressBar;
using System;

namespace Handbrake.Handler
{
    public class HandbrakeLibraryEncoder : IEncoder
    {
        private readonly ILogger _logger;

        public HandbrakeLibraryEncoder(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<bool> Execute(EncodeJobContext jobContext)
        {
            _logger.Information("Converting file {FileName}", jobContext.InputFileName);
            using ProgressBar progressBar = new ProgressBar(10000, jobContext.InputFileName);
            var progress = progressBar.AsProgress<float>();

            GetDimensions(jobContext.QueueItem.MaxHeight, jobContext.SourceAspectRatio, !jobContext.QueueItem.IsVertical, out var width, out var height);

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

            HandbrakeCliWrapper.Handbrake conv = new HandbrakeCliWrapper.Handbrake(@"C:\handbrakecli\HandBrakeCLI.exe");

            try
            {
                File.Delete(Path.Combine(jobContext.FolderTranscoded, jobContext.QueueItem.OutputDirectory));
                var transcode = Task.Run(async () => await conv.Transcode(config, jobContext.FileQueued, jobContext.FolderTranscoded, jobContext.QueueItem.OutputDirectory));
                _logger.Information("Converting file {FileName} with command {Command}", jobContext.InputFileName, $"C:\\handbrakecli\\HandBrakeCLI.exe -i \"{jobContext.FileQueued}\" -o \"{Path.Combine(jobContext.FolderTranscoded, jobContext.QueueItem.OutputDirectory)}\" " + config.ToString());

                while (transcode.Status != TaskStatus.RanToCompletion)
                {
                    Thread.Sleep(1000);

                    // TODO - Add my progress bar in from the Firestore deleter.
                    progress.Report(conv.Status.Percentage / 100);
                    //Console.WriteLine($"Progress: {conv.Status.Percentage}%");
                }
                return true;
            }
            catch(Exception ex) {
                _logger.Error(ex, $"Attempted to start encode {jobContext.QueueItem.OutputDirectory}, but the file was locked.");
                return false;
            }            
        }

        private void GetDimensions(int maxDimension, double aspectRatio, bool isHorizontal, out int width, out int height)
        {
            // Every should fit within the standard 16:9 letterboxing. 

            double sixteenNineRatio = 1.77777;
            int displayHeight = maxDimension;  // 1080p but actual video is 800
            int displayWidth = (int)Math.Round(sixteenNineRatio * displayHeight); // Thus width on 1080p is 1920 

            int actualWidth = displayWidth; // the width on 1080p is always 1920, regardless or ratio
            double actualRatio = aspectRatio;
            int actualHeight = (int)Math.Round(actualWidth / actualRatio); // but height depends on the aspect ratio...

            if (isHorizontal)
            {
                height = actualHeight;
                width = actualWidth;
            }
            else
            {
                height = actualWidth;
                width = actualHeight;
            }
        }
    }
}