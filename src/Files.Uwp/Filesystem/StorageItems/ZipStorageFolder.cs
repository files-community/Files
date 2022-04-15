using Files.Uwp.Extensions;
using Files.Uwp.Helpers;
using Files.Shared.Extensions;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Toolkit.Uwp;
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
using IO = System.IO;

namespace Files.Uwp.Filesystem.StorageItems
{
    public sealed class ZipStorageFolder : BaseStorageFolder
    {
        private static bool? isDefaultZipApp;

        private Encoding encoding;

        private string containerPath;
        private BaseStorageFile backingFile;

        public override string Path { get; }
        public override string Name { get; }

        public override DateTimeOffset DateCreated { get; }

        public override string DisplayName => Name;
        public override string DisplayType => "FileFolderListItem".GetLocalized();
        public override string FolderRelativeId => $"0\\{Name}";

        public override IStorageItemExtraProperties Properties => new BaseBasicStorageItemExtraProperties(this);


        static ZipStorageFolder()
        {
            // Register all supported codepages (default is UTF-X only)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            // Use extended ascii so you can convert the string back to bytes
            ZipStrings.CodePage = Constants.Filesystem.ExtendedAsciiCodePage;
        }

        public ZipStorageFolder(string path, string containerPath)
        {
            Name = IO.Path.GetFileName(path.TrimEnd('\\', '/'));
            Path = path;
            this.containerPath = containerPath;
        }
        public ZipStorageFolder(string path, string containerPath, ZipEntry entry) : this(path, containerPath)
        {
            DateCreated = entry.DateTime;
        }
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

        public static bool IsZipPath(string path)
        {
            var marker = path.IndexOf(".zip", StringComparison.OrdinalIgnoreCase);
            if (marker is not -1)
            {
                marker += ".zip".Length;
                if (marker == path.Length || path[marker] == '\\')
                {
                    return true;
                }
            }
            return false;
        }

        public static async Task<bool> CheckDefaultZipApp(string filePath)
        {
            Func<Task<bool>> queryFileAssoc = async () =>
            {
                var assoc = await NativeWinApiHelper.GetFileAssociationAsync(filePath);
                if (assoc is not null)
                {
                    isDefaultZipApp = assoc == Package.Current.Id.FamilyName
                        || assoc.Equals(IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe")
                            , StringComparison.OrdinalIgnoreCase);
                }
                return true;
            };
            return isDefaultZipApp ?? await queryFileAssoc();
        }

        public static string DecodeEntryName(ZipEntry entry, Encoding encoding)
        {
            if (encoding == null || entry.IsUnicodeText)
            {
                return entry.Name;
            }
            var decoded = SafetyExtensions.IgnoreExceptions(() =>
            {
                var rawBytes = Encoding.GetEncoding(Constants.Filesystem.ExtendedAsciiCodePage).GetBytes(entry.Name);
                return encoding.GetString(rawBytes);
            });
            return decoded ?? entry.Name;
        }

        public static Encoding DetectFileEncoding(ZipFile zipFile)
        {
            long readEntries = 0;
            Ude.CharsetDetector cdet = new();
            foreach (var entry in zipFile.OfType<ZipEntry>())
            {
                readEntries++;
                if (entry.IsUnicodeText)
                {
                    return Encoding.UTF8;
                }
                var guessedEncoding = SafetyExtensions.IgnoreExceptions(() =>
                {
                    var rawBytes = Encoding.GetEncoding(Constants.Filesystem.ExtendedAsciiCodePage).GetBytes(entry.Name);
                    cdet.Feed(rawBytes, 0, rawBytes.Length);
                    cdet.DataEnd();
                    if (cdet.Charset != null && cdet.Confidence >= 0.9 && (readEntries >= Math.Min(zipFile.Count, 50)))
                    {
                        return Encoding.GetEncoding(cdet.Charset);
                    }
                    return null;
                });
                if (guessedEncoding != null)
                {
                    return guessedEncoding;
                }
            }
            return Encoding.UTF8;
        }


        public override IAsyncOperation<IStorageItem> GetItemAsync(string name)
        {
            return AsyncInfo.Run<IStorageItem>(async (cancellationToken) =>
            {
                using ZipFile zipFile = await OpenZipFileAsync(FileAccessMode.Read);
                if (zipFile is null)
                {
                    return null;
                }

                zipFile.IsStreamOwner = true;
                encoding ??= DetectFileEncoding(zipFile);
                var entry = zipFile.GetEntry(name);
                if (entry is null)
                {
                    return null;
                }

                var wnt = new WindowsNameTransform(containerPath);
                if (entry.IsDirectory)
                {
                    return new ZipStorageFolder(wnt.TransformDirectory(DecodeEntryName(entry, encoding)), containerPath, entry)
                    {
                        encoding = encoding,
                        backingFile = backingFile
                    };
                }

                return new ZipStorageFile(wnt.TransformFile(DecodeEntryName(entry, encoding)), containerPath, entry) { BackingFile = BackingFile };
            });
        }

        public override IAsyncOperation<BaseStorageFile> GetFileAsync(string name)
            => AsyncInfo.Run<BaseStorageFile>(async (cancellationToken) => await GetItemAsync(name) as ZipStorageFile);
        public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync()
            => AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken) => (await GetItemsAsync())?.OfType<ZipStorageFile>().ToList());

        public override IAsyncOperation<BaseStorageFolder> GetFolderAsync(string name)
            => AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) => await GetItemAsync(name) as ZipStorageFolder);
        public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync()
            => AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken) => (await GetItemsAsync())?.OfType<ZipStorageFolder>().ToList());

        public override IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName)
            => CreateFileAsync(desiredName, CreationCollisionOption.FailIfExists);
        public override IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options)
        {
            return AsyncInfo.Run<BaseStorageFile>(async (cancellationToken) =>
            {
                using ZipFile zipFile = await OpenZipFileAsync(FileAccessMode.ReadWrite);
                if (zipFile is null)
                {
                    return null;
                }

                zipFile.IsStreamOwner = true;
                var znt = new ZipNameTransform(containerPath);
                var zipDesiredName = znt.TransformFile(IO.Path.Combine(Path, desiredName));
                var entry = zipFile.GetEntry(zipDesiredName);
                zipFile.BeginUpdate(new MemoryArchiveStorage(FileUpdateMode.Direct));
                if (entry is not null)
                {
                    if (options is not CreationCollisionOption.ReplaceExisting)
                    {
                        zipFile.AbortUpdate();
                        return null;
                    }
                    zipFile.Delete(entry);
                }
                zipFile.Add(new FileDataSource(), zipDesiredName);
                zipFile.CommitUpdate();

                var wnt = new WindowsNameTransform(containerPath);
                return new ZipStorageFile(wnt.TransformFile(zipDesiredName), containerPath) { backingFile = backingFile };
            });
        }

        public override IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName)
            => CreateFolderAsync(desiredName, CreationCollisionOption.FailIfExists);
        public override IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName, CreationCollisionOption options)
        {
            return AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) =>
            {
                using ZipFile zipFile = await OpenZipFileAsync(FileAccessMode.ReadWrite);
                if (zipFile is null)
                {
                    return null;
                }

                zipFile.IsStreamOwner = true;
                var znt = new ZipNameTransform(containerPath);
                var zipDesiredName = znt.TransformDirectory(IO.Path.Combine(Path, desiredName));
                var entry = zipFile.GetEntry(zipDesiredName);
                zipFile.BeginUpdate(new MemoryArchiveStorage(FileUpdateMode.Direct));

                if (entry is not null)
                {
                    if (options is not CreationCollisionOption.ReplaceExisting)
                    {
                        zipFile.AbortUpdate();
                        return null;
                    }
                    zipFile.Delete(entry);
                }

                zipFile.AddDirectory(zipDesiredName);
                zipFile.CommitUpdate();

                var wnt = new WindowsNameTransform(containerPath);
                return new ZipStorageFolder(wnt.TransformFile(zipDesiredName), containerPath)
                {
                    encoding = encoding,
                    backingFile = backingFile
                };
            });
        }






        public override IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync()
        {
            return AsyncInfo.Run<IReadOnlyList<IStorageItem>>(async (cancellationToken) =>
            {
                using ZipFile zipFile = await OpenZipFileAsync(FileAccessMode.Read);
                if (zipFile == null)
                {
                    return null;
                }
                zipFile.IsStreamOwner = true;
                encoding ??= DetectFileEncoding(zipFile);
                var wnt = new WindowsNameTransform(ContainerPath, true); // Check with Path.GetFullPath
                var items = new List<IStorageItem>();
                foreach (var entry in zipFile.OfType<ZipEntry>()) // Returns all items recursively
                {
                    string winPath = System.IO.Path.GetFullPath(entry.IsDirectory ? wnt.TransformDirectory(DecodeEntryName(entry, encoding)) : wnt.TransformFile(DecodeEntryName(entry, encoding)));
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
                                        encoding = encoding,
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
            });
        }

        public override IAsyncAction RenameAsync(string desiredName)
            => throw new NotSupportedException();
        public override IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
            => throw new NotSupportedException();

        public override IAsyncAction DeleteAsync()
            => throw new NotSupportedException();
        public override IAsyncAction DeleteAsync(StorageDeleteOption option)
            => throw new NotSupportedException();

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
                using (ZipFile zipFile = new(stream))
                {
                    zipFile.IsStreamOwner = false;
                }
                return true;
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

        private async Task<BaseBasicProperties> GetBasicProperties()
        {
            using ZipFile zipFile = await OpenZipFileAsync(FileAccessMode.Read);
            if (zipFile is null)
            {
                return new BaseBasicProperties();
            }

            zipFile.IsStreamOwner = true;
            var znt = new ZipNameTransform(containerPath);
            var entry = zipFile.GetEntry(znt.TransformFile(Path));

            return entry is null
                ? new BaseBasicProperties()
                : new ZipFolderBasicProperties(entry);
        }

        private IAsyncOperation<ZipFile> OpenZipFileAsync(FileAccessMode accessMode)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                bool readWrite = accessMode is FileAccessMode.ReadWrite;
                if (backingFile is not null)
                {
                    return new ZipFile((await backingFile.OpenAsync(accessMode)).AsStream());
                }

                var hFile = NativeFileOperationsHelper.OpenFileForRead(containerPath, readWrite);
                if (hFile.IsInvalid)
                {
                    return null;
                }

                return new ZipFile((Stream)new FileStream(hFile, readWrite ? FileAccess.ReadWrite : FileAccess.Read));
            });
        }

        private class FileDataSource : IStaticDataSource
        {
            private readonly Stream stream = new MemoryStream();

            public Stream GetSource() => stream;
        }

        private class ZipFolderBasicProperties : BaseBasicProperties
        {
            private readonly ZipEntry entry;

            public ZipFolderBasicProperties(ZipEntry entry) => this.entry = entry;

            public override ulong Size => (ulong)entry.Size;

            public override DateTimeOffset ItemDate => entry.DateTime;
            public override DateTimeOffset DateModified => entry.DateTime;
        }
    }
}
