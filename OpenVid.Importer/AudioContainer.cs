using Database;
using Database.Models;
using OpenVid.Importer.Entities;
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

        public void Run(SegmentJobContext jobContext)
        {
            var videoStream = jobContext.SegmentJob.VideoSegmentQueueItem.Where(i => i.ArgStream == "video").OrderByDescending(i => int.Parse(i.ArgStreamFolder)).First();
            var audioStreams = _trackFinder.Execute(videoStream);

            if (!audioStreams.Any())
            {
                var segmentItem = new VideoSegmentQueueItem()
                {
                    VideoId = jobContext.SegmentJob.VideoId,
                    VideoSegmentQueueId = jobContext.SegmentJob.Id,

                    ArgStream = "audio",
                    ArgInputFolder = videoStream.ArgInputFolder,
                    ArgInputFile = videoStream.ArgInputFile,
                    ArgStreamFolder = $"audio_eng",
                    ArgLanguage = "eng"
                };

                _repository.SaveSegmentItem(segmentItem);
            }
            else
            {
                foreach (var stream in audioStreams)
                {
                    var segmentItem = new VideoSegmentQueueItem()
                    {
                        VideoId = jobContext.SegmentJob.VideoId,
                        VideoSegmentQueueId = jobContext.SegmentJob.Id,

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
}
