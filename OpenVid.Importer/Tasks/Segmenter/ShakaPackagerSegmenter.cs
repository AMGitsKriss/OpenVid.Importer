using Common;
using Database.Models;

using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CatalogManager.Segment
{
    public class ShakaPackagerSegmenter : ISegmenter
    {
        private readonly CatalogImportOptions _configuration;
        private readonly ILogger _logger;

        public ShakaPackagerSegmenter(IOptions<CatalogImportOptions> configuration, ILogger logger)
        {
            _configuration = configuration.Value;
            _logger = logger;
        }

        public void Execute(List<VideoSegmentQueueItem> videosToSegment)
        {
            var firstVideo = videosToSegment.First(i => i.ArgStream == "video");
            Console.WriteLine("Segmenting file {0}", Path.GetFileNameWithoutExtension(firstVideo.ArgInputFile));
            var exe = @"c:\shaka-packager\packager.exe";
            var args = string.Empty;

            foreach (var video in videosToSegment)
            {
                video.ArgInputFolder = video.ArgInputFolder.Replace(".mkv", "");

                var language = string.IsNullOrWhiteSpace(video.ArgLanguage) ? string.Empty : $",language={video.ArgLanguage}";
                var videoInitFullName = Path.Combine(_configuration.ImportDirectory, video.ArgInputFolder, video.ArgStreamFolder, @$"init.mp4");
                var videoItemsFullName = Path.Combine(_configuration.ImportDirectory, video.ArgInputFolder, video.ArgStreamFolder, @$"$Number$.m4s");
                var inputFullName = Path.Combine(_configuration.ImportDirectory, video.ArgInputFolder, video.ArgInputFile);
                string fileToSegment = @$"in=""{inputFullName}"",stream={video.ArgStreamId ?? video.ArgStream},init_segment=""{videoInitFullName}"",segment_template=""{videoItemsFullName}""{language} ";
                args += fileToSegment;
            }
            var dashFile = Path.Combine(_configuration.ImportDirectory, firstVideo.ArgInputFolder, "dash.mpd");
            var hlsFile = Path.Combine(_configuration.ImportDirectory, firstVideo.ArgInputFolder, "hls.m3u8");
            args += @$"--generate_static_live_mpd --mpd_output ""{dashFile}"" --hls_master_playlist_output ""{hlsFile}"" ";
            args += @$"--default_language jpn --default_text_language eng"; // TODO - Make configurable

            Process proc = new Process();
            proc.StartInfo.FileName = exe;
            proc.StartInfo.Arguments = args;
            proc.StartInfo.CreateNoWindow = false;
            proc.StartInfo.UseShellExecute = false;

            proc.StartInfo.RedirectStandardOutput = false;
            proc.StartInfo.RedirectStandardError = false;
            string testString = string.Empty;
            proc.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                // Prepend line numbers to each line of the output.
                if (!String.IsNullOrEmpty(e.Data))
                {
                    var line = "\n[" + DateTime.Now + "]: " + e.Data;
                    testString += line;
                }
            });

            var logMsg = $"Packaging {videosToSegment.First().VideoId} \n \"{exe} {args}\"";
            if (!proc.Start())
            {
                var ex = new Exception("Error starting the HandbrakeCLI process.");
                _logger.Error(ex, logMsg);
                throw ex;
            }
            else
            {
                _logger.Information(logMsg);
            }

            proc.WaitForExit();
            proc.Close();

            if (!File.Exists(dashFile) || !File.Exists(hlsFile))
            {
                var ex = new FileNotFoundException($"The manifest files could not be created in {firstVideo.ArgInputFolder}");
                _logger.Error(ex, $"Failed to execute command \"{exe} {args}\"");
                throw ex;
            }
        }
    }
}
