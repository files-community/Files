using Windows.Storage;

namespace Files.Filesystem
{
    public class StorageFolderWithPath : IStorageItemWithPath
    {
        public StorageFolder Folder
        {
            get
            {
                return (StorageFolder)Item;
            }
            set
            {
                Item = value;
            }
        }

        public string Path { get; set; }
        public IStorageItem Item { get; set; }
        public FilesystemItemType ItemType => FilesystemItemType.Directory;

        public StorageFolderWithPath(StorageFolder folder)
        {
            Folder = folder;
            Path = folder.Path;
        }

        public StorageFolderWithPath(StorageFolder folder, string path)
        {
            Folder = folder;
            Path = path;
        }
    }
}