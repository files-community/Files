using Windows.Storage;

namespace Files.Filesystem
{
    public class StorageFileWithPath : IStorageItemWithPath
    {
        public StorageFileWithPath(StorageFile file)
        {
            File = file;
            Path = File.Path;
        }

        public StorageFileWithPath(StorageFile file, string path)
        {
            File = file;
            Path = path;
        }

        public StorageFile File
        {
            get
            {
                return (StorageFile)Item;
            }
            set
            {
                Item = value;
            }
        }

        public IStorageItem Item { get; set; }
        public FilesystemItemType ItemType => FilesystemItemType.File;
        public string Path { get; set; }
    }
}