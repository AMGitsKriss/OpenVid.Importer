using Database.Models;
using Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace OpenVid.Importer.Tasks.AudioTracks
{
    public class FfmpegFindAudioTracks : IFindAudioTracks
    {
        private readonly ComponentOptions _options;

        FfmpegFindAudioTracks(IOptions<ComponentOptions> options)
        {
            _options = options.Value;
        }

        public List<AudioTrack> Execute(VideoSegmentQueueItem video)
        {
            var source = Path.Combine(video.ArgInputFolder, video.ArgInputFile);
            Console.WriteLine("Finding audio for file {0}", Path.GetFileNameWithoutExtension(video.ArgInputFile));

            string args = $"-i \"{source}\"";
            Process proc = new Process();
            proc.StartInfo.FileName = _options.Ffmpeg;
            proc.StartInfo.Arguments = args;
            proc.StartInfo.CreateNoWindow = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            if (!proc.Start())
            {
                Console.WriteLine("Error starting");
            }

            proc.WaitForExit();
            proc.Close();

            string outputString = proc.StandardError.ReadToEnd();
            string[] outpuyByLine = outputString.Trim().Split(new char[] { '\n' });

            var regexPattern = @"^(.*?)#0:(\d+)\(([a-zA-Z]+)\): Audio: ([a-zA-Z]+)";
            var languages = outpuyByLine.Select(s => Regex.Match(s, regexPattern));

            var results = new List<AudioTrack>();
            foreach (var match in languages)
            {
                var stream = match.Groups[2].Value;
                var language = match.Groups[3].Value;
                var trackInfo = new AudioTrack()
                {
                    Id = stream,
                    Language = language
                };
                results.Add(trackInfo);
            }

            return results;
        }
    }
}
