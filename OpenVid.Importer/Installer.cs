using CatalogManager.Segment;
using Database;
using Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenVid.Importer.Models;
using OpenVid.Importer.Tasks.AudioTracks;
using OpenVid.Importer.Tasks.Encoder;
using OpenVid.Importer.Tasks.Metadata;
using OpenVid.Importer.Tasks.Thumbnails;
using System;
namespace OpenVid.Importer
{
    public static class Installer
    {
        public static ServiceProvider LoadServiceCollection()
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

        public static IConfigurationRoot LoadConfiguration()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var configFileName = environment == null ? "appsettings.json" : $"appsettings.{environment}.json";
            var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile(configFileName, optional: false, reloadOnChange: true);

            return builder.Build();

        }
    }
}
