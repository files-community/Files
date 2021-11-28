﻿using Files.Helpers;
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

namespace Files.Filesystem.StorageItems
{
    public sealed class ZipStorageFile : BaseStorageFile
    {
        public ZipStorageFile(string path, string containerPath)
        {
            Name = System.IO.Path.GetFileName(path.TrimEnd('\\', '/'));
            Path = path;
            ContainerPath = containerPath;
        }

        public ZipStorageFile(string path, string containerPath, ZipEntry entry)
        {
            Name = System.IO.Path.GetFileName(path.TrimEnd('\\', '/'));
            Path = path;
            ContainerPath = containerPath;
            DateCreated = entry.DateTime;
        }

        public override IAsyncOperation<StorageFile> ToStorageFileAsync()
        {
            return StorageFile.CreateStreamedFileAsync(Name, ZipDataStreamingHandler(Path), null);
        }

        public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode)
        {
            return AsyncInfo.Run<IRandomAccessStream>(async (cancellationToken) =>
            {
                bool rw = accessMode == FileAccessMode.ReadWrite;
                if (Path == ContainerPath)
                {
                    if (BackingFile != null)
                    {
                        return await BackingFile.OpenAsync(accessMode);
                    }
                    else
                    {
                        var hFile = NativeFileOperationsHelper.OpenFileForRead(ContainerPath, rw);
                        if (hFile.IsInvalid)
                        {
                            return null;
                        }
                        return new FileStream(hFile, rw ? FileAccess.ReadWrite : FileAccess.Read).AsRandomAccessStream();
                    }
                }

                ZipFile zipFile = await OpenZipFileAsync(accessMode);
                if (zipFile == null)
                {
                    return null;
                }
                zipFile.IsStreamOwner = true;
                var znt = new ZipNameTransform(ContainerPath);
                var entry = zipFile.GetEntry(znt.TransformFile(Path));
                if (!rw)
                {
                    if (entry != null)
                    {
                        return new NonSeekableRandomAccessStream(zipFile.GetInputStream(entry), (ulong)entry.Size)
                        {
                            DisposeCallback = () => zipFile.Close()
                        };
                    }
                }
                else
                {
                    return new RandomAccessStreamWithFlushCallback()
                    {
                        DisposeCallback = () => zipFile.Close(),
                        FlushCallback = WriteZipEntry(zipFile)
                    };
                }
                return null;
            });
        }

        public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync() => throw new NotSupportedException();

        public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder)
        {
            return CopyAsync(destinationFolder, Name, NameCollisionOption.FailIfExists);
        }

        public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName)
        {
            return CopyAsync(destinationFolder, desiredNewName, NameCollisionOption.FailIfExists);
        }

        public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
        {
            return AsyncInfo.Run<BaseStorageFile>(async (cancellationToken) =>
            {
                using (ZipFile zipFile = await OpenZipFileAsync(FileAccessMode.Read))
                {
                    if (zipFile == null)
                    {
                        return null;
                    }
                    zipFile.IsStreamOwner = true;
                    var znt = new ZipNameTransform(ContainerPath);
                    var entry = zipFile.GetEntry(znt.TransformFile(Path));
                    if (entry != null)
                    {
                        var destFolder = destinationFolder.AsBaseStorageFolder();
                        var destFile = await destFolder.CreateFileAsync(desiredNewName, option.Convert());
                        using (var inStream = zipFile.GetInputStream(entry))
                        using (var outStream = await destFile.OpenStreamForWriteAsync())
                        {
                            await inStream.CopyToAsync(outStream);
                            await outStream.FlushAsync();
                        }
                        return destFile;
                    }
                    return null;
                }
            });
        }

        public override IAsyncAction CopyAndReplaceAsync(IStorageFile fileToReplace)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                using (ZipFile zipFile = await OpenZipFileAsync(FileAccessMode.Read))
                {
                    if (zipFile == null)
                    {
                        return;
                    }
                    zipFile.IsStreamOwner = true;
                    var znt = new ZipNameTransform(ContainerPath);
                    var entry = zipFile.GetEntry(znt.TransformFile(Path));
                    if (entry != null)
                    {
                        using var hDestFile = fileToReplace.CreateSafeFileHandle(FileAccess.ReadWrite);
                        using (var inStream = zipFile.GetInputStream(entry))
                        using (var outStream = new FileStream(hDestFile, FileAccess.Write))
                        {
                            await inStream.CopyToAsync(outStream);
                            await outStream.FlushAsync();
                        }
                    }
                }
            });
        }

        public override IAsyncAction MoveAsync(IStorageFolder destinationFolder)
        {
            throw new NotSupportedException();
        }

        public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName)
        {
            throw new NotSupportedException();
        }

        public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
        {
            throw new NotSupportedException();
        }

        public override IAsyncAction MoveAndReplaceAsync(IStorageFile fileToReplace)
        {
            throw new NotSupportedException();
        }

        public override string ContentType => "application/octet-stream";

        public override string FileType => System.IO.Path.GetExtension(Name);

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
            return GetBasicProperties().AsAsyncOperation();
        }

        public override bool IsOfType(StorageItemTypes type) => type == StorageItemTypes.File;

        public override Windows.Storage.FileAttributes Attributes => Windows.Storage.FileAttributes.Normal | Windows.Storage.FileAttributes.ReadOnly;

        public override DateTimeOffset DateCreated { get; }

        public override string Name { get; }

        public override string Path { get; }

        public string ContainerPath { get; private set; }

        public BaseStorageFile BackingFile { get; set; }

        public override IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync()
        {
            return AsyncInfo.Run<IRandomAccessStreamWithContentType>(async (cancellationToken) =>
            {
                if (Path == ContainerPath)
                {
                    if (BackingFile != null)
                    {
                        return await BackingFile.OpenReadAsync();
                    }
                    else
                    {
                        var hFile = NativeFileOperationsHelper.OpenFileForRead(ContainerPath);
                        if (hFile.IsInvalid)
                        {
                            return null;
                        }
                        return new StreamWithContentType(new FileStream(hFile, FileAccess.Read).AsRandomAccessStream());
                    }
                }

                ZipFile zipFile = await OpenZipFileAsync(FileAccessMode.Read);
                if (zipFile == null)
                {
                    return null;
                }
                zipFile.IsStreamOwner = true;
                var znt = new ZipNameTransform(ContainerPath);
                var entry = zipFile.GetEntry(znt.TransformFile(Path));
                if (entry != null)
                {
                    var nsStream = new NonSeekableRandomAccessStream(zipFile.GetInputStream(entry), (ulong)entry.Size)
                    {
                        DisposeCallback = () => zipFile.Close()
                    };
                    return new StreamWithContentType(nsStream);
                }
                return null;
            });
        }

        public override IAsyncOperation<IInputStream> OpenSequentialReadAsync()
        {
            return AsyncInfo.Run<IInputStream>(async (cancellationToken) =>
            {
                if (Path == ContainerPath)
                {
                    if (BackingFile != null)
                    {
                        return await BackingFile.OpenSequentialReadAsync();
                    }
                    else
                    {
                        var hFile = NativeFileOperationsHelper.OpenFileForRead(ContainerPath);
                        if (hFile.IsInvalid)
                        {
                            return null;
                        }
                        return new FileStream(hFile, FileAccess.Read).AsInputStream();
                    }
                }

                ZipFile zipFile = await OpenZipFileAsync(FileAccessMode.Read);
                if (zipFile == null)
                {
                    return null;
                }
                zipFile.IsStreamOwner = true;
                var znt = new ZipNameTransform(ContainerPath);
                var entry = zipFile.GetEntry(znt.TransformFile(Path));
                if (entry != null)
                {
                    return new InputStreamWithDisposeCallback(zipFile.GetInputStream(entry)) { DisposeCallback = () => zipFile.Close() };
                }
                return null;
            });
        }

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

        public override IAsyncOperation<BaseStorageFolder> GetParentAsync()
        {
            throw new NotSupportedException();
        }

        public override bool IsEqual(IStorageItem item) => item?.Path == Path;

        public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode, StorageOpenOptions options) => OpenAsync(accessMode);

        public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync(StorageOpenOptions options) => throw new NotSupportedException();

        public static IAsyncOperation<BaseStorageFile> FromPathAsync(string path)
        {
            return AsyncInfo.Run<BaseStorageFile>(cancellationToken =>
            {
                var marker = path.IndexOf(".zip", StringComparison.OrdinalIgnoreCase);
                if (marker != -1)
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

        public override string DisplayName => Name;

        public override string DisplayType
        {
            get
            {
                var itemType = "ItemTypeFile".GetLocalized();
                if (Name.Contains(".", StringComparison.Ordinal))
                {
                    itemType = System.IO.Path.GetExtension(Name).Trim('.') + " " + itemType;
                }
                return itemType;
            }
        }

        public override string FolderRelativeId => $"0\\{Name}";

        public override IStorageItemExtraProperties Properties => new BaseBasicStorageItemExtraProperties(this);

        #region Private

        private IAsyncOperation<ZipFile> OpenZipFileAsync(FileAccessMode accessMode, bool openProtected = false)
        {
            return AsyncInfo.Run<ZipFile>(async (cancellationToken) =>
            {
                bool readWrite = accessMode == FileAccessMode.ReadWrite;
                if (BackingFile != null)
                {
                    return new ZipFile((await BackingFile.OpenAsync(accessMode)).AsStream());
                }
                else
                {
                    var hFile = openProtected ?
                        await NativeFileOperationsHelper.OpenProtectedFileForRead(ContainerPath) :
                        NativeFileOperationsHelper.OpenFileForRead(ContainerPath, readWrite);
                    if (hFile.IsInvalid)
                    {
                        return null;
                    }
                    return new ZipFile(new FileStream(hFile, readWrite ? FileAccess.ReadWrite : FileAccess.Read));
                }
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
                    using (ZipFile zipFile = await OpenZipFileAsync(FileAccessMode.Read, openProtected: true))
                    {
                        if (zipFile == null)
                        {
                            request.FailAndClose(StreamedFileFailureMode.CurrentlyUnavailable);
                            return;
                        }
                        zipFile.IsStreamOwner = true;
                        var znt = new ZipNameTransform(ContainerPath);
                        var entry = zipFile.GetEntry(znt.TransformFile(name));
                        if (entry != null)
                        {
                            using (var inStream = zipFile.GetInputStream(entry))
                            using (var outStream = request.AsStreamForWrite())
                            {
                                await inStream.CopyToAsync(outStream);
                                await outStream.FlushAsync();
                            }
                            request.Dispose();
                        }
                        else
                        {
                            request.FailAndClose(StreamedFileFailureMode.CurrentlyUnavailable);
                        }
                    }
                }
                catch
                {
                    request.FailAndClose(StreamedFileFailureMode.Failed);
                }
            };
        }

        private Func<IRandomAccessStream, IAsyncOperation<bool>> WriteZipEntry(ZipFile zipFile)
        {
            return (stream) => AsyncInfo.Run((cancellationToken) => Task.Run(() =>
            {
                try
                {
                    var znt = new ZipNameTransform(ContainerPath);
                    var zipDesiredName = znt.TransformFile(Path);
                    var entry = zipFile.GetEntry(zipDesiredName);

                    zipFile.BeginUpdate(new MemoryArchiveStorage(FileUpdateMode.Direct));
                    if (entry != null)
                    {
                        zipFile.Delete(entry);
                    }
                    zipFile.Add(new StreamDataSource(stream), zipDesiredName);
                    zipFile.CommitUpdate();
                }
                catch (Exception ex)
                {
                    App.Logger.Warn(ex, "Error writing zip file");
                }
                return true;
            }));
        }

        private async Task<BaseBasicProperties> GetBasicProperties()
        {
            using (ZipFile zipFile = await OpenZipFileAsync(FileAccessMode.Read))
            {
                if (zipFile == null)
                {
                    return null;
                }
                zipFile.IsStreamOwner = true;
                var znt = new ZipNameTransform(ContainerPath);
                var entry = zipFile.GetEntry(znt.TransformFile(Path));
                if (entry != null)
                {
                    return new ZipFileBasicProperties(entry);
                }
                return new BaseBasicProperties();
            }
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

        private class ZipFileBasicProperties : BaseBasicProperties
        {
            private ZipEntry zipEntry;

            public ZipFileBasicProperties(ZipEntry entry)
            {
                this.zipEntry = entry;
            }

            public override DateTimeOffset DateModified => zipEntry.DateTime;

            public override DateTimeOffset ItemDate => zipEntry.DateTime;

            public override ulong Size => (ulong)zipEntry.Size;
        }

        private class StreamDataSource : IStaticDataSource
        {
            private IRandomAccessStream stream;

            public StreamDataSource(IRandomAccessStream stream)
            {
                this.stream = stream;
            }

            public Stream GetSource() => stream.CloneStream().AsStream();
        }

        #endregion
    }
}
