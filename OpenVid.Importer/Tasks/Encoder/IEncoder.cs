using Database.Models;

namespace OpenVid.Importer.Tasks.Encoder
{
    public interface IEncoder
    {
        void Execute(VideoEncodeQueue queueItem);
    }
}