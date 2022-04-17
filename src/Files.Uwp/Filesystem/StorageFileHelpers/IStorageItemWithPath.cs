using Windows.Storage;

namespace Files.Uwp.Filesystem
{
    public interface IStorageItemWithPath
    {
        public string Name { get; }
        public string Path { get; }

        public IStorageItem Item { get; }
        public FilesystemItemType ItemType { get; }
    }
}
