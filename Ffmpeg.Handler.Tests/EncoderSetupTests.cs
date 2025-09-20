using Common;
using Common.Entities;
using Database.Models;
using Handbrake.Handler;
using Microsoft.Extensions.Options;
using NSubstitute;
using Serilog;

namespace Ffmpeg.Handler.Tests
{
    internal class EncoderSetupTests
    {
        [Test]
        public async Task TestEndicerTest()
        {
            // GIVEN

            var componentOptions = new ComponentOptions() { Handbrake = "C:\\Utilities\\handbrakecli\\handbrakecli.exe" };
            IOptions<ComponentOptions> options = Options.Create(componentOptions);
            var logger = Substitute.For<ILogger>();
            var handbrakeWrapper = Substitute.For<ITranscodeWrapper>();

            var config = new CatalogImportOptions {
            ImportDirectory = "",
            };
            var dbQueueItem = new VideoEncodeQueue
            {
                MaxHeight = 720,
                IsVertical = true,
                Encoder = "x264",
                VideoFormat = "av_mp4",
                Quality = 20,
                OutputDirectory = "K:\\",
                InputDirectory = "K:\\OpenVid Bucket\\video\\73\\73c137e0e15d118a0c0c65ce7d27bdb9.mp4"
            };
            var jobContext = new EncodeJobContext(config, dbQueueItem)
            {
                SourceHeight = 1920,
                SourceWidth = 1080
            };

            var encoder = new HandbrakeLibraryEncoder(options, logger, handbrakeWrapper);

            // Act
            var result = await encoder.Execute(jobContext);

            // Assert
            await handbrakeWrapper.Received(1).RunTranscode(
                Arg.Is<CustomHandbrakeConfiguration>(cfg =>
                    cfg.MaxHeight == 720 && cfg.MaxWidth == 1280),
                Arg.Any<EncodeJobContext>()
            );

            Assert.True(result);
        }

        [Test]
        [TestCase(720, 1920, 1080, 1280, 720)]
        [TestCase(720, 1080, 1920, 720, 1280)]
        [TestCase(720, 2224, 1080, 1280, 622)]
        public async Task GetDimensionsTest1(int targetResolution, int sourceWidth, int sourceHeight, int intendedWidth, int intendedHeight)
        {
            // GIVEN

            var logger = Substitute.For<ILogger>();
            var handbrakeWrapper = Substitute.For<ITranscodeWrapper>();
            var componentOptions = new ComponentOptions() { Handbrake = "C:\\Utilities\\handbrakecli\\handbrakecli.exe" };
            IOptions<ComponentOptions> options = Options.Create(componentOptions);

            var encoder = new HandbrakeLibraryEncoder(options, logger, handbrakeWrapper);

            // Act
            encoder.GetDimensions(
                targetResolution, 
                sourceWidth, 
                sourceHeight, 
                sourceWidth / sourceHeight, 
                sourceWidth > sourceHeight,
                out int finalWidith,
                out int finalHeight);

            // Assert
            Assert.AreEqual(intendedWidth, finalWidith);
            Assert.AreEqual(intendedHeight, finalHeight);
        }
    }
}
