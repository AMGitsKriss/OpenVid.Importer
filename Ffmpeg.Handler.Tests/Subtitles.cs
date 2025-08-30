using Moq;
using Serilog;

namespace Ffmpeg.Handler.Tests
{
    public class SubtitleTests
    {
        [SetUp]
        public void Setup()
        {
        }
        /*
        [Test]
        public async Task Test1()
        {
            var meta = new MetadataExtractor(new Mock<ILogger>().Object);
            var subs = new SubtitleExtractor(new Mock<ILogger>().Object);

            var metadata = await meta.Extract("");
            await subs.SaveSubtitles(metadata, "", "");

            Assert.Pass();
        }*/
    }
}