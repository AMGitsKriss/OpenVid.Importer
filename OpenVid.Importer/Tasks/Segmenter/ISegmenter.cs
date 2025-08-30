using Database.Models;
using System.Collections.Generic;

namespace CatalogManager.Segment
{
    public interface ISegmenter
    {
        void Execute(List<VideoSegmentQueueItem> videosToSegment);
    }
}
