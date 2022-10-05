using Database.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CatalogManager.Segment
{
    public class ShakaPackagerSegmenter : ISegmenter
    {
        public void Execute(List<VideoSegmentQueueItem> videosToSegment)
        {
            var firstVideo = videosToSegment.First();
            Console.WriteLine("Segmenting file {0}", Path.GetFileNameWithoutExtension(firstVideo.ArgInputFile));
            var exe = @"c:\shaka-packager\packager.exe";
            var args = string.Empty;

            foreach (var video in videosToSegment)
            {
                var language = string.IsNullOrWhiteSpace(video.ArgLanguage) ? string.Empty : $",language={video.ArgLanguage}";
                var videoInitFullName = Path.Combine(video.ArgInputFolder, video.ArgStreamFolder, @$"init.mp4");
                var videoItemsFullName = Path.Combine(video.ArgInputFolder, video.ArgStreamFolder, @$"$Number$.m4s");
                var inputFullName = Path.Combine(video.ArgInputFolder, video.ArgInputFile);
                string fileToSegment = @$"'in=""{inputFullName}"",stream={video.ArgStreamId ?? video.ArgStream},init_segment=""{videoInitFullName}"",segment_template=""{videoItemsFullName}""{language}' ";
                args += fileToSegment;
            }
            var dashFile = Path.Combine(firstVideo.ArgInputFolder, "dash.mpd");
            var hlsFile = Path.Combine(firstVideo.ArgInputFolder, "hls.m3u8");
            args += @$"--generate_static_live_mpd --mpd_output ""{dashFile}"" --hls_master_playlist_output ""{hlsFile}"" ";
            args += @$"--default_language jpn --default_text_language eng"; // TODO - Make configurable

            Process proc = new Process();
            proc.StartInfo.FileName = exe;
            proc.StartInfo.Arguments = args;
            proc.StartInfo.CreateNoWindow = false; 
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;

            if (!proc.Start())
            {
                throw new Exception("Error starting the HandbrakeCLI process.");
            }

            string outputString = proc.StandardOutput.ReadToEnd();
            string errorString = proc.StandardError.ReadToEnd();

            proc.WaitForExit();
            proc.Close();

            if (!File.Exists(dashFile) || !File.Exists(hlsFile))
                throw new FileNotFoundException($"The manifest files could not be created in {firstVideo.ArgInputFolder}");
        }
    }
}
