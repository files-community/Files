using Files.Shared;
using Files.Shared.Extensions;
using Files.Uwp.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using IO = System.IO;
using Storage = Windows.Storage;

namespace Files.Uwp.Filesystem.StorageItems
{
    public class BinStorageFolder : ShellStorageFolder, IBinStorageItem
    {
        public string OriginalPath { get; }
        public DateTimeOffset DateDeleted { get; }

        public BinStorageFolder(ShellFileItem item) : base(item)
        {
            OriginalPath = item.FilePath;
            DateDeleted = item.RecycleDate;
        }

        public override IAsyncOperation<IStorageItem> GetItemAsync(string name)
        {
            return AsyncInfo.Run<IStorageItem>(async (cancellationToken) =>
            {
                var res = await GetFolderAndItems(Path, true);
                if (res.Items is null)
                {
                    return null;
                }

                var entry = res.Items.FirstOrDefault(x => x.FileName != null && x.FileName.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (entry is null)
                {
                    return null;
                }

                if (entry.IsFolder)
                {
                    return new BinStorageFolder(entry);
                }

                return new BinStorageFile(entry);
            });
        }
        public override IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync()
        {
            return AsyncInfo.Run<IReadOnlyList<IStorageItem>>(async (cancellationToken) =>
            {
                var res = await GetFolderAndItems(Path, true);
                if (res.Items is null)
                {
                    return null;
                }

                var items = new List<IStorageItem>();
                foreach (var entry in res.Items)
                {
                    if (entry.IsFolder)
                    {
                        items.Add(new BinStorageFolder(entry));
                    }
                    else
                    {
                        items.Add(new BinStorageFile(entry));
                    }
                }
                return items;
            });
        }
    }

    public interface IBinStorageItem : IStorageItem
    {
        string OriginalPath { get; }
        DateTimeOffset DateDeleted { get; }
    }

    public class ShellStorageFolder : BaseStorageFolder
    {
        public override string Path { get; }
        public override string Name { get; }
        public override string DisplayName => Name;
        public override string DisplayType { get; }
        public override string FolderRelativeId => $"0\\{Name}";

        public override DateTimeOffset DateCreated { get; }
        public override Storage.FileAttributes Attributes => Storage.FileAttributes.Directory;
        public override IStorageItemExtraProperties Properties => new BaseBasicStorageItemExtraProperties(this);

        public ShellStorageFolder(ShellFileItem item)
        {
            Name = item.FileName;
            Path = item.RecyclePath; // True path on disk
            DateCreated = item.CreatedDate;
            DisplayType = item.FileType;
        }

        public static bool IsShellPath(string path)
        {
            return path is not null && path.StartsWith("shell:", StringComparison.OrdinalIgnoreCase) || path.StartsWith("::{", StringComparison.OrdinalIgnoreCase);
        }

        public static IAsyncOperation<BaseStorageFolder> FromPathAsync(string path)
        {
            return AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) =>
            {
                if (IsShellPath(path))
                {
                    var res = await GetFolderAndItems(path, false);
                    if (res.Folder != null)
                    {
                        if (path.StartsWith(Constants.CommonPaths.RecycleBinPath))
                        {
                            return new BinStorageFolder(res.Folder);
                        }
                        return new ShellStorageFolder(res.Folder);
                    }
                }
                return null;
            });
        }

        protected static async Task<(ShellFileItem Folder, List<ShellFileItem> Items)> GetFolderAndItems(string path, bool enumerate)
        {
            if (await AppServiceConnectionHelper.Instance is NamedPipeAsAppServiceConnection connection)
            {
                ValueSet value = new ValueSet()
                {
                    { "Arguments", "ShellFolder" },
                    { "action", enumerate ? "Enumerate" : "Query" },
                    { "folder", path }
                };
                var (status, response) = await connection.SendMessageForResponseAsync(value);

                if (status == AppServiceResponseStatus.Success)
                {
                    var folder = JsonConvert.DeserializeObject<ShellFileItem>(response.Get("Folder", ""));
                    var items = JsonConvert.DeserializeObject<List<ShellFileItem>>(response.Get("Enumerate", ""));
                    return (folder, items);
                }
            }
            return (null, null);
        }

        public override IAsyncOperation<StorageFolder> ToStorageFolderAsync() => throw new NotSupportedException();

        public override bool IsEqual(IStorageItem item) => item?.Path == Path;
        public override bool IsOfType(StorageItemTypes type) => type == StorageItemTypes.Folder;

        public override IAsyncOperation<IndexedState> GetIndexedStateAsync() => Task.FromResult(IndexedState.NotIndexed).AsAsyncOperation();

        public override IAsyncOperation<BaseStorageFolder> GetParentAsync() => throw new NotSupportedException();

        public override IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync()
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                var res = await GetFolderAndItems(Path, false);
                if (res.Folder is not null)
                {
                    return new ShellFolderBasicProperties(res.Folder);
                }
                return new BaseBasicProperties();
            });
        }

        public override IAsyncOperation<IStorageItem> GetItemAsync(string name)
        {
            return AsyncInfo.Run<IStorageItem>(async (cancellationToken) =>
            {
            var res = await GetFolderAndItems(Path, true);
                if (res.Items is null)
                {
                    return null;
                }

                var entry = res.Items.FirstOrDefault(x => x.FileName != null && x.FileName.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (entry is null)
                {
                    return null;
                }

                if (entry.IsFolder)
                {
                    return new ShellStorageFolder(entry);
                }

                return new ShellStorageFile(entry);
            });
        }
        public override IAsyncOperation<IStorageItem> TryGetItemAsync(string name)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                try
                {
                    return await GetItemAsync(name);
                }
                catch
                {
                    return null;
                }
            });
        }
        public override IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync()
        {
            return AsyncInfo.Run<IReadOnlyList<IStorageItem>>(async (cancellationToken) =>
            {
                var res = await GetFolderAndItems(Path, true);
                if (res.Items is null)
                {
                    return null;
                }

                var items = new List<IStorageItem>();
                foreach (var entry in res.Items)
                {
                    if (entry.IsFolder)
                    {
                        items.Add(new ShellStorageFolder(entry));
                    }
                    else
                    {
                        items.Add(new ShellStorageFile(entry));
                    }
                }
                return items;
            });
        }
        public override IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync(uint startIndex, uint maxItemsToRetrieve)
            => AsyncInfo.Run<IReadOnlyList<IStorageItem>>(async (cancellationToken)
                => (await GetItemsAsync()).Skip((int)startIndex).Take((int)maxItemsToRetrieve).ToList()
            );

        public override IAsyncOperation<BaseStorageFile> GetFileAsync(string name)
            => AsyncInfo.Run<BaseStorageFile>(async (cancellationToken) => await GetItemAsync(name) as ShellStorageFile);
        public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync()
            => AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken) => (await GetItemsAsync())?.OfType<ShellStorageFile>().ToList());
        public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(CommonFileQuery query)
            => AsyncInfo.Run(async (cancellationToken) => await GetFilesAsync());
        public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(CommonFileQuery query, uint startIndex, uint maxItemsToRetrieve)
            => AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken)
                => (await GetFilesAsync()).Skip((int)startIndex).Take((int)maxItemsToRetrieve).ToList()
            );

        public override IAsyncOperation<BaseStorageFolder> GetFolderAsync(string name)
            => AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) => await GetItemAsync(name) as ShellStorageFolder);
        public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync()
            => AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken) => (await GetItemsAsync())?.OfType<ShellStorageFolder>().ToList());
        public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(CommonFolderQuery query)
            => AsyncInfo.Run(async (cancellationToken) => await GetFoldersAsync());
        public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(CommonFolderQuery query, uint startIndex, uint maxItemsToRetrieve)
        {
            return AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken) =>
            {
                var items = await GetFoldersAsync();
                return items.Skip((int)startIndex).Take((int)maxItemsToRetrieve).ToList();
            });
        }

        public override IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName) => throw new NotSupportedException();
        public override IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options)
            => throw new NotSupportedException();

        public override IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName) => throw new NotSupportedException();
        public override IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName, CreationCollisionOption options)
            => throw new NotSupportedException();

        public override IAsyncAction RenameAsync(string desiredName) => throw new NotSupportedException();
        public override IAsyncAction RenameAsync(string desiredName, NameCollisionOption option) => throw new NotSupportedException();

        public override IAsyncAction DeleteAsync() => throw new NotSupportedException();
        public override IAsyncAction DeleteAsync(StorageDeleteOption option) => throw new NotSupportedException();

        public override bool AreQueryOptionsSupported(QueryOptions queryOptions) => false;
        public override bool IsCommonFileQuerySupported(CommonFileQuery query) => false;
        public override bool IsCommonFolderQuerySupported(CommonFolderQuery query) => false;

        public override StorageItemQueryResult CreateItemQuery() => throw new NotSupportedException();
        public override BaseStorageItemQueryResult CreateItemQueryWithOptions(QueryOptions queryOptions) => throw new NotSupportedException();

        public override StorageFileQueryResult CreateFileQuery() => throw new NotSupportedException();
        public override StorageFileQueryResult CreateFileQuery(CommonFileQuery query) => throw new NotSupportedException();
        public override BaseStorageFileQueryResult CreateFileQueryWithOptions(QueryOptions queryOptions) => throw new NotSupportedException();

        public override StorageFolderQueryResult CreateFolderQuery() => throw new NotSupportedException();
        public override StorageFolderQueryResult CreateFolderQuery(CommonFolderQuery query) => throw new NotSupportedException();
        public override BaseStorageFolderQueryResult CreateFolderQueryWithOptions(QueryOptions queryOptions) => throw new NotSupportedException();

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode)
            => Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize)
            => Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
            => Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();

        private class ShellFolderBasicProperties : BaseBasicProperties
        {
            private readonly ShellFileItem folder;

            public ShellFolderBasicProperties(ShellFileItem folder) => this.folder = folder;

            public override ulong Size => folder.FileSizeBytes;

            public override DateTimeOffset ItemDate => folder.ModifiedDate;
            public override DateTimeOffset DateModified => folder.ModifiedDate;
        }
    }
}
