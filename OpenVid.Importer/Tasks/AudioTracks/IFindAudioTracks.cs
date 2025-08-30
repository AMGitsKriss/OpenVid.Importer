using Common;
using Database.Models;
using System.Collections.Generic;

namespace OpenVid.Importer.Tasks.AudioTracks
{
    public interface IFindAudioTracks
    {
        List<AudioTrack> Execute(VideoSegmentQueueItem video);
    }
}
