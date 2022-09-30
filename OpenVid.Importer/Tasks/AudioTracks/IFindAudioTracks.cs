using Database.Models;
using OpenVid.Importer.Models;
using System.Collections.Generic;

namespace OpenVid.Importer.Tasks.AudioTracks
{
    public interface IFindAudioTracks
    {
        List<AudioTrack> Execute(VideoSegmentQueueItem video);
    }
}
