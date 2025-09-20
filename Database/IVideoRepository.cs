using Database.Models;
using System.Collections.Generic;
using System.Linq;

namespace Database
{
    public interface IVideoRepository
    {
        VideoSegmentQueue GetPendingSegmentForVideo(int videoId);
        List<VideoSegmentQueueItem> GetItemsForSegmenterJob(int jobId);
        VideoEncodeQueue GetNextPendingEncode();

        VideoSource SaveVideoSource(VideoSource toSave);
        bool SaveEncodeJob(VideoEncodeQueue encodeJob);
        bool SaveSegmentJob(VideoSegmentQueue segmentJob);
        bool SaveSegmentItem(VideoSegmentQueueItem segmentJob);
        bool IsSourceFileStillNeeded(int videoId, int queueItemId);
        void SetPendingSegmentingDone(int videoId);
        IQueryable<VideoSegmentQueue> GetSegmentJobsForVideo(int videoId);
        VideoSegmentQueue GetNextPendingSegment();
        List<VideoEncodeQueue> GetSimilarEncodeJobs(VideoEncodeQueue queueItem);
        bool DeleteVideo(int id);
        IQueryable<Video> GetAllVideos();
        Video GetVideo(int id);
        IQueryable<Tag> DefineTags(List<string> tags);
        Video SaveVideo(Video video);
        IEnumerable<string> GetAllTags();
    }
}