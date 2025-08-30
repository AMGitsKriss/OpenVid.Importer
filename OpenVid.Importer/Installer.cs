using CatalogManager.Segment;
using Common;
using Database;
using Database.Models;
using Ffmpeg.Handler;
using Handbrake.Handler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenVid.Importer.Tasks.AudioTracks;
using OpenVid.Importer.Tasks.Ingest;
using OpenVid.Importer.Tasks.Thumbnails;
using Serilog;
using Serilog.Events;
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
                .AddScoped<HandbrakeHandler>()
                .AddScoped<AudioContainer>()
                .AddScoped<SegmenterContainer>()

                .AddScoped<IFindAudioTracks, ShakaPackagerFindAudioTracks>()
                .AddScoped<MetadataExtractor>()
                .AddScoped<IGenerateThumbnails, FfmpegGenerateThumbnails>()
                .AddScoped<IEncoder, HandbrakeLibraryEncoder>()
                .AddScoped<ISegmenter, ShakaPackagerSegmenter>()
                .AddScoped<SubtitleExtractor>()
                .AddScoped<IngestService>()

                .Configure<ConnectionStringOptions>(configuration.GetSection("ConnectionStrings"))
                .Configure<CatalogImportOptions>(configuration.GetSection("Catalog"))
                .AddDbContext<OpenVidContext>(o => o.UseSqlServer(configuration.GetConnectionString("DefaultDatabase")))
                .AddScoped<IDbConnectionFactory, DbConnectionFactory>()
                .AddScoped<IVideoRepository, VideoRepository>()
                .AddSerilog(configuration)
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

        public static IServiceCollection AddSerilog(this IServiceCollection services, IConfigurationRoot configuration)
        {
            var loggerConfig = new LoggerConfiguration();
            loggerConfig.WriteTo.Seq("http://localhost:5341/", apiKey: "lAr93RAGSr1iILEr1Kta");
            loggerConfig.WriteTo.Console();
            loggerConfig.MinimumLevel.Override("Microsoft", LogEventLevel.Error);
            loggerConfig.MinimumLevel.Override("System", LogEventLevel.Error);
            loggerConfig.Enrich.WithProperty("Application", configuration["Logging:Application:Name"] ?? "OpenVid.Importer");
            loggerConfig.Enrich.FromLogContext();

            ILogger logger = loggerConfig.CreateLogger();

            services.AddSingleton(logger);

            return services;
        }
    }
}
