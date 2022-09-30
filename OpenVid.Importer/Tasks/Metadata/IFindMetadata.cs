using OpenVid.Importer.Models;

namespace OpenVid.Importer.Tasks.Metadata
{
    public interface IFindMetadata
    {
        VideoMetadata Execute(string location);
    }
}
