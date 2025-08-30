using Common;
using Common.Entities;
using Database;
using Database.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenVid.Importer.Entities;
using OpenVid.Importer.Tasks.Ingest;
using System.Threading.Tasks;

namespace OpenVid.Importer
{
    class Program
    {
        private static IVideoRepository _repository;
        private static HandbrakeHandler _encoderService;
        private static AudioContainer _audioService;
        private static SegmenterContainer _segmenter;
        private static CatalogImportOptions _configuration;
        private static IngestService _ingest;

        static async Task Main(string[] args)
        {
            SetUp();

            // TODO - Doesn't run well if the same video is imported multiple times. 

            // Step 1 - Queue Unqueued videos & pull out the subtitles
            await _ingest.IngestFiles();

            // Step 2 - Put all the pending videos through Handbrake. This will create all the configured quality videos. 
            VideoEncodeQueue pendingEncodeJob;
            while ((pendingEncodeJob = _repository.GetNextPendingEncode()) != null)
            {
                // TODO - Kill HandbrakeCLI.exe on exit
                var currentJob = new EncodeJobContext(_configuration, pendingEncodeJob);
                if (!await _encoderService.Run(currentJob))
                    break;
            }

            // Step 3 - Do video segmenting for HLS/DASH
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
            _encoderService = serviceProvider.GetService<HandbrakeHandler>();
            _audioService = serviceProvider.GetService<AudioContainer>();
            _segmenter = serviceProvider.GetService<SegmenterContainer>();
            _configuration = serviceProvider.GetService<IOptions<CatalogImportOptions>>().Value;
            _ingest = serviceProvider.GetService<IngestService>();
        }
    }
}
