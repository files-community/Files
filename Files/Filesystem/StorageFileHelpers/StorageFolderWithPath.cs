using Files.Filesystem.StorageItems;
using Windows.Storage;
using Windows.Storage.Search;

namespace Files.Filesystem
{
    public class StorageFolderWithPath : IStorageItemWithPath
    {
        public BaseStorageFolder Folder
        {
            get
            {
                return (BaseStorageFolder)Item;
            }
            set
            {
                Item = value;
            }
        }

        public string Path { get; set; }
        public IStorageItem Item { get; set; }
        public FilesystemItemType ItemType => FilesystemItemType.Directory;

        public StorageFolderWithPath(BaseStorageFolder folder)
        {
            Folder = folder;
            Path = folder.Path;
        }

        public StorageFolderWithPath(BaseStorageFolder folder, string path)
        {
            Folder = folder;
            Path = path;
        }
    }
}