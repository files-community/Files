using Files.Uwp.Extensions;
using Files.Uwp.Helpers;
using Files.Shared.Extensions;
using Microsoft.Toolkit.Uwp;
using SevenZip;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using IO = System.IO;

namespace Files.Uwp.Filesystem.StorageItems
{
    public sealed class ZipStorageFolder : BaseStorageFolder, ICreateFileWithStream
    {
        private readonly string containerPath;
        private BaseStorageFile backingFile;

        public override string Path { get; }
        public override string Name { get; }
        public override string DisplayName => Name;
        public override string DisplayType => "FileFolderListItem".GetLocalized();
        public override string FolderRelativeId => $"0\\{Name}";

        public override DateTimeOffset DateCreated { get; }
        public override Windows.Storage.FileAttributes Attributes => Windows.Storage.FileAttributes.Directory;
        public override IStorageItemExtraProperties Properties => new BaseBasicStorageItemExtraProperties(this);

        public ZipStorageFolder(string path, string containerPath)
        {
            Name = IO.Path.GetFileName(path.TrimEnd('\\', '/'));
            Path = path;
            this.containerPath = containerPath;
        }
        public ZipStorageFolder(string path, string containerPath, BaseStorageFile backingFile) : this(path, containerPath)
            => this.backingFile = backingFile;
        public ZipStorageFolder(string path, string containerPath, ArchiveFileInfo entry) : this(path, containerPath)
            => DateCreated = entry.CreationTime == DateTime.MinValue ? DateTimeOffset.MinValue : entry.CreationTime;
        public ZipStorageFolder(BaseStorageFile backingFile)
        {
            if (string.IsNullOrEmpty(backingFile.Path))
            {
                throw new ArgumentException("Backing file Path cannot be null");
            }
            Name = IO.Path.GetFileName(backingFile.Path.TrimEnd('\\', '/'));
            Path = backingFile.Path;
            containerPath = backingFile.Path;
            this.backingFile = backingFile;
        }
        public ZipStorageFolder(string path, string containerPath, ArchiveFileInfo entry, BaseStorageFile backingFile) : this(path, containerPath, entry)
            => this.backingFile = backingFile;

        public static bool IsZipPath(string path, bool includeRoot = true)
        {
            if (!FileExtensionHelpers.IsBrowsableZipFile(path, out var ext))
            {
                return false;
            }
            var marker = path.IndexOf(ext, StringComparison.OrdinalIgnoreCase);
            if (marker is -1)
            {
                return false;
            }
            marker += ext.Length;
            return (marker == path.Length && includeRoot) || (marker < path.Length && path[marker] is '\\');
        }

        private static ConcurrentDictionary<string, Task<bool>> defaultAppDict = new();
        public static async Task<bool> CheckDefaultZipApp(string filePath)
        {
            Func<Task<bool>> queryFileAssoc = async () =>
            {
                var assoc = await NativeWinApiHelper.GetFileAssociationAsync(filePath);
                if (assoc != null)
                {
                    return assoc == Package.Current.Id.FamilyName
                        || assoc.Equals(IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"), StringComparison.OrdinalIgnoreCase);
                }
                return true;
            };
            var ext = IO.Path.GetExtension(filePath)?.ToLowerInvariant();
            return await defaultAppDict.GetAsync(ext, queryFileAssoc);
        }

        public static IAsyncOperation<BaseStorageFolder> FromPathAsync(string path)
        {
            return AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) =>
            {
                if (!FileExtensionHelpers.IsBrowsableZipFile(path, out var ext))
                {
                    return null;
                }
                var marker = path.IndexOf(ext, StringComparison.OrdinalIgnoreCase);
                if (marker is not -1)
                {
                    var containerPath = path.Substring(0, marker + ext.Length);
                    if (CheckAccess(containerPath))
                    {
                        return new ZipStorageFolder(path, containerPath);
                    }
                }
                return null;
            });
        }

        public static IAsyncOperation<BaseStorageFolder> FromStorageFileAsync(BaseStorageFile file)
            => AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) => await CheckAccess(file) ? new ZipStorageFolder(file) : null);

        public override IAsyncOperation<StorageFolder> ToStorageFolderAsync() => throw new NotSupportedException();

        public override bool IsEqual(IStorageItem item) => item?.Path == Path;
        public override bool IsOfType(StorageItemTypes type) => type == StorageItemTypes.Folder;

        public override IAsyncOperation<IndexedState> GetIndexedStateAsync() => Task.FromResult(IndexedState.NotIndexed).AsAsyncOperation();

        public override IAsyncOperation<BaseStorageFolder> GetParentAsync() => throw new NotSupportedException();

        private async Task<BaseBasicProperties> GetBasicProperties()
        {
            using SevenZipExtractor zipFile = await OpenZipFileAsync();
            if (zipFile == null || zipFile.ArchiveFileData == null)
            {
                return new BaseBasicProperties();
            }
            //zipFile.IsStreamOwner = true;
            var entry = zipFile.ArchiveFileData.FirstOrDefault(x => System.IO.Path.Combine(containerPath, x.FileName) == Path);
            return entry.FileName is null
                ? new BaseBasicProperties()
                : new ZipFolderBasicProperties(entry);
        }
        public override IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync()
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                if (Path == containerPath)
                {
                    var zipFile = new SystemStorageFile(await StorageFile.GetFileFromPathAsync(Path));
                    return await zipFile.GetBasicPropertiesAsync();
                }
                return await GetBasicProperties();
            });
        }

        public override IAsyncOperation<IStorageItem> GetItemAsync(string name)
        {
            return AsyncInfo.Run<IStorageItem>(async (cancellationToken) =>
            {
                using SevenZipExtractor zipFile = await OpenZipFileAsync();
                if (zipFile == null || zipFile.ArchiveFileData == null)
                {
                    return null;
                }
                //zipFile.IsStreamOwner = true;
                var entry = zipFile.ArchiveFileData.FirstOrDefault(x => System.IO.Path.Combine(containerPath, x.FileName) == System.IO.Path.Combine(Path, name));
                if (entry.FileName is null)
                {
                    return null;
                }

                if (entry.IsDirectory)
                {
                    return new ZipStorageFolder(entry.FileName, containerPath, entry, backingFile);
                }

                return new ZipStorageFile(entry.FileName, containerPath, entry, backingFile);
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
                using SevenZipExtractor zipFile = await OpenZipFileAsync();
                if (zipFile == null || zipFile.ArchiveFileData == null)
                {
                    return null;
                }
                //zipFile.IsStreamOwner = true;
                var items = new List<IStorageItem>();
                foreach (var entry in zipFile.ArchiveFileData) // Returns all items recursively
                {
                    string winPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(containerPath, entry.FileName));
                    if (winPath.StartsWith(Path.WithEnding("\\"), StringComparison.Ordinal)) // Child of self
                    {
                        var split = winPath.Substring(Path.Length).Split('\\', StringSplitOptions.RemoveEmptyEntries);
                        if (split.Length > 0)
                        {
                            if (entry.IsDirectory || split.Length > 1) // Not all folders have a ZipEntry
                            {
                                var itemPath = System.IO.Path.Combine(Path, split[0]);
                                if (!items.Any(x => x.Path == itemPath))
                                {
                                    items.Add(new ZipStorageFolder(itemPath, containerPath, entry, backingFile));
                                }
                            }
                            else
                            {
                                items.Add(new ZipStorageFile(winPath, containerPath, entry, backingFile));
                            }
                        }
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
            => AsyncInfo.Run<BaseStorageFile>(async (cancellationToken) => await GetItemAsync(name) as ZipStorageFile);
        public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync()
            => AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken) => (await GetItemsAsync())?.OfType<ZipStorageFile>().ToList());
        public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(CommonFileQuery query)
            => AsyncInfo.Run(async (cancellationToken) => await GetFilesAsync());
        public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(CommonFileQuery query, uint startIndex, uint maxItemsToRetrieve)
            => AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken)
                => (await GetFilesAsync()).Skip((int)startIndex).Take((int)maxItemsToRetrieve).ToList()
            );

        public override IAsyncOperation<BaseStorageFolder> GetFolderAsync(string name)
            => AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) => await GetItemAsync(name) as ZipStorageFolder);
        public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync()
            => AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken) => (await GetItemsAsync())?.OfType<ZipStorageFolder>().ToList());
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

        public override IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName)
            => CreateFileAsync(desiredName, CreationCollisionOption.FailIfExists);
        public override IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options)
            => CreateFileAsync(new MemoryStream(), desiredName, options);

        public override IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName)
            => CreateFolderAsync(desiredName, CreationCollisionOption.FailIfExists);
        public override IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName, CreationCollisionOption options)
        {
            return AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) =>
            {
                var zipDesiredName = System.IO.Path.Combine(Path, desiredName);
                var item = await GetItemAsync(desiredName);
                if (item != null)
                {
                    if (options != CreationCollisionOption.ReplaceExisting)
                    {
                        return null;
                    }
                    await item.DeleteAsync();
                }

                using (var ms = new MemoryStream())
                {
                    using (var archiveStream = await OpenZipFileAsync(FileAccessMode.Read))
                    {
                        SevenZipCompressor compressor = new SevenZipCompressor() { CompressionMode = CompressionMode.Append };
                        compressor.SetFormatFromExistingArchive(archiveStream);
                        var fileName = IO.Path.GetRelativePath(containerPath, zipDesiredName);
                        await compressor.CompressStreamDictionaryAsync(archiveStream, new Dictionary<string, Stream>() { { fileName, null } }, "", ms);
                    }
                    using (var archiveStream = await OpenZipFileAsync(FileAccessMode.ReadWrite))
                    {
                        ms.Position = 0;
                        await ms.CopyToAsync(archiveStream);
                        await ms.FlushAsync();
                        archiveStream.SetLength(archiveStream.Position);
                    }
                }

                return new ZipStorageFolder(zipDesiredName, containerPath, backingFile);
            });
        }

        public override IAsyncAction RenameAsync(string desiredName) => RenameAsync(desiredName, NameCollisionOption.FailIfExists);
        public override IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                if (Path == containerPath)
                {
                    if (backingFile != null)
                    {
                        await backingFile.RenameAsync(desiredName, option);
                    }
                    else
                    {
                        var fileName = IO.Path.Combine(IO.Path.GetDirectoryName(Path), desiredName);
                        NativeFileOperationsHelper.MoveFileFromApp(Path, fileName);
                    }
                }
                else
                {
                    var index = await FetchZipIndex();
                    if (index.IsEmpty())
                    {
                        return;
                    }
                    using (var ms = new MemoryStream())
                    {
                        using (var archiveStream = await OpenZipFileAsync(FileAccessMode.Read))
                        {
                            SevenZipCompressor compressor = new SevenZipCompressor() { CompressionMode = CompressionMode.Append };
                            compressor.SetFormatFromExistingArchive(archiveStream);
                            var folderKey = IO.Path.GetRelativePath(containerPath, Path);
                            var folderDes = IO.Path.Combine(IO.Path.GetDirectoryName(folderKey), desiredName);
                            var entriesMap = new Dictionary<int, string>(index.Select(x => new KeyValuePair<int, string>(x.Index,
                                IO.Path.Combine(folderDes, IO.Path.GetRelativePath(folderKey, x.Key)))));
                            await compressor.ModifyArchiveAsync(archiveStream, entriesMap, "", ms);
                        }
                        using (var archiveStream = await OpenZipFileAsync(FileAccessMode.ReadWrite))
                        {
                            ms.Position = 0;
                            await ms.CopyToAsync(archiveStream);
                            await ms.FlushAsync();
                            archiveStream.SetLength(archiveStream.Position);
                        }
                    }
                }
            });
        }

        public override IAsyncAction DeleteAsync() => DeleteAsync(StorageDeleteOption.Default);
        public override IAsyncAction DeleteAsync(StorageDeleteOption option)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                if (Path == containerPath)
                {
                    if (backingFile != null)
                    {
                        await backingFile.DeleteAsync();
                    }
                    else if (option == StorageDeleteOption.PermanentDelete)
                    {
                        NativeFileOperationsHelper.DeleteFileFromApp(Path);
                    }
                    else
                    {
                        throw new NotSupportedException("Moving to recycle bin is not supported.");
                    }
                }
                else
                {
                    var index = await FetchZipIndex();
                    if (index.IsEmpty())
                    {
                        return;
                    }
                    using (var ms = new MemoryStream())
                    {
                        using (var archiveStream = await OpenZipFileAsync(FileAccessMode.Read))
                        {
                            SevenZipCompressor compressor = new SevenZipCompressor() { CompressionMode = CompressionMode.Append };
                            compressor.SetFormatFromExistingArchive(archiveStream);
                            var entriesMap = new Dictionary<int, string>(index.Select(x => new KeyValuePair<int, string>(x.Index, null)));
                            await compressor.ModifyArchiveAsync(archiveStream, entriesMap, "", ms);
                        }
                        using (var archiveStream = await OpenZipFileAsync(FileAccessMode.ReadWrite))
                        {
                            ms.Position = 0;
                            await ms.CopyToAsync(archiveStream);
                            await ms.FlushAsync();
                            archiveStream.SetLength(archiveStream.Position);
                        }
                    }
                }
            });
        }

        public override bool AreQueryOptionsSupported(QueryOptions queryOptions) => false;
        public override bool IsCommonFileQuerySupported(CommonFileQuery query) => false;
        public override bool IsCommonFolderQuerySupported(CommonFolderQuery query) => false;

        public override StorageItemQueryResult CreateItemQuery() => throw new NotSupportedException();
        public override BaseStorageItemQueryResult CreateItemQueryWithOptions(QueryOptions queryOptions) => new(this, queryOptions);

        public override StorageFileQueryResult CreateFileQuery() => throw new NotSupportedException();
        public override StorageFileQueryResult CreateFileQuery(CommonFileQuery query) => throw new NotSupportedException();
        public override BaseStorageFileQueryResult CreateFileQueryWithOptions(QueryOptions queryOptions) => new(this, queryOptions);

        public override StorageFolderQueryResult CreateFolderQuery() => throw new NotSupportedException();
        public override StorageFolderQueryResult CreateFolderQuery(CommonFolderQuery query) => throw new NotSupportedException();
        public override BaseStorageFolderQueryResult CreateFolderQueryWithOptions(QueryOptions queryOptions) => new(this, queryOptions);

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                if (Path != containerPath)
                {
                    return null;
                }
                var zipFile = await StorageFile.GetFileFromPathAsync(Path);
                return await zipFile.GetThumbnailAsync(mode);
            });
        }
        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                if (Path != containerPath)
                {
                    return null;
                }
                var zipFile = await StorageFile.GetFileFromPathAsync(Path);
                return await zipFile.GetThumbnailAsync(mode, requestedSize);
            });
        }
        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                if (Path != containerPath)
                {
                    return null;
                }
                var zipFile = await StorageFile.GetFileFromPathAsync(Path);
                return await zipFile.GetThumbnailAsync(mode, requestedSize, options);
            });
        }

        private static bool CheckAccess(string path)
        {
            return SafetyExtensions.IgnoreExceptions(() =>
            {
                var hFile = NativeFileOperationsHelper.OpenFileForRead(path);
                if (hFile.IsInvalid)
                {
                    return false;
                }
                using var stream = new FileStream(hFile, FileAccess.Read);
                return CheckAccess(stream);
            });
        }
        private static bool CheckAccess(Stream stream)
        {
            return SafetyExtensions.IgnoreExceptions(() =>
            {
                using (SevenZipExtractor zipFile = new SevenZipExtractor(stream))
                {
                    //zipFile.IsStreamOwner = false;
                    return zipFile.ArchiveFileData != null;
                }
            });
        }
        private static async Task<bool> CheckAccess(IStorageFile file)
        {
            return await SafetyExtensions.IgnoreExceptions(async () =>
            {
                using var stream = await file.OpenReadAsync();
                return CheckAccess(stream.AsStream());
            });
        }

        private IAsyncOperation<SevenZipExtractor> OpenZipFileAsync()
        {
            return AsyncInfo.Run<SevenZipExtractor>(async (cancellationToken) =>
            {
                var zipFile = await OpenZipFileAsync(FileAccessMode.Read);
                return zipFile is not null ? new SevenZipExtractor(zipFile) : null;
            });
        }

        private IAsyncOperation<Stream> OpenZipFileAsync(FileAccessMode accessMode)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                bool readWrite = accessMode is FileAccessMode.ReadWrite;
                if (backingFile != null)
                {
                    return (await backingFile.OpenAsync(accessMode)).AsStream();
                }
                else
                {
                    var hFile = NativeFileOperationsHelper.OpenFileForRead(containerPath, readWrite);
                    if (hFile.IsInvalid)
                    {
                        return null;
                    }
                    return (Stream)new FileStream(hFile, readWrite ? FileAccess.ReadWrite : FileAccess.Read);
                }
            });
        }

        private async Task<IEnumerable<(int Index, string Key)>> FetchZipIndex()
        {
            using (SevenZipExtractor zipFile = await OpenZipFileAsync())
            {
                if (zipFile == null || zipFile.ArchiveFileData == null)
                {
                    return null;
                }
                //zipFile.IsStreamOwner = true;
                return zipFile.ArchiveFileData.Where(x => System.IO.Path.Combine(containerPath, x.FileName).IsSubPathOf(Path)).Select(e => (e.Index, e.FileName));
            }
        }

        public IAsyncOperation<BaseStorageFile> CreateFileAsync(Stream contents, string desiredName)
            => CreateFileAsync(new MemoryStream(), desiredName, CreationCollisionOption.FailIfExists);

        public IAsyncOperation<BaseStorageFile> CreateFileAsync(Stream contents, string desiredName, CreationCollisionOption options)
        {
            return AsyncInfo.Run<BaseStorageFile>(async (cancellationToken) =>
            {
                var zipDesiredName = System.IO.Path.Combine(Path, desiredName);
                var item = await GetItemAsync(desiredName);
                if (item != null)
                {
                    if (options != CreationCollisionOption.ReplaceExisting)
                    {
                        return null;
                    }
                    await item.DeleteAsync();
                }

                using (var ms = new MemoryStream())
                {
                    using (var archiveStream = await OpenZipFileAsync(FileAccessMode.Read))
                    {
                        SevenZipCompressor compressor = new SevenZipCompressor() { CompressionMode = CompressionMode.Append };
                        compressor.SetFormatFromExistingArchive(archiveStream);
                        var fileName = IO.Path.GetRelativePath(containerPath, zipDesiredName);
                        await compressor.CompressStreamDictionaryAsync(archiveStream, new Dictionary<string, Stream>() { { fileName, contents } }, "", ms);
                    }
                    using (var archiveStream = await OpenZipFileAsync(FileAccessMode.ReadWrite))
                    {
                        ms.Position = 0;
                        await ms.CopyToAsync(archiveStream);
                        await ms.FlushAsync();
                        archiveStream.SetLength(archiveStream.Position);
                    }
                }

                return new ZipStorageFile(zipDesiredName, containerPath, backingFile);
            });
        }

        private class ZipFolderBasicProperties : BaseBasicProperties
        {
            private ArchiveFileInfo entry;

            public ZipFolderBasicProperties(ArchiveFileInfo entry) => this.entry = entry;

            public override DateTimeOffset DateModified => entry.CreationTime == DateTime.MinValue ? DateTimeOffset.MinValue : entry.CreationTime;

            public override DateTimeOffset ItemDate => entry.CreationTime == DateTime.MinValue ? DateTimeOffset.MinValue : entry.CreationTime;

            public override ulong Size => (ulong)entry.Size;
        }
    }
}
