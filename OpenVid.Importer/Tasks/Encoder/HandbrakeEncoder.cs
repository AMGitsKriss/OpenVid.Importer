using Microsoft.Extensions.Options;
using OpenVid.Importer.Entities;
using System;
using System.Diagnostics;

namespace OpenVid.Importer.Tasks.Encoder
{
    public class HandbrakeEncoder : IEncoder
    {
        public void Execute(EncodeJobContext jobContext)
        {
            Console.WriteLine("Converting file {0}", jobContext.InputFileName);

            string exe = @"C:\handbrakecli\HandBrakeCLI.exe"; // TODO - [HandbrakeCLI] Should be configurable.What if I want to install this elsewhere?
            double sixteenNineRatio = 1.77777;
            string vertDimensions = $" --maxWidth {jobContext.QueueItem.MaxHeight}  --maxHeight {Math.Ceiling(sixteenNineRatio * jobContext.QueueItem.MaxHeight)}";
            string horizDimensions = $" --maxHeight {jobContext.QueueItem.MaxHeight} --maxWidth {Math.Ceiling(sixteenNineRatio * jobContext.QueueItem.MaxHeight)}";
            string dimensionArgs = jobContext.QueueItem.IsVertical ? vertDimensions : horizDimensions;
            string args = $@" -i ""{jobContext.FileQueued}"" -o ""{jobContext.FileTranscoded}"" -e {jobContext.QueueItem.Encoder} --encoder-preset {jobContext.QueueItem.RenderSpeed} -f {jobContext.QueueItem.VideoFormat} --optimize --all-audio --all-subtitles -q {jobContext.QueueItem.Quality} {dimensionArgs}";

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