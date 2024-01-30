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
        private static HandbrakeEncoder _encoderService;
        private static AudioContainer _audioService;
        private static SegmenterContainer _segmenter;
        private static CatalogImportOptions _configuration;

        static void Main(string[] args)
        {
            SetUp();

            VideoEncodeQueue pendingEncodeJob;
            while ((pendingEncodeJob = _repository.GetNextPendingEncode()) != null)
            {
                var currentJob = new EncodeJobContext(_configuration, pendingEncodeJob);
                _encoderService.Run(currentJob);
            }

            VideoSegmentQueue pendingSegmentJob;
            while ((pendingSegmentJob = _repository.GetNextPendingSegment()) != null)
            {
                var currentSegment = new SegmentJobContext(_configuration, pendingSegmentJob);

                // not all video has audio
                if (!currentSegment.HasAudioTracks)
                {
                    _audioService.Run(currentSegment);
                    currentSegment.SegmentJob = _repository.GetNextPendingSegment(); // Refresh the object
                }

                _segmenter.Run(currentSegment);
            }
        }

        static void SetUp()
        {
            var serviceProvider = Installer.LoadServiceCollection();
            _repository = serviceProvider.GetService<IVideoRepository>();
            _encoderService = serviceProvider.GetService<HandbrakeEncoder>();
            _audioService = serviceProvider.GetService<AudioContainer>();
            _segmenter = serviceProvider.GetService<SegmenterContainer>();
            _configuration = serviceProvider.GetService<IOptions<CatalogImportOptions>>().Value;
        }
    }
}
