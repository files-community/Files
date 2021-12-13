using Files.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.Storage.Streams;

namespace Files.Filesystem.StorageItems
{
    public interface IBaseStorageFolder : IStorageFolder, IStorageItem, IStorageFolderQueryOperations, IStorageItemProperties, IStorageItemProperties2, IStorageItem2, IStorageFolder2, IStorageItemPropertiesWithProvider
    {
        new IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName);
        new IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options);
        new IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName);
        new IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName, CreationCollisionOption options);
        new IAsyncOperation<BaseStorageFile> GetFileAsync(string name);
        new IAsyncOperation<BaseStorageFolder> GetFolderAsync(string name);
        new IAsyncOperation<IStorageItem> GetItemAsync(string name);
        new IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync();
        new IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync();
        new IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync();

        new IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(CommonFileQuery query, uint startIndex, uint maxItemsToRetrieve);
        new IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(CommonFileQuery query);
        new IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(CommonFolderQuery query, uint startIndex, uint maxItemsToRetrieve);
        new IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(CommonFolderQuery query);

        new BaseStorageFileQueryResult CreateFileQueryWithOptions(QueryOptions queryOptions);
        new BaseStorageFolderQueryResult CreateFolderQueryWithOptions(QueryOptions queryOptions);
        new BaseStorageItemQueryResult CreateItemQueryWithOptions(QueryOptions queryOptions);

        new IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync();
        new IStorageItemExtraProperties Properties { get; }

        new IAsyncOperation<BaseStorageFolder> GetParentAsync();

        IAsyncOperation<StorageFolder> ToStorageFolderAsync();
    }

    public interface IBaseStorageFile : IStorageFile, IInputStreamReference, IRandomAccessStreamReference, IStorageItem, IStorageItemProperties, IStorageItemProperties2, IStorageItem2, IStorageItemPropertiesWithProvider, IStorageFilePropertiesWithAvailability, IStorageFile2
    {
        new IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder);
        new IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName);
        new IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option);

        new IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync();
        new IStorageItemExtraProperties Properties { get; }

        new IAsyncOperation<BaseStorageFolder> GetParentAsync();

        IAsyncOperation<StorageFile> ToStorageFileAsync();
    }

    public class BaseStorageItemExtraProperties : IStorageItemExtraProperties
    {
        public virtual IAsyncOperation<IDictionary<string, object>> RetrievePropertiesAsync(IEnumerable<string> propertiesToRetrieve)
        {
            return AsyncInfo.Run<IDictionary<string, object>>((cancellationToken) =>
            {
                var props = new Dictionary<string, object>();
                propertiesToRetrieve.ForEach(x => props[x] = null);
                return Task.FromResult<IDictionary<string, object>>(props);
            });
        }

        public virtual IAsyncAction SavePropertiesAsync([HasVariant] IEnumerable<KeyValuePair<string, object>> propertiesToSave)
        {
            return Task.CompletedTask.AsAsyncAction();
        }

        public virtual IAsyncAction SavePropertiesAsync()
        {
            return Task.CompletedTask.AsAsyncAction();
        }
    }

    public class BaseBasicStorageItemExtraProperties : BaseStorageItemExtraProperties
    {
        private IStorageItem item;

        public BaseBasicStorageItemExtraProperties(IStorageItem item)
        {
            this.item = item;
        }

        public override IAsyncOperation<IDictionary<string, object>> RetrievePropertiesAsync(IEnumerable<string> propertiesToRetrieve)
        {
            return AsyncInfo.Run<IDictionary<string, object>>(async (cancellationToken) =>
            {
                var props = new Dictionary<string, object>();
                propertiesToRetrieve.ForEach(x => props[x] = null);
                // Fill common poperties
                var ret = item.AsBaseStorageFile()?.GetBasicPropertiesAsync() ?? item.AsBaseStorageFolder()?.GetBasicPropertiesAsync();
                var basicProps = ret != null ? await ret : null;
                props["System.ItemPathDisplay"] = item?.Path;
                props["System.DateCreated"] = basicProps?.ItemDate;
                props["System.DateModified"] = basicProps?.DateModified;
                return props;
            });
        }
    }

    public class BaseBasicProperties : BaseStorageItemExtraProperties
    {
        public virtual DateTimeOffset DateModified { get => DateTimeOffset.Now; }
        public virtual DateTimeOffset ItemDate { get => DateTimeOffset.Now; }
        public virtual ulong Size { get => 0; }
    }

    public abstract class BaseStorageFolder : IBaseStorageFolder
    {
        public static implicit operator BaseStorageFolder(StorageFolder value) => value != null ? new SystemStorageFolder(value) : null;

        public abstract IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName);
        public abstract IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options);
        public abstract IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName);
        public abstract IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName, CreationCollisionOption options);
        public abstract IAsyncOperation<BaseStorageFile> GetFileAsync(string name);
        public abstract IAsyncOperation<BaseStorageFolder> GetFolderAsync(string name);
        public abstract IAsyncOperation<IStorageItem> GetItemAsync(string name);
        public abstract IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync();
        public abstract IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync();
        public abstract IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync();
        public abstract IAsyncOperation<StorageFolder> ToStorageFolderAsync();
        public abstract IAsyncAction RenameAsync(string desiredName);
        public abstract IAsyncAction RenameAsync(string desiredName, NameCollisionOption option);
        public abstract IAsyncAction DeleteAsync();
        public abstract IAsyncAction DeleteAsync(StorageDeleteOption option);
        public abstract IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync();
        public abstract bool IsOfType(StorageItemTypes type);

        public abstract FileAttributes Attributes { get; }
        public abstract DateTimeOffset DateCreated { get; }
        public abstract string Name { get; }
        public abstract string Path { get; }

        public abstract IAsyncOperation<IndexedState> GetIndexedStateAsync();
        public abstract StorageFileQueryResult CreateFileQuery();
        public abstract StorageFileQueryResult CreateFileQuery(CommonFileQuery query);
        public abstract BaseStorageFileQueryResult CreateFileQueryWithOptions(QueryOptions queryOptions);
        public abstract StorageFolderQueryResult CreateFolderQuery();
        public abstract StorageFolderQueryResult CreateFolderQuery(CommonFolderQuery query);
        public abstract BaseStorageFolderQueryResult CreateFolderQueryWithOptions(QueryOptions queryOptions);
        public abstract StorageItemQueryResult CreateItemQuery();
        public abstract BaseStorageItemQueryResult CreateItemQueryWithOptions(QueryOptions queryOptions);
        public abstract IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(CommonFileQuery query, uint startIndex, uint maxItemsToRetrieve);
        public abstract IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(CommonFileQuery query);
        public abstract IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(CommonFolderQuery query, uint startIndex, uint maxItemsToRetrieve);

        public abstract IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(CommonFolderQuery query);
        public abstract IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync(uint startIndex, uint maxItemsToRetrieve);
        public abstract bool AreQueryOptionsSupported(QueryOptions queryOptions);
        public abstract bool IsCommonFolderQuerySupported(CommonFolderQuery query);
        public abstract bool IsCommonFileQuerySupported(CommonFileQuery query);
        public abstract IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode);
        public abstract IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize);
        public abstract IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options);

        public abstract string DisplayName { get; }
        public abstract string DisplayType { get; }
        public abstract string FolderRelativeId { get; }
        public abstract IStorageItemExtraProperties Properties { get; }

        IAsyncOperation<StorageFile> IStorageFolder.CreateFileAsync(string desiredName)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                return await (await CreateFileAsync(desiredName)).ToStorageFileAsync();
            });
        }

        IAsyncOperation<StorageFile> IStorageFolder.CreateFileAsync(string desiredName, CreationCollisionOption options)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                return await (await CreateFileAsync(desiredName, options)).ToStorageFileAsync();
            });
        }

        IAsyncOperation<StorageFolder> IStorageFolder.CreateFolderAsync(string desiredName)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                return await (await CreateFolderAsync(desiredName)).ToStorageFolderAsync();
            });
        }

        IAsyncOperation<StorageFolder> IStorageFolder.CreateFolderAsync(string desiredName, CreationCollisionOption options)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                return await (await CreateFolderAsync(desiredName, options)).ToStorageFolderAsync();
            });
        }

        IAsyncOperation<StorageFile> IStorageFolder.GetFileAsync(string name)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                return await (await GetFileAsync(name)).ToStorageFileAsync();
            });
        }

        IAsyncOperation<StorageFolder> IStorageFolder.GetFolderAsync(string name)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                return await (await GetFolderAsync(name)).ToStorageFolderAsync();
            });
        }

        IAsyncOperation<IReadOnlyList<StorageFile>> IStorageFolder.GetFilesAsync()
        {
            return AsyncInfo.Run<IReadOnlyList<StorageFile>>(async (cancellationToken) =>
            {
                return await Task.WhenAll((await GetFilesAsync()).Select(x => x.ToStorageFileAsync().AsTask()));
            });
        }

        IAsyncOperation<IReadOnlyList<StorageFolder>> IStorageFolder.GetFoldersAsync()
        {
            return AsyncInfo.Run<IReadOnlyList<StorageFolder>>(async (cancellationToken) =>
            {
                return await Task.WhenAll((await GetFoldersAsync()).Select(x => x.ToStorageFolderAsync().AsTask()));
            });
        }

        public IAsyncOperation<StorageItemThumbnail> GetScaledImageAsThumbnailAsync(ThumbnailMode mode)
        {
            return Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
        }

        public IAsyncOperation<StorageItemThumbnail> GetScaledImageAsThumbnailAsync(ThumbnailMode mode, uint requestedSize)
        {
            return Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
        }

        public IAsyncOperation<StorageItemThumbnail> GetScaledImageAsThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
        {
            return Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
        }

        public abstract IAsyncOperation<BaseStorageFolder> GetParentAsync();
        public abstract bool IsEqual(IStorageItem item);
        public abstract IAsyncOperation<IStorageItem> TryGetItemAsync(string name);

        public StorageProvider Provider => null;

        IAsyncOperation<StorageFolder> IStorageItem2.GetParentAsync()
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                return await (await GetParentAsync()).ToStorageFolderAsync();
            });
        }

        public static IAsyncOperation<BaseStorageFolder> GetFolderFromPathAsync(string path)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                BaseStorageFolder folder = null;
                folder ??= await ZipStorageFolder.FromPathAsync(path);
                folder ??= await FtpStorageFolder.FromPathAsync(path);
                folder ??= await SystemStorageFolder.FromPathAsync(path);
                return folder;
            });
        }

        IAsyncOperation<IReadOnlyList<StorageFile>> IStorageFolderQueryOperations.GetFilesAsync(CommonFileQuery query, uint startIndex, uint maxItemsToRetrieve)
        {
            return AsyncInfo.Run<IReadOnlyList<StorageFile>>(async (cancellationToken) =>
            {
                return await Task.WhenAll((await GetFilesAsync(query, startIndex, maxItemsToRetrieve)).Select(x => x.ToStorageFileAsync().AsTask()));
            });
        }

        IAsyncOperation<IReadOnlyList<StorageFile>> IStorageFolderQueryOperations.GetFilesAsync(CommonFileQuery query)
        {
            return AsyncInfo.Run<IReadOnlyList<StorageFile>>(async (cancellationToken) =>
            {
                return await Task.WhenAll((await GetFilesAsync(query)).Select(x => x.ToStorageFileAsync().AsTask()));
            });
        }

        IAsyncOperation<IReadOnlyList<StorageFolder>> IStorageFolderQueryOperations.GetFoldersAsync(CommonFolderQuery query, uint startIndex, uint maxItemsToRetrieve)
        {
            return AsyncInfo.Run<IReadOnlyList<StorageFolder>>(async (cancellationToken) =>
            {
                return await Task.WhenAll((await GetFoldersAsync(query, startIndex, maxItemsToRetrieve)).Select(x => x.ToStorageFolderAsync().AsTask()));
            });
        }

        IAsyncOperation<IReadOnlyList<StorageFolder>> IStorageFolderQueryOperations.GetFoldersAsync(CommonFolderQuery query)
        {
            return AsyncInfo.Run<IReadOnlyList<StorageFolder>>(async (cancellationToken) =>
            {
                return await Task.WhenAll((await GetFoldersAsync(query)).Select(x => x.ToStorageFolderAsync().AsTask()));
            });
        }

        StorageFileQueryResult IStorageFolderQueryOperations.CreateFileQueryWithOptions(QueryOptions queryOptions) => throw new NotSupportedException();
        StorageFolderQueryResult IStorageFolderQueryOperations.CreateFolderQueryWithOptions(QueryOptions queryOptions) => throw new NotSupportedException();
        StorageItemQueryResult IStorageFolderQueryOperations.CreateItemQueryWithOptions(QueryOptions queryOptions) => throw new NotSupportedException();

        IAsyncOperation<BasicProperties> IStorageItem.GetBasicPropertiesAsync()
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                var item = await ToStorageFolderAsync();
                return await item.GetBasicPropertiesAsync();
            });
        }

        StorageItemContentProperties IStorageItemProperties.Properties
        {
            get
            {
                if (this is SystemStorageFolder sysFolder)
                {
                    return sysFolder.Folder.Properties;
                }
                return null;
            }
        }
    }

    public abstract class BaseStorageFile : IBaseStorageFile
    {
        public static implicit operator BaseStorageFile(StorageFile value) => value != null ? new SystemStorageFile(value) : null;

        public abstract IAsyncOperation<StorageFile> ToStorageFileAsync();
        public abstract IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode);
        public abstract IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync();
        public abstract IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder);
        public abstract IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName);
        public abstract IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option);
        public abstract IAsyncAction CopyAndReplaceAsync(IStorageFile fileToReplace);
        public abstract IAsyncAction MoveAsync(IStorageFolder destinationFolder);
        public abstract IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName);
        public abstract IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option);
        public abstract IAsyncAction MoveAndReplaceAsync(IStorageFile fileToReplace);

        public abstract string ContentType { get; }
        public abstract string FileType { get; }

        public abstract IAsyncAction RenameAsync(string desiredName);
        public abstract IAsyncAction RenameAsync(string desiredName, NameCollisionOption option);
        public abstract IAsyncAction DeleteAsync();
        public abstract IAsyncAction DeleteAsync(StorageDeleteOption option);
        public abstract IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync();
        public abstract bool IsOfType(StorageItemTypes type);

        public abstract FileAttributes Attributes { get; }
        public abstract DateTimeOffset DateCreated { get; }
        public abstract string Name { get; }
        public abstract string Path { get; }

        public abstract IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync();
        public abstract IAsyncOperation<IInputStream> OpenSequentialReadAsync();
        public abstract IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode);
        public abstract IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize);
        public abstract IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options);

        public abstract string DisplayName { get; }
        public abstract string DisplayType { get; }
        public abstract string FolderRelativeId { get; }
        public abstract IStorageItemExtraProperties Properties { get; }

        IAsyncOperation<StorageFile> IStorageFile.CopyAsync(IStorageFolder destinationFolder)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                return await (await CopyAsync(destinationFolder)).ToStorageFileAsync();
            });
        }

        IAsyncOperation<StorageFile> IStorageFile.CopyAsync(IStorageFolder destinationFolder, string desiredNewName)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                return await (await CopyAsync(destinationFolder, desiredNewName)).ToStorageFileAsync();
            });
        }

        IAsyncOperation<StorageFile> IStorageFile.CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                return await (await CopyAsync(destinationFolder, desiredNewName, option)).ToStorageFileAsync();
            });
        }

        public IAsyncOperation<StorageItemThumbnail> GetScaledImageAsThumbnailAsync(ThumbnailMode mode)
        {
            return Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
        }

        public IAsyncOperation<StorageItemThumbnail> GetScaledImageAsThumbnailAsync(ThumbnailMode mode, uint requestedSize)
        {
            return Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
        }

        public IAsyncOperation<StorageItemThumbnail> GetScaledImageAsThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
        {
            return Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
        }

        public abstract IAsyncOperation<BaseStorageFolder> GetParentAsync();
        public abstract bool IsEqual(IStorageItem item);

        public StorageProvider Provider => null;

        public bool IsAvailable => true;

        public abstract IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode, StorageOpenOptions options);
        public abstract IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync(StorageOpenOptions options);

        IAsyncOperation<StorageFolder> IStorageItem2.GetParentAsync()
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                return await (await GetParentAsync()).ToStorageFolderAsync();
            });
        }

        public static IAsyncOperation<BaseStorageFile> GetFileFromPathAsync(string path)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                BaseStorageFile file = null;
                file ??= await ZipStorageFile.FromPathAsync(path);
                file ??= await FtpStorageFile.FromPathAsync(path);
                file ??= await SystemStorageFile.FromPathAsync(path);
                return file;
            });
        }

        IAsyncOperation<BasicProperties> IStorageItem.GetBasicPropertiesAsync()
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                var item = await ToStorageFileAsync();
                return await item.GetBasicPropertiesAsync();
            });
        }

        StorageItemContentProperties IStorageItemProperties.Properties
        {
            get
            {
                if (this is SystemStorageFile sysFile)
                {
                    return sysFile.File.Properties;
                }
                return null;
            }
        }

        public async Task<string> ReadTextAsync(int maxLength = -1)
        {
            using var inputStream = await OpenSequentialReadAsync();
            using var dataReader = new DataReader(inputStream);
            StringBuilder builder = new StringBuilder();
            uint bytesRead, bytesToRead;
            do
            {
                bytesToRead = maxLength < 0 ? 4096 : (uint)Math.Min(maxLength, 4096);
                bytesRead = await dataReader.LoadAsync(bytesToRead);
                builder.Append(dataReader.ReadString(bytesRead));
            } while (bytesRead > 0);
            return builder.ToString();
        }

        public async Task WriteTextAsync(string text)
        {
            using var stream = await OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.AllowOnlyReaders);
            using var outputStream = stream.GetOutputStreamAt(0);
            using var dataWriter = new DataWriter(outputStream);
            dataWriter.WriteString(text);
            await dataWriter.StoreAsync();
            await stream.FlushAsync();
        }

        public async Task WriteBytesAsync(byte[] dataBytes)
        {
            using var stream = await OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.AllowOnlyReaders);
            using var outputStream = stream.GetOutputStreamAt(0);
            using var dataWriter = new DataWriter(outputStream);
            dataWriter.WriteBytes(dataBytes);
            await dataWriter.StoreAsync();
            await stream.FlushAsync();
        }
    }
}
