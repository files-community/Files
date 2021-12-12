using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;

namespace Files.Filesystem.StorageItems
{
    public sealed class SystemStorageFolder : BaseStorageFolder
    {
        public StorageFolder Folder { get; }

        public SystemStorageFolder(StorageFolder folder)
        {
            Folder = folder;
        }

        public override IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName)
        {
            return AsyncInfo.Run<BaseStorageFile>(async (cancellationToken) =>
            {
                return new SystemStorageFile(await Folder.CreateFileAsync(desiredName));
            });
        }

        public override IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options)
        {
            return AsyncInfo.Run<BaseStorageFile>(async (cancellationToken) =>
            {
                return new SystemStorageFile(await Folder.CreateFileAsync(desiredName, options));
            });
        }

        public override IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName)
        {
            return AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) =>
            {
                return new SystemStorageFolder(await Folder.CreateFolderAsync(desiredName));
            });
        }

        public override IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName, CreationCollisionOption options)
        {
            return AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) =>
            {
                return new SystemStorageFolder(await Folder.CreateFolderAsync(desiredName, options));
            });
        }

        public override IAsyncOperation<BaseStorageFile> GetFileAsync(string name)
        {
            return AsyncInfo.Run<BaseStorageFile>(async (cancellationToken) =>
            {
                return new SystemStorageFile(await Folder.GetFileAsync(name));
            });
        }

        public override IAsyncOperation<BaseStorageFolder> GetFolderAsync(string name)
        {
            return AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) =>
            {
                return new SystemStorageFolder(await Folder.GetFolderAsync(name));
            });
        }

        public override IAsyncOperation<IStorageItem> GetItemAsync(string name)
        {
            return Folder.GetItemAsync(name);
        }

        public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync()
        {
            return AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken) =>
            {
                return (await Folder.GetFilesAsync()).Select(item => new SystemStorageFile(item)).ToList();
            });
        }

        public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync()
        {
            return AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken) =>
            {
                return (await Folder.GetFoldersAsync()).Select(item => new SystemStorageFolder(item)).ToList();
            });
        }

        public override IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync()
        {
            return Folder.GetItemsAsync();
        }

        public override IAsyncAction RenameAsync(string desiredName)
        {
            return Folder.RenameAsync(desiredName);
        }

        public override IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
        {
            return Folder.RenameAsync(desiredName, option);
        }

        public override IAsyncAction DeleteAsync()
        {
            return Folder.DeleteAsync();
        }

        public override IAsyncAction DeleteAsync(StorageDeleteOption option)
        {
            return Folder.DeleteAsync(option);
        }

        public override IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync()
        {
            return AsyncInfo.Run<BaseBasicProperties>(async (cancellationToken) =>
            {
                var basicProps = await Folder.GetBasicPropertiesAsync();
                return new SystemFolderBasicProperties(basicProps);
            });
        }

        public override bool IsOfType(StorageItemTypes type)
        {
            return Folder.IsOfType(type);
        }

        public override FileAttributes Attributes => Folder.Attributes;

        public override DateTimeOffset DateCreated => Folder.DateCreated;

        public override string Name => Folder.Name;

        public override string Path => Folder.Path;

        public override IAsyncOperation<IndexedState> GetIndexedStateAsync()
        {
            return Folder.GetIndexedStateAsync();
        }

        public override StorageFileQueryResult CreateFileQuery()
        {
            return Folder.CreateFileQuery();
        }

        public override StorageFileQueryResult CreateFileQuery(CommonFileQuery query)
        {
            return Folder.CreateFileQuery(query);
        }

        public override BaseStorageFileQueryResult CreateFileQueryWithOptions(QueryOptions queryOptions)
        {
            return new SystemStorageFileQueryResult(Folder.CreateFileQueryWithOptions(queryOptions));
        }

        public override StorageFolderQueryResult CreateFolderQuery()
        {
            return Folder.CreateFolderQuery();
        }

        public override StorageFolderQueryResult CreateFolderQuery(CommonFolderQuery query)
        {
            return Folder.CreateFolderQuery(query);
        }

        public override BaseStorageFolderQueryResult CreateFolderQueryWithOptions(QueryOptions queryOptions)
        {
            return new SystemStorageFolderQueryResult(Folder.CreateFolderQueryWithOptions(queryOptions));
        }

        public override StorageItemQueryResult CreateItemQuery()
        {
            return Folder.CreateItemQuery();
        }

        public override BaseStorageItemQueryResult CreateItemQueryWithOptions(QueryOptions queryOptions)
        {
            return new SystemStorageItemQueryResult(Folder.CreateItemQueryWithOptions(queryOptions));
        }

        public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(CommonFileQuery query, uint startIndex, uint maxItemsToRetrieve)
        {
            return AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken) =>
            {
                var items = await Folder.GetFilesAsync(query, startIndex, maxItemsToRetrieve);
                return items.Select(x => new SystemStorageFile(x)).ToList();
            });
        }

        public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(CommonFileQuery query)
        {
            return AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken) =>
            {
                var items = await Folder.GetFilesAsync(query);
                return items.Select(x => new SystemStorageFile(x)).ToList();
            });
        }

        public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(CommonFolderQuery query, uint startIndex, uint maxItemsToRetrieve)
        {
            return AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken) =>
            {
                var items = await Folder.GetFoldersAsync(query, startIndex, maxItemsToRetrieve);
                return items.Select(x => new SystemStorageFolder(x)).ToList();
            });
        }

        public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(CommonFolderQuery query)
        {
            return AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken) =>
            {
                var items = await Folder.GetFoldersAsync(query);
                return items.Select(x => new SystemStorageFolder(x)).ToList();
            });
        }

        public override IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync(uint startIndex, uint maxItemsToRetrieve)
        {
            return Folder.GetItemsAsync(startIndex, maxItemsToRetrieve);
        }

        public override bool AreQueryOptionsSupported(QueryOptions queryOptions)
        {
            return Folder.AreQueryOptionsSupported(queryOptions);
        }

        public override bool IsCommonFolderQuerySupported(CommonFolderQuery query)
        {
            return Folder.IsCommonFolderQuerySupported(query);
        }

        public override bool IsCommonFileQuerySupported(CommonFileQuery query)
        {
            return Folder.IsCommonFileQuerySupported(query);
        }

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode)
        {
            return Folder.GetThumbnailAsync(mode);
        }

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize)
        {
            return Folder.GetThumbnailAsync(mode, requestedSize);
        }

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
        {
            return Folder.GetThumbnailAsync(mode, requestedSize, options);
        }

        public override IAsyncOperation<StorageFolder> ToStorageFolderAsync()
        {
            return Task.FromResult(Folder).AsAsyncOperation();
        }

        public override IAsyncOperation<BaseStorageFolder> GetParentAsync()
        {
            return AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) =>
            {
                return new SystemStorageFolder(await Folder.GetParentAsync());
            });
        }

        public override bool IsEqual(IStorageItem item) => Folder.IsEqual(item);

        public override IAsyncOperation<IStorageItem> TryGetItemAsync(string name) => Folder.TryGetItemAsync(name);

        public static IAsyncOperation<BaseStorageFolder> FromPathAsync(string path)
        {
            return AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) =>
            {
                return new SystemStorageFolder(await StorageFolder.GetFolderFromPathAsync(path));
            });
        }

        public override string DisplayName => Folder.DisplayName;

        public override string DisplayType => Folder.DisplayType;

        public override string FolderRelativeId => Folder.FolderRelativeId;

        public override IStorageItemExtraProperties Properties => Folder.Properties;

        private class SystemFolderBasicProperties : BaseBasicProperties
        {
            private IStorageItemExtraProperties basicProps;

            public SystemFolderBasicProperties(IStorageItemExtraProperties basicProps)
            {
                this.basicProps = basicProps;
            }

            public override DateTimeOffset DateModified
            {
                get => (basicProps as BasicProperties)?.DateModified ?? DateTimeOffset.Now;
            }

            public override DateTimeOffset ItemDate
            {
                get => (basicProps as BasicProperties)?.ItemDate ?? DateTimeOffset.Now;
            }

            public override ulong Size
            {
                get => (basicProps as BasicProperties)?.Size ?? 0;
            }

            public override IAsyncOperation<IDictionary<string, object>> RetrievePropertiesAsync(IEnumerable<string> propertiesToRetrieve)
            {
                return basicProps.RetrievePropertiesAsync(propertiesToRetrieve);
            }

            public override IAsyncAction SavePropertiesAsync([HasVariant] IEnumerable<KeyValuePair<string, object>> propertiesToSave)
            {
                return basicProps.SavePropertiesAsync(propertiesToSave);
            }

            public override IAsyncAction SavePropertiesAsync()
            {
                return basicProps.SavePropertiesAsync();
            }
        }
    }
}
