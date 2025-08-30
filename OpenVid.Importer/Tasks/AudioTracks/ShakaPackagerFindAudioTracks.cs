using Common;
using Database.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenVid.Importer.Tasks.AudioTracks
{
    public class ShakaPackagerFindAudioTracks : IFindAudioTracks
    {
        public List<AudioTrack> Execute(VideoSegmentQueueItem video)
        {
            Console.WriteLine("Finding audio for file {0}", Path.GetFileNameWithoutExtension(video.ArgInputFile));
            var location = Path.Combine(video.ArgInputFolder, video.ArgInputFile);

            string cmd = $"in=\"{location}\" --dump_stream_info";
            Process proc = new Process();
            proc.StartInfo.FileName = @"c:\shaka-packager\packager.exe";
            proc.StartInfo.Arguments = cmd;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            if (!proc.Start())
            {
                Console.WriteLine("Error starting");
            }

            string outputString = proc.StandardOutput.ReadToEnd();
            string[] outpuyByLine = outputString.Trim().Split("\r\n\r\n").Select(s => s.Replace("\n", string.Empty)).Where(s => s.Contains("type: Audio")).ToArray();

            //var regexPattern = @"Stream \[([0-9]+)\] type: Audio";
            var regexPattern = @"Stream \[([0-9]+)\] type: Audio((.)*) language: ([a-zA-Z]+)";
            var languages = outpuyByLine.Select(s => Regex.Match(s, regexPattern));

            proc.WaitForExit();
            proc.Close();

            return languages.Select(l => new AudioTrack()
            {
                Id = l.Groups[1].Value,
                Language = l.Groups[4].Value
            }).ToList();
        }
    }
}
