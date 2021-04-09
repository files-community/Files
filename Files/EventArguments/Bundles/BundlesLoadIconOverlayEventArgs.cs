namespace Files.EventArguments.Bundles
{
    public class BundlesLoadIconOverlayEventArgs
    {
        public readonly string path;

        public readonly uint thumbnailSize;

        public (byte[] IconData, byte[] OverlayData, bool IsCustom) outData;

        public BundlesLoadIconOverlayEventArgs(string path, uint thumbnailSize)
        {
            this.path = path;
            this.thumbnailSize = thumbnailSize;
        }
    }
}