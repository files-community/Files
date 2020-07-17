using Windows.Storage;

namespace Files.Filesystem
{
    public interface IStorageItemWithPath
    {
        public string Path { get; set; }
        public IStorageItem Item { get; set; }
    }
}