using System;
using System.IO;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Database.Models
{
    public partial class VideoSegmentQueueItem
    {
        public int Id { get; set; }
        public int VideoId { get; set; }
        public int VideoSegmentQueueId { get; set; }
        /// <summary>
        /// eg. C:\video1
        /// </summary>
        public string ArgInputFolder { get; set; }
        /// <summary>
        /// eg. video.mp4, eng.vtt
        /// </summary>
        public string ArgInputFile { get; set; }
        /// <summary>
        /// eg. audio, video, text
        /// </summary>
        public string ArgStream { get; set; }

        /// <summary>
        /// eg. audio_eng, 720, subtitles_eng
        /// </summary>
        public string ArgStreamFolder { get; set; }
        /// <summary>
        /// eg. en, eng, jp, jpn
        /// </summary>
        public string? ArgLanguage { get; set; }

        public string InputFileFullName
        {
            get
            {
                return Path.Combine(ArgInputFolder, ArgInputFile);
            }
        }

        public virtual VideoSegmentQueue VideoSegmentQueue { get; set; }
        public virtual Video Video { get; set; }
    }
}
