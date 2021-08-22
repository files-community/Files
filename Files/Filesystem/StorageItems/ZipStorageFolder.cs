using Files.Helpers;
using ICSharpCode.SharpZipLib.Zip;
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
    public sealed class ZipStorageFolder : BaseStorageFolder
    {
        public ZipStorageFolder(string path, string containerPath)
        {
            Name = System.IO.Path.GetFileName(path.TrimEnd('\\', '/'));
            Path = path;
            ContainerPath = containerPath;
        }

        public ZipStorageFolder(string path, string containerPath, ZipEntry entry)
        {
            Name = System.IO.Path.GetFileName(path.TrimEnd('\\', '/'));
            Path = path;
            ContainerPath = containerPath;
            DateCreated = entry.DateTime;
        }

        public override IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName)
        {
            return CreateFileAsync(desiredName, CreationCollisionOption.FailIfExists);
        }

        public override IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options)
        {
            return AsyncInfo.Run<BaseStorageFile>(async (cancellationToken) =>
            {
                var hFile = NativeFileOperationsHelper.OpenFileForRead(ContainerPath, true);
                if (hFile.IsInvalid)
                {
                    return null;
                }
                using (ZipFile zipFile = new ZipFile(new FileStream(hFile, FileAccess.ReadWrite)))
                {
                    zipFile.IsStreamOwner = true;

                    var znt = new ZipNameTransform(ContainerPath);
                    var zipDesiredName = znt.TransformFile(System.IO.Path.Combine(Path, desiredName));
                    var entry = zipFile.GetEntry(zipDesiredName);

                    zipFile.BeginUpdate(new MemoryArchiveStorage(FileUpdateMode.Direct));
                    if (entry != null)
                    {
                        if (options != CreationCollisionOption.ReplaceExisting)
                        {
                            zipFile.AbortUpdate();
                            return null;
                        }
                        zipFile.Delete(entry);
                    }
                    zipFile.Add(new FileDataSource() { Stream = new MemoryStream() }, zipDesiredName);
                    zipFile.CommitUpdate();

                    var wnt = new WindowsNameTransform(ContainerPath);
                    return new ZipStorageFile(wnt.TransformFile(zipDesiredName), ContainerPath);
                }
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
                var hFile = NativeFileOperationsHelper.OpenFileForRead(ContainerPath, true);
                if (hFile.IsInvalid)
                {
                    return null;
                }
                using (ZipFile zipFile = new ZipFile(new FileStream(hFile, FileAccess.ReadWrite)))
                {
                    zipFile.IsStreamOwner = true;

                    var znt = new ZipNameTransform(ContainerPath);
                    var zipDesiredName = znt.TransformDirectory(System.IO.Path.Combine(Path, desiredName));
                    var entry = zipFile.GetEntry(zipDesiredName);

                    zipFile.BeginUpdate(new MemoryArchiveStorage(FileUpdateMode.Direct));
                    if (entry != null)
                    {
                        if (options != CreationCollisionOption.ReplaceExisting)
                        {
                            zipFile.AbortUpdate();
                            return null;
                        }
                        zipFile.Delete(entry);
                    }
                    zipFile.AddDirectory(zipDesiredName);
                    zipFile.CommitUpdate();

                    var wnt = new WindowsNameTransform(ContainerPath);
                    return new ZipStorageFolder(wnt.TransformFile(zipDesiredName), ContainerPath);
                }
            });
        }

        public override IAsyncOperation<BaseStorageFile> GetFileAsync(string name)
        {
            return AsyncInfo.Run<BaseStorageFile>(async (cancellationToken) =>
            {
                return await GetItemAsync(name) as ZipStorageFile;
            });
        }

        public override IAsyncOperation<BaseStorageFolder> GetFolderAsync(string name)
        {
            return AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) =>
            {
                return await GetItemAsync(name) as ZipStorageFolder;
            });
        }

        public override IAsyncOperation<IStorageItem> GetItemAsync(string name)
        {
            return AsyncInfo.Run<IStorageItem>(async (cancellationToken) =>
            {
                var hFile = NativeFileOperationsHelper.OpenFileForRead(ContainerPath);
                if (hFile.IsInvalid)
                {
                    return null;
                }
                using (ZipFile zipFile = new ZipFile(new FileStream(hFile, FileAccess.Read)))
                {
                    zipFile.IsStreamOwner = true;
                    var entry = zipFile.GetEntry(name);
                    if (entry != null)
                    {
                        var wnt = new WindowsNameTransform(ContainerPath);
                        if (entry.IsDirectory)
                        {
                            return new ZipStorageFolder(wnt.TransformDirectory(entry.Name), ContainerPath, entry);
                        }
                        else
                        {
                            return new ZipStorageFile(wnt.TransformFile(entry.Name), ContainerPath, entry);
                        }
                    }
                    return null;
                }
            });
        }

        public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync()
        {
            return AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken) =>
            {
                return (await GetItemsAsync())?.OfType<ZipStorageFile>().ToList();
            });
        }

        public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync()
        {
            return AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken) =>
            {
                return (await GetItemsAsync())?.OfType<ZipStorageFolder>().ToList();
            });
        }

        public override IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync()
        {
            return AsyncInfo.Run<IReadOnlyList<IStorageItem>>(async (cancellationToken) =>
            {
                var hFile = NativeFileOperationsHelper.OpenFileForRead(ContainerPath);
                if (hFile.IsInvalid)
                {
                    return null;
                }
                using (ZipFile zipFile = new ZipFile(new FileStream(hFile, FileAccess.Read)))
                {
                    zipFile.IsStreamOwner = true;
                    var wnt = new WindowsNameTransform(ContainerPath);
                    var items = new List<IStorageItem>();
                    foreach (var entry in zipFile.OfType<ZipEntry>()) // Returns all items recursively
                    {
                        string winPath = entry.IsDirectory ? wnt.TransformDirectory(entry.Name) : wnt.TransformFile(entry.Name);
                        if (winPath.StartsWith(Path)) // Child of self
                        {
                            var split = winPath.Substring(Path.Length).Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                            if (split.Length > 0)
                            {
                                if (entry.IsDirectory || split.Length > 1) // Not all folders have a ZipEntry
                                {
                                    var itemPath = System.IO.Path.Combine(Path, split[0]);
                                    if (!items.Any(x => x.Path == itemPath))
                                    {
                                        items.Add(new ZipStorageFolder(itemPath, ContainerPath, entry));
                                    }
                                }
                                else
                                {
                                    items.Add(new ZipStorageFile(winPath, ContainerPath, entry));
                                }
                            }
                        }
                    }
                    return items;
                }
            });
        }

        public override IAsyncAction RenameAsync(string desiredName)
        {
            throw new NotSupportedException();
        }

        public override IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
        {
            throw new NotSupportedException();
        }

        public override IAsyncAction DeleteAsync()
        {
            throw new NotSupportedException();
        }

        public override IAsyncAction DeleteAsync(StorageDeleteOption option)
        {
            throw new NotSupportedException();
        }

        public override IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync()
        {
            return AsyncInfo.Run<BaseBasicProperties>(async (cancellationToken) =>
            {
                if (Path == ContainerPath)
                {
                    var zipFile = new SystemStorageFile(await StorageFile.GetFileFromPathAsync(Path));
                    return await zipFile.GetBasicPropertiesAsync();
                }
                return GetBasicProperties();
            });
        }

        private BaseBasicProperties GetBasicProperties()
        {
            if (Path == ContainerPath)
            {
                return new BaseBasicProperties();
            }
            var hFile = NativeFileOperationsHelper.OpenFileForRead(ContainerPath);
            if (hFile.IsInvalid)
            {
                return new BaseBasicProperties();
            }
            using (ZipFile zipFile = new ZipFile(new FileStream(hFile, FileAccess.Read)))
            {
                zipFile.IsStreamOwner = true;
                var znt = new ZipNameTransform(ContainerPath);
                var entry = zipFile.GetEntry(znt.TransformFile(Path));
                if (entry != null)
                {
                    return new ZipFolderBasicProperties(entry);
                }
                return new BaseBasicProperties();
            }
        }

        public override bool IsOfType(StorageItemTypes type) => type == StorageItemTypes.Folder;

        public override Windows.Storage.FileAttributes Attributes => Windows.Storage.FileAttributes.Directory;

        public override DateTimeOffset DateCreated { get; }

        public override string Name { get; }

        public override string Path { get; }

        public string ContainerPath { get; private set; }

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
            return AsyncInfo.Run<StorageItemThumbnail>(async (cancellationToken) =>
            {
                if (Path == ContainerPath)
                {
                    var zipFile = await StorageFile.GetFileFromPathAsync(Path);
                    return await zipFile.GetThumbnailAsync(mode);
                }
                return null;
            });
        }

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize)
        {
            return AsyncInfo.Run<StorageItemThumbnail>(async (cancellationToken) =>
            {
                if (Path == ContainerPath)
                {
                    var zipFile = await StorageFile.GetFileFromPathAsync(Path);
                    return await zipFile.GetThumbnailAsync(mode, requestedSize);
                }
                return null;
            });
        }

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
        {
            return AsyncInfo.Run<StorageItemThumbnail>(async (cancellationToken) =>
            {
                if (Path == ContainerPath)
                {
                    var zipFile = await StorageFile.GetFileFromPathAsync(Path);
                    return await zipFile.GetThumbnailAsync(mode, requestedSize, options);
                }
                return null;
            });
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
            var marker = path.IndexOf(".zip");
            if (marker != -1)
            {
                var containerPath = path.Substring(0, marker + ".zip".Length);
                if (CheckAccess(containerPath))
                {
                    return Task.FromResult<BaseStorageFolder>(new ZipStorageFolder(path, containerPath)).AsAsyncOperation();
                }
            }
            return Task.FromResult<BaseStorageFolder>(null).AsAsyncOperation();
        }

        public static bool IsZipPath(string path)
        {
            var marker = path.IndexOf(".zip");
            if (marker != -1)
            {
                marker += ".zip".Length;
                if (marker == path.Length || path[marker] == '\\')
                {
                    return true;
                }
            }
            return false;
        }

        private static bool CheckAccess(string path)
        {
            try
            {
                var hFile = NativeFileOperationsHelper.OpenFileForRead(path);
                if (hFile.IsInvalid)
                {
                    return false;
                }
                using (ZipFile zipFile = new ZipFile(new FileStream(hFile, FileAccess.Read)))
                {
                    zipFile.IsStreamOwner = true;
                }
                return true;
            }
            catch
            {
                return false;
            }
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

        private class FileDataSource : IStaticDataSource
        {
            public Stream Stream { get; set; }

            public Stream GetSource() => Stream;
        }

        private class ZipFolderBasicProperties : BaseBasicProperties
        {
            private ZipEntry zipEntry;

            public ZipFolderBasicProperties(ZipEntry entry)
            {
                this.zipEntry = entry;
            }

            public override DateTimeOffset DateModified => zipEntry.DateTime;

            public override DateTimeOffset ItemDate => zipEntry.DateTime;

            public override ulong Size => (ulong)zipEntry.Size;
        }
    }
}
