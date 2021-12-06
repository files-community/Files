using Files.Common;
using Files.Extensions;
using Files.Helpers;
using Files.Services;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using SevenZip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;

namespace Files.Filesystem.StorageItems
{
    public sealed class ZipStorageFolder : BaseStorageFolder
    {
        public static List<string> Extensions => new List<string>()
        {
            ".zip", ".7z", ".rar"
        };

        private static Dictionary<string, bool> defaultAppDict = new Dictionary<string, bool>();
        public static async Task<bool> CheckDefaultZipApp(string filePath)
        {
            IUserSettingsService userSettingsService = Ioc.Default.GetService<IUserSettingsService>();
            Func<Task<bool>> queryFileAssoc = async () =>
            {
                var assoc = await NativeWinApiHelper.GetFileAssociationAsync(filePath);
                if (assoc != null)
                {
                    return assoc == Package.Current.Id.FamilyName
                        || assoc.Equals(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"), StringComparison.OrdinalIgnoreCase);
                }
                return true;
            };
            var ext = System.IO.Path.GetExtension(filePath)?.ToLowerInvariant();
            return userSettingsService.PreferencesSettingsService.OpenArchivesInFiles || await defaultAppDict.Get(ext, queryFileAssoc());
        }

        public ZipStorageFolder(string path, string containerPath)
        {
            Name = System.IO.Path.GetFileName(path.TrimEnd('\\', '/'));
            Path = System.IO.Path.Combine(containerPath, path);
            ContainerPath = containerPath;
        }

        public ZipStorageFolder(string path, string containerPath, ArchiveFileInfo entry)
        {
            Name = System.IO.Path.GetFileName(path.TrimEnd('\\', '/'));
            Path = System.IO.Path.Combine(containerPath, path);
            ContainerPath = containerPath;
            DateCreated = entry.CreationTime == DateTime.MinValue ? DateTimeOffset.MinValue : entry.CreationTime;
            Index = entry.Index;
        }

        public ZipStorageFolder(BaseStorageFile backingFile)
        {
            if (string.IsNullOrEmpty(backingFile.Path))
            {
                throw new ArgumentException("Backing file Path cannot be null");
            }
            Name = System.IO.Path.GetFileName(backingFile.Path.TrimEnd('\\', '/'));
            Path = backingFile.Path;
            ContainerPath = backingFile.Path;
            BackingFile = backingFile;
        }

        public override IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName)
        {
            return CreateFileAsync(desiredName, CreationCollisionOption.FailIfExists);
        }

        public override IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options)
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

                //using (var ms = new MemoryStream())
                //{
                //    using (var archiveStream = await OpenZipFileAsync(FileAccessMode.Read))
                //    {
                //        SevenZipCompressor compressor = new SevenZipCompressor(archiveStream)
                //        {
                //            CompressionMode = CompressionMode.Append
                //        };
                //        var fileName = System.IO.Path.GetRelativePath(ContainerPath, zipDesiredName);
                //        await compressor.CompressStreamDictionaryAsync(new Dictionary<string, Stream>() { { fileName, new MemoryStream() } }, ms);
                //    }
                //    using (var archiveStream = await OpenZipFileAsync(FileAccessMode.ReadWrite))
                //    {
                //        ms.Position = 0;
                //        await ms.CopyToAsync(archiveStream);
                //        await ms.FlushAsync();
                //    }
                //}

                // No point in creating an empty file inside an archive, simply return a ZipStorageFile
                return new ZipStorageFile(zipDesiredName, ContainerPath) { BackingFile = BackingFile };
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
                        SevenZipCompressor compressor = new SevenZipCompressor(archiveStream)
                        {
                            CompressionMode = CompressionMode.Append
                        };
                        var fileName = System.IO.Path.GetRelativePath(ContainerPath, zipDesiredName);
                        await compressor.CompressStreamDictionaryAsync(new Dictionary<string, Stream>() { { fileName, null } }, ms);
                    }
                    using (var archiveStream = await OpenZipFileAsync(FileAccessMode.ReadWrite))
                    {
                        ms.Position = 0;
                        await ms.CopyToAsync(archiveStream);
                        await ms.FlushAsync();
                    }
                }

                return new ZipStorageFolder(desiredName, ContainerPath)
                {
                    BackingFile = BackingFile
                };
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
                using (SevenZipExtractor zipFile = await OpenZipFileAsync())
                {
                    if (zipFile == null || zipFile.ArchiveFileData == null)
                    {
                        return null;
                    }
                    //zipFile.IsStreamOwner = true;
                    var entry = zipFile.ArchiveFileData.FirstOrDefault(x => System.IO.Path.Combine(ContainerPath, x.FileName) == System.IO.Path.Combine(Path, name));
                    if (entry.FileName != null)
                    {
                        if (entry.IsDirectory)
                        {
                            return new ZipStorageFolder(entry.FileName, ContainerPath, entry)
                            {
                                BackingFile = BackingFile
                            };
                        }
                        else
                        {
                            return new ZipStorageFile(entry.FileName, ContainerPath, entry) { BackingFile = BackingFile };
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
                using (SevenZipExtractor zipFile = await OpenZipFileAsync())
                {
                    if (zipFile == null || zipFile.ArchiveFileData == null)
                    {
                        return null;
                    }
                    //zipFile.IsStreamOwner = true;
                    var items = new List<IStorageItem>();
                    foreach (var entry in zipFile.ArchiveFileData) // Returns all items recursively
                    {
                        string winPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(ContainerPath, entry.FileName));
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
                                        items.Add(new ZipStorageFolder(itemPath, ContainerPath, entry)
                                        {
                                            BackingFile = BackingFile
                                        });
                                    }
                                }
                                else
                                {
                                    items.Add(new ZipStorageFile(winPath, ContainerPath, entry) { BackingFile = BackingFile });
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
            return RenameAsync(desiredName, NameCollisionOption.FailIfExists);
        }

        public override IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                if (Path == ContainerPath)
                {
                    if (BackingFile != null)
                    {
                        await BackingFile.RenameAsync(desiredName, option);
                    }
                    else
                    {
                        var parent = await GetParentAsync();
                        var item = await parent.GetItemAsync(Name);
                        await item.RenameAsync(desiredName, option);
                    }
                }
                else
                {
                    if (Index < 0)
                    {
                        Index = await FetchZipIndex();
                        if (Index < 0)
                        {
                            return;
                        }
                    }
                    using (var ms = new MemoryStream())
                    {
                        using (var archiveStream = await OpenZipFileAsync(FileAccessMode.Read))
                        {
                            SevenZipCompressor compressor = new SevenZipCompressor(archiveStream)
                            {
                                CompressionMode = CompressionMode.Append
                            };
                            var fileName = Regex.Replace(Path, $"{Regex.Escape(Name)}(?!.*{Regex.Escape(Name)})", desiredName);
                            await compressor.ModifyArchiveAsync(ms, new Dictionary<int, string>() { { Index, fileName } });
                        }
                        using (var archiveStream = await OpenZipFileAsync(FileAccessMode.ReadWrite))
                        {
                            ms.Position = 0;
                            await ms.CopyToAsync(archiveStream);
                            await ms.FlushAsync();
                        }
                    }
                }
            });
        }

        public override IAsyncAction DeleteAsync()
        {
            return DeleteAsync(StorageDeleteOption.Default);
        }

        public override IAsyncAction DeleteAsync(StorageDeleteOption option)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                if (Path == ContainerPath)
                {
                    if (BackingFile != null)
                    {
                        await BackingFile.DeleteAsync();
                    }
                    else
                    {
                        var parent = await GetParentAsync();
                        var item = await parent.GetItemAsync(Name);
                        await item.DeleteAsync(option);
                    }
                }
                else
                {
                    if (Index < 0)
                    {
                        Index = await FetchZipIndex();
                        if (Index < 0)
                        {
                            return;
                        }
                    }
                    using (var ms = new MemoryStream())
                    {
                        using (var archiveStream = await OpenZipFileAsync(FileAccessMode.Read))
                        {
                            SevenZipCompressor compressor = new SevenZipCompressor(archiveStream)
                            {
                                CompressionMode = CompressionMode.Append
                            };
                            await compressor.ModifyArchiveAsync(ms, new Dictionary<int, string>() { { Index, null} });
                        }
                        using (var archiveStream = await OpenZipFileAsync(FileAccessMode.ReadWrite))
                        {
                            ms.Position = 0;
                            await ms.CopyToAsync(archiveStream);
                            await ms.FlushAsync();
                        }
                    }
                }
            });
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
                return await GetBasicProperties();
            });
        }

        public override bool IsOfType(StorageItemTypes type) => type == StorageItemTypes.Folder;

        public override Windows.Storage.FileAttributes Attributes => Windows.Storage.FileAttributes.Directory;

        public override DateTimeOffset DateCreated { get; }

        private int Index { get; set; } = -2; // Index in zip file

        public override string Name { get; }

        public override string Path { get; }

        public string ContainerPath { get; private set; }

        public BaseStorageFile BackingFile { get; set; }

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
            return AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) =>
            {
                if (BackingFile != null)
                {
                    if (Path == ContainerPath)
                    {
                        return await BackingFile.GetParentAsync();
                    }
                    return new ZipStorageFolder(System.IO.Path.GetDirectoryName(Path), ContainerPath)
                    {
                        BackingFile = BackingFile
                    };
                }
                else
                {
                    return await BaseStorageFolder.GetFolderFromPathAsync(System.IO.Path.GetDirectoryName(Path));
                }
            });
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
            return AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) =>
            {
                var ext = ZipStorageFolder.Extensions.FirstOrDefault(x => path.Contains(x, StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrEmpty(ext))
                {
                    return null;
                }
                var marker = path.IndexOf(ext, StringComparison.OrdinalIgnoreCase);
                if (marker != -1)
                {
                    var containerPath = path.Substring(0, marker + ext.Length);
                    if (!await CheckDefaultZipApp(path))
                    {
                        return null;
                    }
                    if (CheckAccess(containerPath))
                    {
                        return new ZipStorageFolder(path, containerPath);
                    }
                }
                return null;
            });
        }

        public static IAsyncOperation<BaseStorageFolder> FromStorageFileAsync(BaseStorageFile file)
        {
            return AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) =>
            {
                if (await CheckAccess(file))
                {
                    return new ZipStorageFolder(file);
                }
                return null;
            });
        }

        public static bool IsZipPath(string path)
        {
            var ext = ZipStorageFolder.Extensions.FirstOrDefault(x => path.Contains(x, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(ext))
            {
                return false;
            }
            var marker = path.IndexOf(ext, StringComparison.OrdinalIgnoreCase);
            if (marker != -1)
            {
                marker += ext.Length;
                if (marker == path.Length || path[marker] == '\\')
                {
                    return true;
                }
            }
            return false;
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

        #region Private

        private IAsyncOperation<SevenZipExtractor> OpenZipFileAsync()
        {
            return AsyncInfo.Run<SevenZipExtractor>(async (cancellationToken) =>
            {
                return new SevenZipExtractor(await OpenZipFileAsync(FileAccessMode.Read));
            });
        }

        private IAsyncOperation<Stream> OpenZipFileAsync(FileAccessMode accessMode)
        {
            return AsyncInfo.Run<Stream>(async (cancellationToken) =>
            {
                bool readWrite = accessMode == FileAccessMode.ReadWrite;
                if (BackingFile != null)
                {
                    return (await BackingFile.OpenAsync(accessMode)).AsStream();
                }
                else
                {
                    var hFile = NativeFileOperationsHelper.OpenFileForRead(ContainerPath, readWrite);
                    if (hFile.IsInvalid)
                    {
                        return null;
                    }
                    return (Stream)new FileStream(hFile, readWrite ? FileAccess.ReadWrite : FileAccess.Read);
                }
            });
        }

        private static bool CheckAccess(string path)
        {
            return Common.Extensions.IgnoreExceptions(() =>
            {
                var hFile = NativeFileOperationsHelper.OpenFileForRead(path);
                if (hFile.IsInvalid)
                {
                    return false;
                }
                using (var stream = new FileStream(hFile, FileAccess.Read))
                {
                    return CheckAccess(stream);
                }
            });
        }

        private static async Task<bool> CheckAccess(IStorageFile file)
        {
            return await Common.Extensions.IgnoreExceptions(async () =>
            {
                using (var stream = await file.OpenReadAsync())
                {
                    return CheckAccess(stream.AsStream());
                }
            });
        }

        private static bool CheckAccess(Stream stream)
        {
            return Common.Extensions.IgnoreExceptions(() =>
            {
                using (SevenZipExtractor zipFile = new SevenZipExtractor(stream))
                {
                    //zipFile.IsStreamOwner = false;
                    return zipFile.ArchiveFileData != null;
                }
            });
        }

        private async Task<int> FetchZipIndex()
        {
            using (SevenZipExtractor zipFile = await OpenZipFileAsync())
            {
                if (zipFile == null || zipFile.ArchiveFileData == null)
                {
                    return -2;
                }
                //zipFile.IsStreamOwner = true;
                var entry = zipFile.ArchiveFileData.FirstOrDefault(x => System.IO.Path.Combine(ContainerPath, x.FileName) == Path);
                if (entry.FileName != null)
                {
                    return entry.Index;
                }
                return -2;
            }
        }

        private async Task<BaseBasicProperties> GetBasicProperties()
        {
            using (SevenZipExtractor zipFile = await OpenZipFileAsync())
            {
                if (zipFile == null || zipFile.ArchiveFileData == null)
                {
                    return new BaseBasicProperties();
                }
                //zipFile.IsStreamOwner = true;
                var entry = zipFile.ArchiveFileData.FirstOrDefault(x => System.IO.Path.Combine(ContainerPath, x.FileName) == Path);
                if (entry.FileName != null)
                {
                    return new ZipFolderBasicProperties(entry);
                }
                return new BaseBasicProperties();
            }
        }

        private class ZipFolderBasicProperties : BaseBasicProperties
        {
            private ArchiveFileInfo zipEntry;

            public ZipFolderBasicProperties(ArchiveFileInfo entry)
            {
                this.zipEntry = entry;
            }

            public override DateTimeOffset DateModified => zipEntry.CreationTime == DateTime.MinValue ? DateTimeOffset.MinValue : zipEntry.CreationTime;

            public override DateTimeOffset ItemDate => zipEntry.CreationTime == DateTime.MinValue ? DateTimeOffset.MinValue : zipEntry.CreationTime;

            public override ulong Size => (ulong)zipEntry.Size;
        }

        #endregion
    }
}
