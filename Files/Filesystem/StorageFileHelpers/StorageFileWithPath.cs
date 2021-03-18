using System;
using System.Runtime.Serialization;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Files.Filesystem
{
    public class StorageFileWithPath : IStorageItem
    {
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

        public string Path { get; set; }
        public IStorageItem Item { get; set; }
        public FilesystemItemType ItemType => FilesystemItemType.File;

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

        public IAsyncAction RenameAsync(string desiredName) => File.RenameAsync(desiredName);
        public IAsyncAction RenameAsync(string desiredName, NameCollisionOption option) => File.RenameAsync(desiredName, option);
        public IAsyncAction DeleteAsync() => File.DeleteAsync();
        public IAsyncAction DeleteAsync(StorageDeleteOption option) => File.DeleteAsync(option);
        public IAsyncOperation<BasicProperties> GetBasicPropertiesAsync() => File.GetBasicPropertiesAsync();
        public bool IsOfType(StorageItemTypes type) => File.IsOfType(type);

        public FileAttributes Attributes => File.Attributes;

        public DateTimeOffset DateCreated => File.DateCreated;

        public string Name => File.Name;
    }
}