using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Files.Uwp.Filesystem.StorageItems
{
    /// <summary>
    /// Implements IStorageItem, allowing us to get an instance of IStorageItem for a ListedItem
    /// representing a standard filesystem item. As such, VirtualStorageItem does not support hidden, 
    /// shortcut, or link items.
    /// </summary>
    public class VirtualStorageItem : IStorageItem
    {
        private readonly ListedItem item;
        private readonly BasicProperties props;

        public VirtualStorageItem(ListedItem item, BasicProperties props)
        {
            this.item = item;
            this.props = props;
        }

        public IAsyncAction RenameAsync(string desiredName)
        {
            throw new NotImplementedException();
        }

        public IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
        {
            throw new NotImplementedException();
        }

        public IAsyncAction DeleteAsync()
        {
            throw new NotImplementedException();
        }

        public IAsyncAction DeleteAsync(StorageDeleteOption option)
        {
            throw new NotImplementedException();
        }

        public IAsyncOperation<BasicProperties> GetBasicPropertiesAsync()
        {
            return Task.FromResult(props).AsAsyncOperation();
        }

        public bool IsOfType(StorageItemTypes type)
        {
            return item.PrimaryItemAttribute == type;
        }

        public FileAttributes Attributes => item.PrimaryItemAttribute == StorageItemTypes.File ? FileAttributes.Normal : FileAttributes.Directory;

        public DateTimeOffset DateCreated => item.ItemDateCreatedReal;

        public string Name => item.ItemName;

        public string Path => item.ItemPath;
    }
}
