using Files.Uwp.Helpers;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Toolkit.Uwp;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using IO = System.IO;
using Storage = Windows.Storage;

namespace Files.Uwp.Filesystem.StorageItems
{
    public sealed class ZipStorageFile : BaseStorageFile
    {
        private readonly string containerPath;
        private readonly BaseStorageFile backingFile;

        public override string Path { get; }
        public override string Name { get; }
        public override string DisplayName => Name;
        public override string ContentType => "application/octet-stream";
        public override string FileType => IO.Path.GetExtension(Name);
        public override string FolderRelativeId => $"0\\{Name}";

        public override string DisplayType
        {
            get
            {
                var itemType = "ItemTypeFile".GetLocalized();
                if (Name.Contains(".", StringComparison.Ordinal))
                {
                    itemType = FileType.Trim('.') + " " + itemType;
                }
                return itemType;
            }
        }

        public override DateTimeOffset DateCreated { get; }
        public override Storage.FileAttributes Attributes => Storage.FileAttributes.Normal | Storage.FileAttributes.ReadOnly;

        private IStorageItemExtraProperties properties;
        public override IStorageItemExtraProperties Properties => properties ??= new BaseBasicStorageItemExtraProperties(this);

        public ZipStorageFile(string path, string containerPath)
        {
            Name = IO.Path.GetFileName(path.TrimEnd('\\', '/'));
            Path = path;
            this.containerPath = containerPath;
        }
        public ZipStorageFile(string path, string containerPath, BaseStorageFile backingFile) : this(path, containerPath)
            => this.backingFile = backingFile;
        public ZipStorageFile(string path, string containerPath, ZipEntry entry) : this(path, containerPath)
            => DateCreated = entry.DateTime;
        public ZipStorageFile(string path, string containerPath, ZipEntry entry, BaseStorageFile backingFile) : this(path, containerPath, entry)
            => this.backingFile = backingFile;

        public override IAsyncOperation<StorageFile> ToStorageFileAsync()
            => StorageFile.CreateStreamedFileAsync(Name, ZipDataStreamingHandler(Path), null);

        public static IAsyncOperation<BaseStorageFile> FromPathAsync(string path)
        {
            return AsyncInfo.Run(cancellationToken =>
            {
                var marker = path.IndexOf(".zip", StringComparison.OrdinalIgnoreCase);
                if (marker is not -1)
                {
                    var containerPath = path.Substring(0, marker + ".zip".Length);
                    if (path == containerPath)
                    {
                        return Task.FromResult<BaseStorageFile>(null); // Root
                    }
                    if (CheckAccess(containerPath))
                    {
                        return Task.FromResult<BaseStorageFile>(new ZipStorageFile(path, containerPath));
                    }
                }
                return Task.FromResult<BaseStorageFile>(null);
            });
        }

        public override bool IsEqual(IStorageItem item) => item?.Path == Path;
        public override bool IsOfType(StorageItemTypes type) => type is StorageItemTypes.File;

        public override IAsyncOperation<BaseStorageFolder> GetParentAsync() => throw new NotSupportedException();
        public override IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync() => GetBasicProperties().AsAsyncOperation();

        public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                bool rw = accessMode is FileAccessMode.ReadWrite;
                if (Path == containerPath)
                {
                    if (backingFile is not null)
                    {
                        return await backingFile.OpenAsync(accessMode);
                    }

                    var file = NativeFileOperationsHelper.OpenFileForRead(containerPath, rw);
                    if (file.IsInvalid)
                    {
                        return null;
                    }

                    return new FileStream(file, rw ? FileAccess.ReadWrite : FileAccess.Read).AsRandomAccessStream();
                }

                if (!rw)
                {
                    ZipFile zipFile = await OpenZipFileAsync(accessMode);
                    if (zipFile is null)
                    {
                        return null;
                    }

                    zipFile.IsStreamOwner = true;
                    var name = new ZipNameTransform(containerPath);
                    var entry = zipFile.GetEntry(name.TransformFile(Path));

                    if (entry is not null)
                    {
                        return new NonSeekableRandomAccessStreamForRead(zipFile.GetInputStream(entry), (ulong)entry.Size)
                        {
                            DisposeCallback = () => zipFile.Close()
                        };
                    }
                    return null;
                }

                var znt = new ZipNameTransform(containerPath);
                var zipDesiredName = znt.TransformFile(Path);

                using (ZipFile zipFile = await OpenZipFileAsync(accessMode))
                {
                    var entry = zipFile.GetEntry(zipDesiredName);
                    if (entry is not null)
                    {
                        zipFile.BeginUpdate(new MemoryArchiveStorage(FileUpdateMode.Direct));
                        zipFile.Delete(entry);
                        zipFile.CommitUpdate();
                    }
                }

                if (backingFile is not null)
                {
                    var stream = new ZipOutputStream((await backingFile.OpenAsync(FileAccessMode.ReadWrite)).AsStream(), true);
                    await stream.PutNextEntryAsync(new ZipEntry(zipDesiredName));
                    return new NonSeekableRandomAccessStreamForWrite(stream);
                }

                var hFile = NativeFileOperationsHelper.OpenFileForRead(containerPath, true);
                if (hFile.IsInvalid)
                {
                    return null;
                }

                var zos = new ZipOutputStream(new FileStream(hFile, FileAccess.ReadWrite), true);
                await zos.PutNextEntryAsync(new ZipEntry(zipDesiredName));
                return new NonSeekableRandomAccessStreamForWrite(zos);
            });
        }
        public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode, StorageOpenOptions options)
            => OpenAsync(accessMode);

        public override IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync()
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                if (Path == containerPath)
                {
                    if (backingFile is not null)
                    {
                        return await backingFile.OpenReadAsync();
                    }

                    var hFile = NativeFileOperationsHelper.OpenFileForRead(containerPath);
                    if (hFile.IsInvalid)
                    {
                        return null;
                    }

                    return new StreamWithContentType(new FileStream(hFile, FileAccess.Read).AsRandomAccessStream());
                }

                ZipFile zipFile = await OpenZipFileAsync(FileAccessMode.Read);
                if (zipFile is null)
                {
                    return null;
                }

                zipFile.IsStreamOwner = true;
                var znt = new ZipNameTransform(containerPath);
                var entry = zipFile.GetEntry(znt.TransformFile(Path));
                if (entry is null)
                {
                    return null;
                }

                var nsStream = new NonSeekableRandomAccessStreamForRead(zipFile.GetInputStream(entry), (ulong)entry.Size)
                {
                    DisposeCallback = () => zipFile.Close()
                };
                return new StreamWithContentType(nsStream);
            });
        }

        public override IAsyncOperation<IInputStream> OpenSequentialReadAsync()
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                if (Path == containerPath)
                {
                    if (backingFile is not null)
                    {
                        return await backingFile.OpenSequentialReadAsync();
                    }

                    var hFile = NativeFileOperationsHelper.OpenFileForRead(containerPath);
                    if (hFile.IsInvalid)
                    {
                        return null;
                    }

                    return new FileStream(hFile, FileAccess.Read).AsInputStream();
                }

                ZipFile zipFile = await OpenZipFileAsync(FileAccessMode.Read);
                if (zipFile is null)
                {
                    return null;
                }

                zipFile.IsStreamOwner = true;
                var znt = new ZipNameTransform(containerPath);
                var entry = zipFile.GetEntry(znt.TransformFile(Path));
                if (entry is null)
                {
                    return null;
                }

                return new InputStreamWithDisposeCallback(zipFile.GetInputStream(entry)) { DisposeCallback = () => zipFile.Close() };
            });
        }

        public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync()
            => throw new NotSupportedException();
        public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync(StorageOpenOptions options)
            => throw new NotSupportedException();

        public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder)
            => CopyAsync(destinationFolder, Name, NameCollisionOption.FailIfExists);
        public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName)
            => CopyAsync(destinationFolder, desiredNewName, NameCollisionOption.FailIfExists);
        public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                using ZipFile zipFile = await OpenZipFileAsync(FileAccessMode.Read);
                if (zipFile is null)
                {
                    return null;
                }

                zipFile.IsStreamOwner = true;
                var znt = new ZipNameTransform(containerPath);
                var entry = zipFile.GetEntry(znt.TransformFile(Path));
                if (entry is null)
                {
                    return null;
                }

                var destFolder = destinationFolder.AsBaseStorageFolder();
                var destFile = await destFolder.CreateFileAsync(desiredNewName, option.Convert());

                using var inStream = zipFile.GetInputStream(entry);
                using var outStream = await destFile.OpenStreamForWriteAsync();

                await inStream.CopyToAsync(outStream);
                await outStream.FlushAsync();

                return destFile;
            });
        }
        public override IAsyncAction CopyAndReplaceAsync(IStorageFile fileToReplace)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                using ZipFile zipFile = await OpenZipFileAsync(FileAccessMode.Read);
                if (zipFile is null)
                {
                    return;
                }

                zipFile.IsStreamOwner = true;
                var znt = new ZipNameTransform(containerPath);
                var entry = zipFile.GetEntry(znt.TransformFile(Path));
                if (entry is null)
                {
                    return;
                }

                using var hDestFile = fileToReplace.CreateSafeFileHandle(FileAccess.ReadWrite);
                using var inStream = zipFile.GetInputStream(entry);
                using var outStream = new FileStream(hDestFile, FileAccess.Write);

                await inStream.CopyToAsync(outStream);
                await outStream.FlushAsync();
            });
        }

        public override IAsyncAction MoveAsync(IStorageFolder destinationFolder)
            => throw new NotSupportedException();
        public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName)
            => throw new NotSupportedException();
        public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
            => throw new NotSupportedException();
        public override IAsyncAction MoveAndReplaceAsync(IStorageFile fileToReplace)
            => throw new NotSupportedException();

        public override IAsyncAction RenameAsync(string desiredName)
            => throw new NotSupportedException();
        public override IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
            => throw new NotSupportedException();

        public override IAsyncAction DeleteAsync()
            => throw new NotSupportedException();
        public override IAsyncAction DeleteAsync(StorageDeleteOption option)
            => throw new NotSupportedException();

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode)
            => Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize)
            => Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
            => Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();

        private static bool CheckAccess(string path)
        {
            try
            {
                var hFile = NativeFileOperationsHelper.OpenFileForRead(path);
                if (hFile.IsInvalid)
                {
                    return false;
                }
                using (ZipFile zipFile = new(new FileStream(hFile, FileAccess.Read)))
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

        private async Task<BaseBasicProperties> GetBasicProperties()
        {
            using ZipFile zipFile = await OpenZipFileAsync(FileAccessMode.Read);
            if (zipFile is null)
            {
                return null;
            }

            zipFile.IsStreamOwner = true;
            var znt = new ZipNameTransform(containerPath);
            var entry = zipFile.GetEntry(znt.TransformFile(Path));

            return entry is null
                ? new BaseBasicProperties()
                : new ZipFileBasicProperties(entry);
        }

        private IAsyncOperation<ZipFile> OpenZipFileAsync(FileAccessMode accessMode, bool openProtected = false)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                bool readWrite = accessMode == FileAccessMode.ReadWrite;
                if (backingFile is not null)
                {
                    return new ZipFile((await backingFile.OpenAsync(accessMode)).AsStream());
                }

                var hFile = openProtected
                    ? await NativeFileOperationsHelper.OpenProtectedFileForRead(containerPath)
                    : NativeFileOperationsHelper.OpenFileForRead(containerPath, readWrite);
                if (hFile.IsInvalid)
                {
                    return null;
                }

                return new ZipFile((Stream)new FileStream(hFile, readWrite ? FileAccess.ReadWrite : FileAccess.Read));
            });
        }

        private StreamedFileDataRequestedHandler ZipDataStreamingHandler(string name)
        {
            return async request =>
            {
                try
                {
                    // If called from here it fails with Access Denied?!
                    //var hFile = NativeFileOperationsHelper.OpenFileForRead(ContainerPath);
                    using ZipFile zipFile = await OpenZipFileAsync(FileAccessMode.Read, openProtected: true);
                    if (zipFile is null)
                    {
                        request.FailAndClose(StreamedFileFailureMode.CurrentlyUnavailable);
                        return;
                    }

                    zipFile.IsStreamOwner = true;
                    var znt = new ZipNameTransform(containerPath);
                    var entry = zipFile.GetEntry(znt.TransformFile(name));
                    if (entry is null)
                    {
                        request.FailAndClose(StreamedFileFailureMode.CurrentlyUnavailable);
                    }
                    else
                    {
                        using (var inStream = zipFile.GetInputStream(entry))
                        using (var outStream = request.AsStreamForWrite())
                        {
                            await inStream.CopyToAsync(outStream);
                            await outStream.FlushAsync();
                        }
                        request.Dispose();
                    }
                }
                catch
                {
                    request.FailAndClose(StreamedFileFailureMode.Failed);
                }
            };
        }

        private class ZipFileBasicProperties : BaseBasicProperties
        {
            private readonly ZipEntry entry;

            public ZipFileBasicProperties(ZipEntry entry) => this.entry = entry;

            public override ulong Size => (ulong)entry.Size;

            public override DateTimeOffset ItemDate => entry.DateTime;
            public override DateTimeOffset DateModified => entry.DateTime;
        }
    }
}
