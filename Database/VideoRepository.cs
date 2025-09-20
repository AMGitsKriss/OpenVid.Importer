using Dapper;
using Database.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Database
{
    public class VideoRepository : IVideoRepository
    {
        private readonly OpenVidContext _context;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public VideoRepository(OpenVidContext context, IDbConnectionFactory dbConnectionFactory)
        {
            _context = context;
            _dbConnectionFactory = dbConnectionFactory;
        }

        public VideoSegmentQueue GetPendingSegmentForVideo(int videoId)
        {
            var sql = "SELECT TOP 1 * FROM VideoSegmentQueue WHERE IsDone = 0 AND VideoId = @VideoId";
            using var connection = _dbConnectionFactory.OpenDefault();

            var result = connection.Query<VideoSegmentQueue>(sql, new
            {
                VideoId = videoId
            }).FirstOrDefault();

            return result;
        }

        public List<VideoSegmentQueueItem> GetItemsForSegmenterJob(int jobId)
        {
            var sql = "SELECT * FROM VideoSegmentQueueItem WHERE VideoSegmentQueueId = @JobId";
            using var connection = _dbConnectionFactory.OpenDefault();

            var result = connection.Query<VideoSegmentQueueItem>(sql, new
            {
                JobId = jobId
            });

            return result.ToList();
        }

        public VideoEncodeQueue GetNextPendingEncode()
        {
            var sql = "SELECT TOP 1 * FROM VideoEncodeQueue WHERE IsDone = 0 ORDER BY videoID ASC, MaxHeight DESC";
            using var connection = _dbConnectionFactory.OpenDefault();

            var result = connection.Query<VideoEncodeQueue>(sql).FirstOrDefault();

            return result;
        }

        public VideoSegmentQueue GetNextPendingSegment()
        {
            var sql = "SELECT TOP 1 * FROM VideoSegmentQueue WHERE IsDone = 0";
            using var connection = _dbConnectionFactory.OpenDefault();

            var result = connection.Query<VideoSegmentQueue>(sql).FirstOrDefault();
            if (result != null)
                result.VideoSegmentQueueItem = GetItemsForSegmenterJob(result.Id);

            return result;
        }




        [Obsolete]
        public VideoSource SaveVideoSource(VideoSource videoSource)
        {
            try
            {
                if (videoSource.Id == 0)
                {
                    _context.VideoSource.Add(videoSource);
                }
                else
                {
                    _context.VideoSource.Update(videoSource);
                }

                _context.SaveChanges();

                return videoSource;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [Obsolete]
        public bool SaveEncodeJob(VideoEncodeQueue encodeJob)
        {
            if (encodeJob.Id == 0)
            {
                _context.Add(encodeJob);
            }
            else
            {
                _context.Update(encodeJob);
            }

            _context.SaveChanges();

            return encodeJob.Id != 0;
        }

        [Obsolete]
        public bool SaveSegmentJob(VideoSegmentQueue segmentJob)
        {
            if (segmentJob.Id == 0)
            {
                _context.Add(segmentJob);
            }
            else
            {
                _context.Update(segmentJob);
            }

            _context.SaveChanges();

            return segmentJob.Id != 0;
        }

        [Obsolete]
        public bool SaveSegmentItem(VideoSegmentQueueItem segmentJob)
        {
            if (segmentJob.Id == 0)
            {
                _context.Add(segmentJob);
            }
            else
            {
                _context.Update(segmentJob);
            }

            _context.SaveChanges();

            return segmentJob.Id != 0;
        }


        [Obsolete]
        public bool IsSourceFileStillNeeded(int videoId, int queueItemId)
        {
            return _context.VideoEncodeQueue.Any(v =>
                v.VideoId == videoId && v.Id != queueItemId &&
                !v.IsDone);
        }

        [Obsolete]
        public void SetPendingSegmentingDone(int videoId)
        {
            var segmentsForvideo = _context.VideoSegmentQueue.Where(s => s.VideoId == videoId).ToList();

            foreach (var item in segmentsForvideo)
            {
                item.IsDone = true;
            }

            _context.UpdateRange(segmentsForvideo);

            _context.SaveChanges();
        }

        [Obsolete]
        public IQueryable<VideoSegmentQueue> GetSegmentJobsForVideo(int videoId)
        {
            return _context.VideoSegmentQueue.Where(q => q.VideoId == videoId);
        }

        public List<VideoEncodeQueue> GetSimilarEncodeJobs(VideoEncodeQueue queueItem)
        {
            var sql = "SELECT * FROM VideoEncodeQueue WHERE Id != @Id AND IsDone = 0 AND VideoId = @VideoId AND Quality = @Quality AND MaxHeight = @MaxHeight AND RenderSpeed = @RenderSpeed AND Encoder = @Encoder";
            using var connection = _dbConnectionFactory.OpenDefault();

            var result = connection.Query<VideoEncodeQueue>(sql, new {
                queueItem.Id,
                queueItem.VideoId,
                queueItem.Quality,
                queueItem.MaxHeight,
                queueItem.RenderSpeed,
                queueItem.Encoder,
            });

            return result.ToList();
        }

        public virtual bool DeleteVideo(int id)
        {
            Video video = GetVideo(id);

            _context.Video.Remove(video);

            _context.SaveChanges();

            return true;
        }

        public Video GetVideo(int id)
        {
            return GetAllVideos().Include(x => x.VideoSource).Include(x => x.VideoTag).ThenInclude(x => x.Tag).Include(x => x.VideoEncodeQueue).Include(x => x.VideoSegmentQueue).Include(x => x.VideoSegmentQueueItem).FirstOrDefault(x => x.Id == id);
        }

        public IQueryable<Video> GetAllVideos()
        {
            return _context.Video.Include(v => v.VideoSource);
        }

        public IQueryable<Tag> DefineTags(List<string> tags)
        {
            try
            {
                var existingTags = _context.Tag.Select(x => x.Name.ToLower());

                foreach (var unsafeTag in tags)
                {
                    string tag = unsafeTag.Trim().ToLower();
                    if (!existingTags.Contains(tag))
                    {
                        _context.Tag.Add(new Tag() { Name = tag, Type = 1 });
                    }
                }
                _context.SaveChanges();

                var tagsInDatabase = _context.Tag.Where(x => tags.Contains(x.Name));
                return tagsInDatabase;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public Video SaveVideo(Video video)
        {
            try
            {
                if (video.Id == 0)
                {
                    _context.Video.Add(video);
                }
                else
                {
                    _context.Video.Update(video);
                }

                var updateCount = _context.SaveChanges();

                return video;
            }
            catch (Exception ex)
            {
                var test = ex.Message;
                throw ex;
            }
        }

        public IEnumerable<string> GetAllTags()
        {
            var sql = "SELECT Name FROM Tag";
            using var connection = _dbConnectionFactory.OpenDefault();

            var result = connection.Query<string>(sql);

            return result.ToList();
        }
    }
}
