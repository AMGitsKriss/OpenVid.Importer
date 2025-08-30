using HandbrakeCliWrapper;

namespace Handbrake.Handler
{
    internal static class EnumFormatter
    {
        public static string Formatted(this EncoderLevel encoderLevel)
        {
            return encoderLevel.ToString().TrimStart(new char[1] { '_' }).Replace("_", ".");
        }

        public static string Formatted(this AudioSampleRate audioSampleRate)
        {
            return audioSampleRate.ToString().TrimStart(new char[1] { '_' }).Replace("_", ".");
        }

        public static string Formatted(this Mixdown mixdown)
        {
            return mixdown.ToString().TrimStart(new char[1] { '_' }).Replace("__", ".");
        }

        public static string Formatted(this AudioTracks audioTracks)
        {
            return audioTracks.ToString().Replace("_", "-");
        }

        public static string Formatted(this AudioEncoder audioEncoder)
        {
            return audioEncoder.ToString().Replace("__", ":");
        }

        public static string Formatted(this Anamorphic anamorphic)
        {
            return anamorphic.ToString().Replace("_", "-");
        }

        public static string Formatted(this List<AudioCopyMask> audioCopyMasks)
        {
            return string.Join(",", audioCopyMasks);
        }
    }
}