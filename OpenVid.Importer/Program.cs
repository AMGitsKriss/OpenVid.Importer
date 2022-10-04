using CatalogManager.Segment;
using Database;
using Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenVid.Importer.Entities;
using OpenVid.Importer.Tasks.AudioTracks;
using OpenVid.Importer.Tasks.Encoder;
using OpenVid.Importer.Tasks.Metadata;
using OpenVid.Importer.Tasks.Thumbnails;
using System;
using System.Linq;

namespace OpenVid.Importer
{
    class Program
    {
        private static IVideoRepository _repository;
        private static EncoderContainer _encoderService;
        private static AudioContainer _audioService;
        private static SegmenterContainer _segmenter;

        static void Main(string[] args)
        {
            var serviceProvider = LoadServiceCollection();
            _repository = serviceProvider.GetService<IVideoRepository>();
            _encoderService = serviceProvider.GetService<EncoderContainer>();
            _audioService = serviceProvider.GetService<AudioContainer>();
            _segmenter = serviceProvider.GetService<SegmenterContainer>();

            VideoEncodeQueue pendingEncodeJob;
            while ((pendingEncodeJob = _repository.GetNextPendingEncode()) != null)
            {
                EncodeJobContext context = new EncodeJobContext(null, pendingEncodeJob);
                _encoderService.Run(pendingEncodeJob);
            }

            VideoSegmentQueue pendingSegmentJob;
            while ((pendingSegmentJob = _repository.GetNextPendingSegment()) != null)
            {
                if (!pendingSegmentJob.VideoSegmentQueueItem.Any(i => i.ArgStream == "audio"))
                {
                    _audioService.Run(pendingSegmentJob);
                    pendingSegmentJob = _repository.GetNextPendingSegment(); // Refresh the object
                }

                _segmenter.Run(pendingSegmentJob);
            }

            /*
            var pendingEncodeJob = repository.GetNextPendingEncode();
            if (pendingEncodeJob != null)
            {
                var encoderService = serviceProvider.GetService<EncoderContainer>();
                encoderService.Run(pendingEncodeJob);

                if (pendingEncodeJob.PlaybackFormat == "dash")
                {
                    var pendingSegmentJob = repository.GetPendingSegmentForVideo(pendingEncodeJob.VideoId);
                    if (pendingSegmentJob != null)
                    {
                        pendingSegmentJob.VideoSegmentQueueItem = repository.GetItemsForSegmenterJob(pendingSegmentJob.Id);

                        if (!pendingSegmentJob.VideoSegmentQueueItem.Any(i => i.ArgStream == "audio"))
                        {
                            var audioService = serviceProvider.GetService<AudioContainer>();
                            audioService.Run(pendingSegmentJob);
                        }

                        var segmenter = serviceProvider.GetService<SegmenterContainer>();
                        segmenter.Run(pendingSegmentJob);
                    }
                }
            }
            */
        }

        static ServiceProvider LoadServiceCollection()
        {
            var configuration = LoadConfiguration();
            return new ServiceCollection()
                .AddOptions()
                .AddScoped<EncoderContainer>()
                .AddScoped<AudioContainer>()
                .AddScoped<SegmenterContainer>()

                .AddScoped<IFindAudioTracks, ShakaPackagerFindAudioTracks>()
                //.AddScoped<IFindAudioTracks, FfmpegFindAudioTracks>()
                .AddScoped<IFindMetadata, FfmpegFindMetadata>()
                .AddScoped<IGenerateThumbnails, FfmpegGenerateThumbnails>()
                .AddScoped<IEncoder, HandbrakeEncoder>()
                .AddScoped<ISegmenter, ShakaPackagerSegmenter>()

                .Configure<ConnectionStringOptions>(configuration.GetSection("ConnectionStrings"))
                .Configure<CatalogImportOptions>(configuration.GetSection("Catalog"))
                .AddDbContext<OpenVidContext>(o => o.UseSqlServer(configuration.GetConnectionString("DefaultDatabase")))
                .AddScoped<IDbConnectionFactory, DbConnectionFactory>()
                .AddScoped<IVideoRepository, VideoRepository>()

                .BuildServiceProvider();
        }

        static IConfigurationRoot LoadConfiguration()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            return builder.Build();

        }
    }
}
