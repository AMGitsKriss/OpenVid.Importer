using Database;
using Database.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenVid.Importer.Entities;
using OpenVid.Importer.Models;

namespace OpenVid.Importer
{
    class Program
    {
        private static IVideoRepository _repository;
        private static EncoderContainer _encoderService;
        private static AudioContainer _audioService;
        private static SegmenterContainer _segmenter;
        private static CatalogImportOptions _configuration;

        static void Main(string[] args)
        {
            SetUp();

            VideoEncodeQueue pendingEncodeJob;
            while ((pendingEncodeJob = _repository.GetNextPendingEncode()) != null)
            {
                var jobContext = new EncodeJobContext(_configuration, pendingEncodeJob);
                _encoderService.Run(jobContext);
            }

            VideoSegmentQueue pendingSegmentJob;
            while ((pendingSegmentJob = _repository.GetNextPendingSegment()) != null)
            {
                var jobContext = new SegmentJobContext(_configuration, pendingSegmentJob);
                if (!jobContext.HasAudioTracks)
                {
                    _audioService.Run(jobContext);
                    jobContext.SegmentJob = _repository.GetNextPendingSegment(); // Refresh the object
                }

                _segmenter.Run(jobContext);
            }
        }

        static void SetUp()
        {
            var serviceProvider = Installer.LoadServiceCollection();
            _repository = serviceProvider.GetService<IVideoRepository>();
            _encoderService = serviceProvider.GetService<EncoderContainer>();
            _audioService = serviceProvider.GetService<AudioContainer>();
            _segmenter = serviceProvider.GetService<SegmenterContainer>();
            _configuration = serviceProvider.GetService<IOptions<CatalogImportOptions>>().Value;
        }
    }
}
