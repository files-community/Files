using Files.Common;
using Files.Helpers;
using FluentFTP;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;

namespace Files.Filesystem.StorageItems
{
    public sealed class FtpStorageFolder : BaseStorageFolder
    {
        public FtpStorageFolder(FtpItem ftpItem)
        {
            DateCreated = ftpItem.ItemDateCreatedReal;
            Name = ftpItem.ItemName;
            Path = ftpItem.ItemPath;
            FtpPath = FtpHelpers.GetFtpPath(ftpItem.ItemPath);
        }

        public FtpStorageFolder(string folder, FtpListItem ftpItem)
        {
            DateCreated = ftpItem.RawCreated < DateTime.FromFileTimeUtc(0) ? DateTimeOffset.MinValue : ftpItem.RawCreated;
            Name = ftpItem.Name;
            Path = PathNormalization.Combine(folder, ftpItem.Name);
            FtpPath = FtpHelpers.GetFtpPath(Path);
        }

        public FtpStorageFolder(IStorageItemWithPath item)
        {
            Name = System.IO.Path.GetFileName(item.Path);
            Path = item.Path;
            FtpPath = FtpHelpers.GetFtpPath(item.Path);
        }

        public FtpStorageFolder CloneWithPath(string path)
        {
            return new FtpStorageFolder(new StorageFolderWithPath(null, path));
        }

        public override IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName)
        {
            return CreateFileAsync(desiredName, CreationCollisionOption.FailIfExists);
        }

        public override IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options)
        {
            return AsyncInfo.Run<BaseStorageFile>(async (cancellationToken) =>
            {
                using var ftpClient = new FtpClient();
                ftpClient.Host = FtpHelpers.GetFtpHost(Path);
                ftpClient.Port = FtpHelpers.GetFtpPort(Path);
                ftpClient.Credentials = FtpManager.Credentials.Get(ftpClient.Host, FtpManager.Anonymous);

                if (!await ftpClient.EnsureConnectedAsync())
                {
                    return null;
                }

                using var stream = new MemoryStream();
                var result = await ftpClient.UploadAsync(stream, $"{FtpPath}/{desiredName}", options == CreationCollisionOption.ReplaceExisting ? FtpRemoteExists.Overwrite : FtpRemoteExists.Skip);

                if (result == FtpStatus.Success)
                {
                    return new FtpStorageFile(new StorageFileWithPath(null, $"{Path}/{desiredName}"));
                }

                if (result == FtpStatus.Skipped)
                {
                    if (options == CreationCollisionOption.FailIfExists)
                    {
                        throw new IOException("File already exists.");
                    }

                    return null;
                }

                throw new IOException($"Failed to create file {FtpPath}/{desiredName}.");
            });
        }

        public override IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName)
        {
            return CreateFolderAsync(desiredName, CreationCollisionOption.FailIfExists);
        }

        public override IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName, CreationCollisionOption options)
        {
            return AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) =>
            {
                using var ftpClient = new FtpClient();
                ftpClient.Host = FtpHelpers.GetFtpHost(Path);
                ftpClient.Port = FtpHelpers.GetFtpPort(Path);
                ftpClient.Credentials = FtpManager.Credentials.Get(ftpClient.Host, FtpManager.Anonymous);

                if (!await ftpClient.EnsureConnectedAsync())
                {
                    throw new IOException($"Failed to connect to FTP server.");
                }

                if (ftpClient.DirectoryExists($"{FtpPath}/{desiredName}"))
                {
                    return new FtpStorageFolder(new StorageFileWithPath(null, $"{Path}/{desiredName}"));
                }

                if (!await ftpClient.CreateDirectoryAsync($"{FtpPath}/{desiredName}",
                    options == CreationCollisionOption.ReplaceExisting,
                    cancellationToken))
                {
                    throw new IOException($"Failed to create folder {desiredName}.");
                }

                return new FtpStorageFolder(new StorageFileWithPath(null, $"{Path}/{desiredName}"));
            });
        }

        public override IAsyncOperation<BaseStorageFile> GetFileAsync(string name)
        {
            return AsyncInfo.Run<BaseStorageFile>(async (cancellationToken) =>
            {
                return await GetItemAsync(name) as BaseStorageFile;
            });
        }

        public override IAsyncOperation<BaseStorageFolder> GetFolderAsync(string name)
        {
            return AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) =>
            {
                return await GetItemAsync(name) as BaseStorageFolder;
            });
        }

        public override IAsyncOperation<IStorageItem> GetItemAsync(string name)
        {
            return AsyncInfo.Run<IStorageItem>(async (cancellationToken) =>
            {
                using var ftpClient = new FtpClient();
                ftpClient.Host = FtpHelpers.GetFtpHost(Path);
                ftpClient.Port = FtpHelpers.GetFtpPort(Path);
                ftpClient.Credentials = FtpManager.Credentials.Get(ftpClient.Host, FtpManager.Anonymous);

                if (!await ftpClient.EnsureConnectedAsync())
                {
                    return null;
                }

                var item = await ftpClient.GetObjectInfoAsync(FtpHelpers.GetFtpPath(PathNormalization.Combine(Path, name)));
                if (item != null)
                {
                    if (item.Type == FtpFileSystemObjectType.File)
                    {
                        return new FtpStorageFile(Path, item);
                    }
                    else if (item.Type == FtpFileSystemObjectType.Directory)
                    {
                        return new FtpStorageFolder(Path, item);
                    }
                }
                return null;
            });
        }

        public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync()
        {
            return AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken) =>
            {
                return (await GetItemsAsync())?.OfType<FtpStorageFile>().ToList();
            });
        }

        public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync()
        {
            return AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken) =>
            {
                return (await GetItemsAsync())?.OfType<FtpStorageFolder>().ToList();
            });
        }

        public override IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync()
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                using var ftpClient = new FtpClient();
                ftpClient.Host = FtpHelpers.GetFtpHost(Path);
                ftpClient.Port = FtpHelpers.GetFtpPort(Path);
                ftpClient.Credentials = FtpManager.Credentials.Get(ftpClient.Host, FtpManager.Anonymous);

                if (!await ftpClient.EnsureConnectedAsync())
                {
                    return null;
                }

                var items = new List<IStorageItem>();
                var list = await ftpClient.GetListingAsync(FtpPath);
                foreach (var item in list)
                {
                    if (item.Type == FtpFileSystemObjectType.File)
                    {
                        items.Add(new FtpStorageFile(Path, item));
                    }
                    else if (item.Type == FtpFileSystemObjectType.Directory)
                    {
                        items.Add(new FtpStorageFolder(Path, item));
                    }
                };
                return (IReadOnlyList<IStorageItem>)items;
            });
        }

        public override IAsyncAction RenameAsync(string desiredName)
        {
            return RenameAsync(desiredName, NameCollisionOption.FailIfExists);
        }

        public override IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                using var ftpClient = new FtpClient();
                ftpClient.Host = FtpHelpers.GetFtpHost(Path);
                ftpClient.Port = FtpHelpers.GetFtpPort(Path);
                ftpClient.Credentials = FtpManager.Credentials.Get(ftpClient.Host, FtpManager.Anonymous);

                if (!await ftpClient.EnsureConnectedAsync())
                {
                    return;
                }

                if (!await ftpClient.MoveDirectoryAsync(FtpPath,
                    $"{PathNormalization.GetParentDir(FtpPath)}/{desiredName}",
                    option == NameCollisionOption.ReplaceExisting ? FtpRemoteExists.Overwrite : FtpRemoteExists.Skip,
                    cancellationToken))
                {
                    if (option == NameCollisionOption.GenerateUniqueName)
                    {
                        // TODO: handle name generation
                    }
                }
            });
        }

        public override IAsyncAction DeleteAsync()
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                using var ftpClient = new FtpClient();
                ftpClient.Host = FtpHelpers.GetFtpHost(Path);
                ftpClient.Port = FtpHelpers.GetFtpPort(Path);
                ftpClient.Credentials = FtpManager.Credentials.Get(ftpClient.Host, FtpManager.Anonymous);

                if (!await ftpClient.EnsureConnectedAsync())
                {
                    return;
                }

                await ftpClient.DeleteDirectoryAsync(FtpPath, cancellationToken);
            });
        }

        public override IAsyncAction DeleteAsync(StorageDeleteOption option)
        {
            return DeleteAsync();
        }

        public override IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync()
        {
            return AsyncInfo.Run<BaseBasicProperties>(async (cancellationToken) =>
            {
                using var ftpClient = new FtpClient();
                ftpClient.Host = FtpHelpers.GetFtpHost(Path);
                ftpClient.Port = FtpHelpers.GetFtpPort(Path);
                ftpClient.Credentials = FtpManager.Credentials.Get(ftpClient.Host, FtpManager.Anonymous);

                if (!await ftpClient.EnsureConnectedAsync())
                {
                    return new BaseBasicProperties();
                }

                var item = await ftpClient.GetObjectInfoAsync(FtpPath);
                if (item != null)
                {
                    return new FtpFolderBasicProperties(item);
                }
                return new BaseBasicProperties();
            });
        }

        public override bool IsOfType(StorageItemTypes type) => type == StorageItemTypes.Folder;

        public override Windows.Storage.FileAttributes Attributes { get; } = Windows.Storage.FileAttributes.Directory;

        public override DateTimeOffset DateCreated { get; }

        public override string Name { get; }

        public override string Path { get; }

        public string FtpPath { get; }

        public override IAsyncOperation<IndexedState> GetIndexedStateAsync()
        {
            return Task.FromResult(IndexedState.NotIndexed).AsAsyncOperation();
        }

        public override StorageFileQueryResult CreateFileQuery() => throw new NotSupportedException();

        public override StorageFileQueryResult CreateFileQuery(CommonFileQuery query) => throw new NotSupportedException();

        public override BaseStorageFileQueryResult CreateFileQueryWithOptions(QueryOptions queryOptions)
        {
            return new BaseStorageFileQueryResult(this, queryOptions);
        }

        public override StorageFolderQueryResult CreateFolderQuery() => throw new NotSupportedException();

        public override StorageFolderQueryResult CreateFolderQuery(CommonFolderQuery query) => throw new NotSupportedException();

        public override BaseStorageFolderQueryResult CreateFolderQueryWithOptions(QueryOptions queryOptions)
        {
            return new BaseStorageFolderQueryResult(this, queryOptions);
        }

        public override StorageItemQueryResult CreateItemQuery() => throw new NotSupportedException();

        public override BaseStorageItemQueryResult CreateItemQueryWithOptions(QueryOptions queryOptions)
        {
            return new BaseStorageItemQueryResult(this, queryOptions);
        }

        public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(CommonFileQuery query, uint startIndex, uint maxItemsToRetrieve)
        {
            return AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken) =>
            {
                var items = await GetFilesAsync();
                return items.Skip((int)startIndex).Take((int)maxItemsToRetrieve).ToList();
            });
        }

        public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(CommonFileQuery query)
        {
            return AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken) =>
            {
                return await GetFilesAsync();
            });
        }

        public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(CommonFolderQuery query, uint startIndex, uint maxItemsToRetrieve)
        {
            return AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken) =>
            {
                var items = await GetFoldersAsync();
                return items.Skip((int)startIndex).Take((int)maxItemsToRetrieve).ToList();
            });
        }

        public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(CommonFolderQuery query)
        {
            return AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken) =>
            {
                return await GetFoldersAsync();
            });
        }

        public override IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync(uint startIndex, uint maxItemsToRetrieve)
        {
            return AsyncInfo.Run<IReadOnlyList<IStorageItem>>(async (cancellationToken) =>
            {
                var items = await GetItemsAsync();
                return items.Skip((int)startIndex).Take((int)maxItemsToRetrieve).ToList();
            });
        }

        public override bool AreQueryOptionsSupported(QueryOptions queryOptions) => false;

        public override bool IsCommonFolderQuerySupported(CommonFolderQuery query) => false;

        public override bool IsCommonFileQuerySupported(CommonFileQuery query) => false;

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode)
        {
            return Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
        }

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize)
        {
            return Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
        }

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
        {
            return Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
        }

        public override IAsyncOperation<StorageFolder> ToStorageFolderAsync() => throw new NotSupportedException();

        public override IAsyncOperation<BaseStorageFolder> GetParentAsync()
        {
            throw new NotSupportedException();
        }

        public override bool IsEqual(IStorageItem item) => item?.Path == Path;

        public override IAsyncOperation<IStorageItem> TryGetItemAsync(string name)
        {
            return AsyncInfo.Run<IStorageItem>(async (cancellationToken) =>
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

        public static IAsyncOperation<BaseStorageFolder> FromPathAsync(string path)
        {
            if (FtpHelpers.IsFtpPath(path) && FtpHelpers.VerifyFtpPath(path))
            {
                return Task.FromResult<BaseStorageFolder>(new FtpStorageFolder(new StorageFolderWithPath(null, path))).AsAsyncOperation();
            }
            return Task.FromResult<BaseStorageFolder>(null).AsAsyncOperation();
        }

        public override string DisplayName => Name;

        public override string DisplayType
        {
            get
            {
                return "FileFolderListItem".GetLocalized();
            }
        }

        public override string FolderRelativeId => $"0\\{Name}";

        public override IStorageItemExtraProperties Properties => new BaseBasicStorageItemExtraProperties(this);

        private class FtpFolderBasicProperties : BaseBasicProperties
        {
            public FtpFolderBasicProperties(FtpItem item)
            {
                DateModified = item.ItemDateModifiedReal;
                ItemDate = item.ItemDateCreatedReal;
                Size = (ulong)item.FileSizeBytes;
            }

            public FtpFolderBasicProperties(FtpListItem item)
            {
                DateModified = item.RawModified < DateTime.FromFileTimeUtc(0) ? DateTimeOffset.MinValue : item.RawModified;
                ItemDate = item.RawCreated < DateTime.FromFileTimeUtc(0) ? DateTimeOffset.MinValue : item.RawCreated;
                Size = (ulong)item.Size;
            }

            public override DateTimeOffset DateModified { get; }

            public override DateTimeOffset ItemDate { get; }

            public override ulong Size { get; }
        }
    }
}