using OpenVid.Importer.Entities;

namespace OpenVid.Importer.Tasks.Encoder
{
    public interface IEncoder
    {
        void Execute(EncodeJobContext queueItem);
    }
}