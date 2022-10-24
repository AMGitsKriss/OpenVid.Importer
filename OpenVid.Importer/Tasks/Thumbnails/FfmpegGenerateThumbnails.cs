using System;
using System.Diagnostics;
using System.IO;

namespace OpenVid.Importer.Tasks.Thumbnails
{
    public class FfmpegGenerateThumbnails : IGenerateThumbnails
    {
        // TODO - Fix thumbnails. Test Videos:
        // 14680, 14657, 14560, 14232, 13102, 12044, 11959, 14743
        public void Execute(string videoPath, string thumbPath)
        {
            Console.WriteLine("Generating thumbnail for file {0}", Path.GetFileNameWithoutExtension(videoPath));

            var cmd = $" -ss 5 -y -itsoffset -1 -i \"{videoPath}\" -vcodec mjpeg -frames:v 1 -filter:v \"scale=300:168:force_original_aspect_ratio=decrease,pad=300:168:-1:-1:color=black\" \"{thumbPath}\"";

            var startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Normal,
                FileName = @"c:\ffmpeg\ffmpeg.exe", // TODO - [ffmpeg] Should be configurable.What if I want to install this elsewhere?
                Arguments = cmd
            };

            var process = new Process
            {
                StartInfo = startInfo
            };

            process.Start();
            process.WaitForExit();
            process.Close();
        }
    }
}
