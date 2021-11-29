using Files.Extensions;
using Files.Helpers;
using Microsoft.Toolkit.Uwp;
using SevenZipExtractor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
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
        public Encoding ZipEncoding { get; set; } = null;

        private static bool? IsDefaultZipApp;
        public static async Task<bool> CheckDefaultZipApp(string filePath)
        {
            Func<Task<bool>> queryFileAssoc = async () =>
            {
                var assoc = await NativeWinApiHelper.GetFileAssociationAsync(filePath);
                if (assoc != null)
                {
                    IsDefaultZipApp = assoc == Package.Current.Id.FamilyName
                        || assoc.Equals(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"), StringComparison.OrdinalIgnoreCase);
                }
                return true;
            };
            return IsDefaultZipApp ?? await queryFileAssoc();
        }

        public static string DecodeEntryName(ZipEntry entry, Encoding zipEncoding)
        {
            // TODO
            return entry.FileName;
        }

        public static Encoding DetectFileEncoding(ArchiveFile zipFile)
        {
            // TODO
            return Encoding.UTF8;
        }

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
            DateCreated = entry.CreationTime;
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
                using (ArchiveFile zipFile = await OpenZipFileAsync(FileAccessMode.ReadWrite))
                {
                    return null;
                    /*
                    if (zipFile == null)
                    {
                        return null;
                    }
                    zipFile.IsStreamOwner = true;

                    var znt = new ZipNameTransform(ContainerPath);
                    var zipDesiredName = znt.TransformFile(System.IO.Path.Combine(Path, desiredName));
                    var entry = zipFile.Entries.FirstOrDefault(x => x.FileName == zipDesiredName);

                    if (entry != null)
                    {
                        if (options != CreationCollisionOption.ReplaceExisting)
                        {
                            return null;
                        }
                        zipFile.Delete(entry);
                    }
                    zipFile.Add(new FileDataSource() { Stream = new MemoryStream() }, zipDesiredName);
                    zipFile.CommitUpdate();

                    var wnt = new WindowsNameTransform(ContainerPath);
                    return new ZipStorageFile(wnt.TransformFile(zipDesiredName), ContainerPath) { BackingFile = BackingFile };
                    */
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
                using (ArchiveFile zipFile = await OpenZipFileAsync(FileAccessMode.ReadWrite))
                {
                    /*
                    if (zipFile == null)
                    {
                        return null;
                    }
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
                    return new ZipStorageFolder(wnt.TransformFile(zipDesiredName), ContainerPath)
                    {
                        ZipEncoding = ZipEncoding,
                        BackingFile = BackingFile
                    };
                    */
                    return null;
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
                using (ArchiveFile zipFile = await OpenZipFileAsync(FileAccessMode.Read))
                {
                    if (zipFile == null)
                    {
                        return null;
                    }
                    zipFile.IsStreamOwner = true;
                    ZipEncoding ??= DetectFileEncoding(zipFile);
                    var entry = zipFile.Entries.FirstOrDefault(x => x.FileName == name);
                    if (entry != null)
                    {
                        //var wnt = new WindowsNameTransform(ContainerPath);
                        if (entry.IsFolder)
                        {
                            //return new ZipStorageFolder(wnt.TransformDirectory(DecodeEntryName(entry, ZipEncoding)), ContainerPath, entry)
                            return new ZipStorageFolder(DecodeEntryName(entry, ZipEncoding), ContainerPath, entry)
                            {
                                ZipEncoding = ZipEncoding,
                                BackingFile = BackingFile
                            };
                        }
                        else
                        {
                            //return new ZipStorageFile(wnt.TransformFile(DecodeEntryName(entry, ZipEncoding)), ContainerPath, entry) { BackingFile = BackingFile };
                            return new ZipStorageFile(DecodeEntryName(entry, ZipEncoding), ContainerPath, entry) { BackingFile = BackingFile };
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
                using (ArchiveFile zipFile = await OpenZipFileAsync(FileAccessMode.Read))
                {
                    if (zipFile == null)
                    {
                        return null;
                    }
                    zipFile.IsStreamOwner = true;
                    ZipEncoding ??= DetectFileEncoding(zipFile);
                    //var wnt = new WindowsNameTransform(ContainerPath, true); // Check with Path.GetFullPath
                    var items = new List<IStorageItem>();
                    foreach (var entry in zipFile.Entries) // Returns all items recursively
                    {
                        string winPath = System.IO.Path.GetFullPath(entry.IsFolder ? DecodeEntryName(entry, ZipEncoding) : DecodeEntryName(entry, ZipEncoding));
                        //string winPath = System.IO.Path.GetFullPath(entry.IsDirectory ? wnt.TransformDirectory(DecodeEntryName(entry, ZipEncoding)) : wnt.TransformFile(DecodeEntryName(entry, ZipEncoding)));
                        if (winPath.StartsWith(Path.WithEnding("\\"), StringComparison.Ordinal)) // Child of self
                        {
                            var split = winPath.Substring(Path.Length).Split('\\', StringSplitOptions.RemoveEmptyEntries);
                            if (split.Length > 0)
                            {
                                if (entry.IsFolder || split.Length > 1) // Not all folders have a ZipEntry
                                {
                                    var itemPath = System.IO.Path.Combine(Path, split[0]);
                                    if (!items.Any(x => x.Path == itemPath))
                                    {
                                        items.Add(new ZipStorageFolder(itemPath, ContainerPath, entry)
                                        {
                                            ZipEncoding = ZipEncoding,
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
                return await GetBasicProperties();
            });
        }

        public override bool IsOfType(StorageItemTypes type) => type == StorageItemTypes.Folder;

        public override Windows.Storage.FileAttributes Attributes => Windows.Storage.FileAttributes.Directory;

        public override DateTimeOffset DateCreated { get; }

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
            return AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) =>
            {
                var marker = path.IndexOf(".zip", StringComparison.OrdinalIgnoreCase);
                if (marker != -1)
                {
                    var containerPath = path.Substring(0, marker + ".zip".Length);
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
            var marker = path.IndexOf(".zip", StringComparison.OrdinalIgnoreCase);
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

        private IAsyncOperation<ArchiveFile> OpenZipFileAsync(FileAccessMode accessMode)
        {
            return AsyncInfo.Run<ArchiveFile>(async (cancellationToken) =>
            {
                bool readWrite = accessMode == FileAccessMode.ReadWrite;
                if (BackingFile != null)
                {
                    return new ArchiveFile((await BackingFile.OpenAsync(accessMode)).AsStream());
                }
                else
                {
                    var hFile = NativeFileOperationsHelper.OpenFileForRead(ContainerPath, readWrite);
                    if (hFile.IsInvalid)
                    {
                        return null;
                    }
                    return new ArchiveFile((Stream)new FileStream(hFile, readWrite ? FileAccess.ReadWrite : FileAccess.Read));
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
                using (ArchiveFile zipFile = new ArchiveFile(stream))
                {
                    zipFile.IsStreamOwner = false;
                }
                return true;
            });
        }

        private async Task<BaseBasicProperties> GetBasicProperties()
        {
            using (ArchiveFile zipFile = await OpenZipFileAsync(FileAccessMode.Read))
            {
                if (zipFile == null)
                {
                    return new BaseBasicProperties();
                }
                zipFile.IsStreamOwner = true;
                //var znt = new ZipNameTransform(ContainerPath);
                //var entry = zipFile.GetEntry(znt.TransformFile(Path));
                var entry = zipFile.Entries.FirstOrDefault(x => x.FileName == Path);
                if (entry != null)
                {
                    return new ZipFolderBasicProperties(entry);
                }
                return new BaseBasicProperties();
            }
        }

        /*private class FileDataSource : IStaticDataSource
        {
            public Stream Stream { get; set; }

            public Stream GetSource() => Stream;
        }*/

        private class ZipFolderBasicProperties : BaseBasicProperties
        {
            private ZipEntry zipEntry;

            public ZipFolderBasicProperties(ZipEntry entry)
            {
                this.zipEntry = entry;
            }

            public override DateTimeOffset DateModified => zipEntry.CreationTime;

            public override DateTimeOffset ItemDate => zipEntry.CreationTime;

            public override ulong Size => (ulong)zipEntry.Size;
        }

        #endregion
    }
}
