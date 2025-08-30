using HandbrakeCliWrapper;
using System.Text;

namespace Handbrake.Handler
{
    public class CustomHandbrakeConfiguration : HandbrakeConfiguration
    {
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (MaxHeight > 0)
            {
                stringBuilder.Append($"--maxHeight {MaxHeight} ");
            }

            if (MaxWidth > 0)
            {
                stringBuilder.Append($"--maxWidth {MaxWidth} ");
            }

            if (WebOptimize)
            {
                stringBuilder.Append("--optimize ");
            }

            stringBuilder.Append($"--format {Format} ");
            stringBuilder.Append($"--encoder {Encoder} ");
            stringBuilder.Append($"--encoder-preset placebo ");
            stringBuilder.Append($"--quality {VideoQuality} ");

            stringBuilder.Append("--" + AudioTracks.Formatted() + " ");

            stringBuilder.Append("--all-subtitles ");

            stringBuilder.Append("--encoder-level " + EncoderLevel.Formatted() + " ");
            stringBuilder.Append("--verbose 0 ");
            return stringBuilder.ToString();
        }
    }
}