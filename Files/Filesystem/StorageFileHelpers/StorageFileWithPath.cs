using Files.Filesystem.StorageItems;
using Windows.Storage;

namespace Files.Filesystem
{
    public class StorageFileWithPath : IStorageItemWithPath
    {
        public BaseStorageFile File
        {
            get
            {
                return (BaseStorageFile)Item;
            }
            set
            {
                Item = value;
            }
        }

        public string Path { get; set; }
        public IStorageItem Item { get; set; }
        public FilesystemItemType ItemType => FilesystemItemType.File;

        public StorageFileWithPath(BaseStorageFile file)
        {
            File = file;
            Path = File.Path;
        }

        public StorageFileWithPath(BaseStorageFile file, string path)
        {
            File = file;
            Path = path;
        }
    }
}