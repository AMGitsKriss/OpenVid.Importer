using Database.Models;
using System;
using System.Diagnostics;
using System.IO;

namespace OpenVid.Importer.Tasks.Encoder
{
    public class HandbrakeEncoder : IEncoder
    {
        public void Execute(VideoEncodeQueue queueItem)
        {
            Console.WriteLine("Converting file {0}", Path.GetFileNameWithoutExtension(queueItem.InputDirectory));
            Helpers.FileHelpers.TouchDirectory(Path.GetDirectoryName(queueItem.OutputDirectory));

            string exe = @"C:\handbrakecli\HandBrakeCLI.exe"; // TODO - [HandbrakeCLI] Should be configurable.What if I want to install this elsewhere?
            string dimensionArgs = queueItem.IsVertical ? $" --maxWidth {queueItem.MaxHeight}" : $" --maxHeight {queueItem.MaxHeight}";
            string args = $@" -i ""{queueItem.InputDirectory}"" -o ""{queueItem.OutputDirectory}"" -e {queueItem.Encoder} --encoder-preset {queueItem.RenderSpeed} -f {queueItem.VideoFormat} --optimize --all-audio --all-subtitles -q {queueItem.Quality} {dimensionArgs}";

            Process proc = new Process();
            proc.StartInfo.FileName = exe;
            proc.StartInfo.Arguments = args;
            proc.StartInfo.CreateNoWindow = false; // Set to true if we want to hide output.
            proc.StartInfo.UseShellExecute = false;
            if (!proc.Start())
            {
                throw new Exception("Error starting the HandbrakeCLI process.");
            }
            proc.WaitForExit();
            proc.Close();
        }
    }
}