﻿using Files.Shared.Extensions;
using Files.Uwp.Helpers;
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
using IO = System.IO;
using Storage = Windows.Storage;

namespace Files.Uwp.Filesystem.StorageItems
{
    public sealed class FtpStorageFile : BaseStorageFile
    {
        public override string Path { get; }
        public override string Name { get; }
        public override string DisplayName => Name;
        public override string ContentType => "application/octet-stream";
        public override string FileType => IO.Path.GetExtension(Name);
        public string FtpPath { get; }
        public override string FolderRelativeId => $"0\\{Name}";

        public override string DisplayType
        {
            get
            {
                var itemType = "ItemTypeFile".GetLocalized();
                if (Name.Contains(".", StringComparison.Ordinal))
                {
                    itemType = IO.Path.GetExtension(Name).Trim('.') + " " + itemType;
                }
                return itemType;
            }
        }

        public override DateTimeOffset DateCreated { get; }
        public override Storage.FileAttributes Attributes { get; } = Storage.FileAttributes.Normal;
        public override IStorageItemExtraProperties Properties => new BaseBasicStorageItemExtraProperties(this);

        public FtpStorageFile(string path, string name, DateTimeOffset dateCreated)
        {
            Path = path;
            Name = name;
            FtpPath = FtpHelpers.GetFtpPath(path);
            DateCreated = dateCreated;
        }
        public FtpStorageFile(string folder, FtpListItem ftpItem)
        {
            Path = PathNormalization.Combine(folder, ftpItem.Name);
            Name = ftpItem.Name;
            FtpPath = FtpHelpers.GetFtpPath(Path);
            DateCreated = ftpItem.RawCreated < DateTime.FromFileTimeUtc(0) ? DateTimeOffset.MinValue : ftpItem.RawCreated;
        }
        public FtpStorageFile(IStorageItemWithPath item)
        {
            Path = item.Path;
            Name = IO.Path.GetFileName(item.Path);
            FtpPath = FtpHelpers.GetFtpPath(item.Path);
        }

        public static IAsyncOperation<BaseStorageFile> FromPathAsync(string path)
            => FtpHelpers.IsFtpPath(path) && FtpHelpers.VerifyFtpPath(path)
                ? Task.FromResult<BaseStorageFile>(new FtpStorageFile(new StorageFileWithPath(null, path))).AsAsyncOperation()
                : Task.FromResult<BaseStorageFile>(null).AsAsyncOperation();

        public override IAsyncOperation<StorageFile> ToStorageFileAsync()
            => StorageFile.CreateStreamedFileAsync(Name, FtpDataStreamingHandler, null);

        public override bool IsEqual(IStorageItem item) => item?.Path == Path;
        public override bool IsOfType(StorageItemTypes type) => type is StorageItemTypes.File;

        public override IAsyncOperation<BaseStorageFolder> GetParentAsync() => throw new NotSupportedException();

        public override IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync()
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                using var ftpClient = GetFtpClient();
                if (!await ftpClient.EnsureConnectedAsync())
                {
                    return new BaseBasicProperties();
                }

                var item = await ftpClient.GetObjectInfoAsync(FtpPath);
                return item is null ? new BaseBasicProperties() : new FtpFileBasicProperties(item);
            });
        }

        public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode)
        {
            return AsyncInfo.Run<IRandomAccessStream>(async (cancellationToken) =>
            {
                var ftpClient = GetFtpClient();
                if (!await ftpClient.EnsureConnectedAsync())
                {
                    return null;
                }

                if (accessMode is FileAccessMode.Read)
                {
                    var inStream = await ftpClient.OpenReadAsync(FtpPath, cancellationToken);
                    return new NonSeekableRandomAccessStreamForRead(inStream, (ulong)inStream.Length)
                    {
                        DisposeCallback = ftpClient.Dispose
                    };
                }
                return new NonSeekableRandomAccessStreamForWrite(await ftpClient.OpenWriteAsync(FtpPath, cancellationToken))
                {
                    DisposeCallback = ftpClient.Dispose
                };
            });
        }
        public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode, StorageOpenOptions options) => OpenAsync(accessMode);

        public override IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync()
        {
            return AsyncInfo.Run<IRandomAccessStreamWithContentType>(async (cancellationToken) =>
            {
                var ftpClient = GetFtpClient();
                if (!await ftpClient.EnsureConnectedAsync())
                {
                    return null;
                }

                var inStream = await ftpClient.OpenReadAsync(FtpPath, cancellationToken);
                var nsStream = new NonSeekableRandomAccessStreamForRead(inStream, (ulong)inStream.Length) { DisposeCallback = ftpClient.Dispose };
                return new StreamWithContentType(nsStream);
            });
        }
        public override IAsyncOperation<IInputStream> OpenSequentialReadAsync()
        {
            return AsyncInfo.Run<IInputStream>(async (cancellationToken) =>
            {
                var ftpClient = GetFtpClient();
                if (!await ftpClient.EnsureConnectedAsync())
                {
                    return null;
                }

                var inStream = await ftpClient.OpenReadAsync(FtpPath, cancellationToken);
                return new InputStreamWithDisposeCallback(inStream) { DisposeCallback = () => ftpClient.Dispose() };
            });
        }

        public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync() => throw new NotSupportedException();
        public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync(StorageOpenOptions options) => throw new NotSupportedException();

        public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder)
            => CopyAsync(destinationFolder, Name, NameCollisionOption.FailIfExists);
        public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName)
            => CopyAsync(destinationFolder, desiredNewName, NameCollisionOption.FailIfExists);
        public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                using var ftpClient = GetFtpClient();
                if (!await ftpClient.EnsureConnectedAsync())
                {
                    return null;
                }

                BaseStorageFolder destFolder = destinationFolder.AsBaseStorageFolder();
                BaseStorageFile file = await destFolder.CreateFileAsync(desiredNewName, option.Convert());

                var stream = await file.OpenStreamForWriteAsync();
                return await ftpClient.DownloadAsync(stream, FtpPath, token: cancellationToken) ? file : null;
            });
        }

        public override IAsyncAction MoveAsync(IStorageFolder destinationFolder) => throw new NotSupportedException();
        public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName) => throw new NotSupportedException();
        public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option) => throw new NotSupportedException();

        public override IAsyncAction CopyAndReplaceAsync(IStorageFile fileToReplace) => throw new NotSupportedException();
        public override IAsyncAction MoveAndReplaceAsync(IStorageFile fileToReplace) => throw new NotSupportedException();

        public override IAsyncAction RenameAsync(string desiredName)
            => RenameAsync(desiredName, NameCollisionOption.FailIfExists);
        public override IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                using var ftpClient = GetFtpClient();
                if (!await ftpClient.EnsureConnectedAsync())
                {
                    return;
                }

                string destination = $"{PathNormalization.GetParentDir(FtpPath)}/{desiredName}";
                var remoteExists = option is NameCollisionOption.ReplaceExisting ? FtpRemoteExists.Overwrite : FtpRemoteExists.Skip;
                bool isSuccessful = await ftpClient.MoveFileAsync(FtpPath, destination, remoteExists, cancellationToken);
                if (!isSuccessful && option is NameCollisionOption.GenerateUniqueName)
                {
                    // TODO: handle name generation
                }
            });
        }

        public override IAsyncAction DeleteAsync()
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                using var ftpClient = GetFtpClient();
                if (await ftpClient.EnsureConnectedAsync())
                {
                    await ftpClient.DeleteFileAsync(FtpPath, cancellationToken);
                }
            });
        }
        public override IAsyncAction DeleteAsync(StorageDeleteOption option) => DeleteAsync();

        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode)
            => Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize)
            => Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
        public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
            => Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();

        private FtpClient GetFtpClient()
        {
            string host = FtpHelpers.GetFtpHost(Path);
            ushort port = FtpHelpers.GetFtpPort(Path);
            var credentials = FtpManager.Credentials.Get(host, FtpManager.Anonymous);

            return new(host, port, credentials);
        }

        private async void FtpDataStreamingHandler(StreamedFileDataRequest request)
        {
            try
            {
                using var ftpClient = GetFtpClient();
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

        private class FtpFileBasicProperties : BaseBasicProperties
        {
            public override ulong Size { get; }

            public override DateTimeOffset ItemDate { get; }
            public override DateTimeOffset DateModified { get; }

            public FtpFileBasicProperties(FtpItem item)
            {
                Size = (ulong)item.FileSizeBytes;
                ItemDate = item.ItemDateCreatedReal;
                DateModified = item.ItemDateModifiedReal;
            }

            public FtpFileBasicProperties(FtpListItem item)
            {
                Size = (ulong)item.Size;
                ItemDate = item.RawCreated < DateTime.FromFileTimeUtc(0) ? DateTimeOffset.MinValue : item.RawCreated;
                DateModified = item.RawModified < DateTime.FromFileTimeUtc(0) ? DateTimeOffset.MinValue : item.RawModified;
            }
        }
    }
}