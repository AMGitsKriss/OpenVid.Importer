namespace OpenVid.Importer.Tasks.Thumbnails
{
    public interface IGenerateThumbnails
    {
        void Execute(string videoPath, string thumbPath);
    }
}
