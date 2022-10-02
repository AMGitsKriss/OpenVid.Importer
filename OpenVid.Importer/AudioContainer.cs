using Database;
using Database.Models;
using OpenVid.Importer.Tasks.AudioTracks;
using System.Linq;

namespace OpenVid.Importer
{
    public class AudioContainer
    {
        private readonly IVideoRepository _repository;
        private readonly IFindAudioTracks _trackFinder;

        public AudioContainer(IVideoRepository repository, IFindAudioTracks trackFinder)
        {
            _repository = repository;
            _trackFinder = trackFinder;
        }

        public void Run(VideoSegmentQueue selectedJob)
        {
            var videoStream = selectedJob.VideoSegmentQueueItem.First(i => i.ArgStream == "video");
            var audioStreams = _trackFinder.Execute(videoStream);

            foreach (var stream in audioStreams)
            {
                var segmentItem = new VideoSegmentQueueItem()
                {
                    VideoId = selectedJob.VideoId,
                    VideoSegmentQueueId = selectedJob.Id,

                    ArgStream = "audio",
                    ArgStreamId = stream.Id,
                    ArgInputFolder = videoStream.ArgInputFolder,
                    ArgInputFile = videoStream.ArgInputFile,
                    ArgStreamFolder = $"audio_{stream.Language}",
                    ArgLanguage = stream.Language
                };

                _repository.SaveSegmentItem(segmentItem);
            }
        }
    }
}
