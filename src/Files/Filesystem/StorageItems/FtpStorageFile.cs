using Files.Common;
using Files.Helpers;
using FluentFTP;
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
    public sealed class FtpStorageFile : BaseStorageFile
    {
        public FtpStorageFile(FtpItem ftpItem)
        {
            DateCreated = ftpItem.ItemDateCreatedReal;
            Name = ftpItem.ItemNameRaw;
            Path = ftpItem.ItemPath;
            FtpPath = FtpHelpers.GetFtpPath(ftpItem.ItemPath);
        }

        public FtpStorageFile(string folder, FtpListItem ftpItem)
        {
            DateCreated = ftpItem.RawCreated < DateTime.FromFileTimeUtc(0) ? DateTimeOffset.MinValue : ftpItem.RawCreated;
            Name = ftpItem.Name;
            Path = PathNormalization.Combine(folder, ftpItem.Name);
            FtpPath = FtpHelpers.GetFtpPath(Path);
        }

        public FtpStorageFile(IStorageItemWithPath item)
        {
            Name = System.IO.Path.GetFileName(item.Path);
            Path = item.Path;
            FtpPath = FtpHelpers.GetFtpPath(item.Path);
        }

        private async void FtpDataStreamingHandler(StreamedFileDataRequest request)
        {
            try
            {
                using var ftpClient = new FtpClient();
                ftpClient.Host = FtpHelpers.GetFtpHost(Path);
                ftpClient.Port = FtpHelpers.GetFtpPort(Path);
                ftpClient.Credentials = FtpManager.Credentials.Get(ftpClient.Host, FtpManager.Anonymous);

                if (!await ftpClient.EnsureConnectedAsync())
                {
                    request.FailAndClose(StreamedFileFailureMode.CurrentlyUnavailable);
                    return;
                }

                using (var outStream = request.AsStreamForWrite())
                {
                    await ftpClient.DownloadAsync(outStream, FtpPath);
                    await outStream.FlushAsync();
                }
                request.Dispose();
            }
            catch
            {
                request.FailAndClose(StreamedFileFailureMode.Incomplete);
            }
        }

        public override IAsyncOperation<StorageFile> ToStorageFileAsync()
        {
            return StorageFile.CreateStreamedFileAsync(Name, FtpDataStreamingHandler, null);
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

                if (!await ftpClient.MoveFileAsync(FtpPath,
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

                await ftpClient.DeleteFileAsync(FtpPath, cancellationToken);
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
                    return new FtpFileBasicProperties(item);
                }
                return new BaseBasicProperties();
            });
        }

        public override bool IsOfType(StorageItemTypes type) => type == StorageItemTypes.File;

        public override Windows.Storage.FileAttributes Attributes { get; } = Windows.Storage.FileAttributes.Normal;

        public override DateTimeOffset DateCreated { get; }

        public override string Name { get; }

        public override string Path { get; }

        public string FtpPath { get; }

        public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode)
        {
            return AsyncInfo.Run<IRandomAccessStream>(async (cancellationToken) =>
            {
                var ftpClient = new FtpClient();
                ftpClient.Host = FtpHelpers.GetFtpHost(Path);
                ftpClient.Port = FtpHelpers.GetFtpPort(Path);
                ftpClient.Credentials = FtpManager.Credentials.Get(ftpClient.Host, FtpManager.Anonymous);

                if (!await ftpClient.EnsureConnectedAsync())
                {
                    return null;
                }

                if (accessMode == FileAccessMode.Read)
                {
                    var inStream = await ftpClient.OpenReadAsync(FtpPath, cancellationToken);
                    return new NonSeekableRandomAccessStreamForRead(inStream, (ulong)inStream.Length)
                    {
                        DisposeCallback = () => ftpClient.Dispose()
                    };
                }
                else
                {
                    return new NonSeekableRandomAccessStreamForWrite(await ftpClient.OpenWriteAsync(FtpPath, cancellationToken))
                    {
                        DisposeCallback = () => ftpClient.Dispose()
                    };
                }
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

                BaseStorageFolder destFolder = destinationFolder.AsBaseStorageFolder();
                BaseStorageFile file = await destFolder.CreateFileAsync(desiredNewName, option.Convert());
                var stream = await file.OpenStreamForWriteAsync();

                if (await ftpClient.DownloadAsync(stream, FtpPath, token: cancellationToken))
                {
                    return file;
                }

                return null;
            });
        }

        public override IAsyncAction CopyAndReplaceAsync(IStorageFile fileToReplace) => throw new NotSupportedException();
        public override IAsyncAction MoveAsync(IStorageFolder destinationFolder) => throw new NotSupportedException();
        public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName) => throw new NotSupportedException();
        public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option) => throw new NotSupportedException();
        public override IAsyncAction MoveAndReplaceAsync(IStorageFile fileToReplace) => throw new NotSupportedException();

        public override string ContentType { get; } = "application/octet-stream";

        public override string FileType => System.IO.Path.GetExtension(Name);

        public override IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync()
        {
            return AsyncInfo.Run<IRandomAccessStreamWithContentType>(async (cancellationToken) =>
            {
                var ftpClient = new FtpClient();
                ftpClient.Host = FtpHelpers.GetFtpHost(Path);
                ftpClient.Port = FtpHelpers.GetFtpPort(Path);
                ftpClient.Credentials = FtpManager.Credentials.Get(ftpClient.Host, FtpManager.Anonymous);

                if (!await ftpClient.EnsureConnectedAsync())
                {
                    return null;
                }

                var inStream = await ftpClient.OpenReadAsync(FtpPath, cancellationToken);
                var nsStream = new NonSeekableRandomAccessStreamForRead(inStream, (ulong)inStream.Length)
                {
                    DisposeCallback = () => ftpClient.Dispose()
                };
                return new StreamWithContentType(nsStream);
            });
        }

        public override IAsyncOperation<IInputStream> OpenSequentialReadAsync()
        {
            return AsyncInfo.Run<IInputStream>(async (cancellationToken) =>
            {
                var ftpClient = new FtpClient();
                ftpClient.Host = FtpHelpers.GetFtpHost(Path);
                ftpClient.Port = FtpHelpers.GetFtpPort(Path);
                ftpClient.Credentials = FtpManager.Credentials.Get(ftpClient.Host, FtpManager.Anonymous);

                if (!await ftpClient.EnsureConnectedAsync())
                {
                    return null;
                }

                var inStream = await ftpClient.OpenReadAsync(FtpPath, cancellationToken);
                return new InputStreamWithDisposeCallback(inStream)
                {
                    DisposeCallback = () => ftpClient.Dispose()
                };
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
            if (FtpHelpers.IsFtpPath(path) && FtpHelpers.VerifyFtpPath(path))
            {
                return Task.FromResult<BaseStorageFile>(new FtpStorageFile(new StorageFileWithPath(null, path))).AsAsyncOperation();
            }
            return Task.FromResult<BaseStorageFile>(null).AsAsyncOperation();
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

        private class FtpFileBasicProperties : BaseBasicProperties
        {
            public FtpFileBasicProperties(FtpItem item)
            {
                DateModified = item.ItemDateModifiedReal;
                ItemDate = item.ItemDateCreatedReal;
                Size = (ulong)item.FileSizeBytes;
            }

            public FtpFileBasicProperties(FtpListItem item)
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