using Windows.Storage;

namespace Files.Filesystem
{
    public interface IStorageItemWithPath
    {
        public string Name { get; }
        public string Path { get; }

        public IStorageItem Item { get; }
        public FilesystemItemType ItemType { get; }
    }
}
