﻿using Files.Helpers;
using Files.ViewModels;
using FluentFTP;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

namespace Files.Filesystem.StorageItems
{
    sealed class FtpStorageFile : IStorageFile
    {
        private readonly ItemViewModel _viewModel;
        private readonly FtpItem _ftpItem;

        public FtpStorageFile(ItemViewModel viewModel, FtpItem ftpItem)
        {
            _viewModel = viewModel;
            _ftpItem = ftpItem;
            DateCreated = _ftpItem.ItemDateCreatedReal;
            Name = _ftpItem.ItemName;
            Path = _ftpItem.ItemPath;
            FtpPath = FtpHelpers.GetFtpPath(_ftpItem.ItemPath);
            FileType = _ftpItem.ItemType;
        }

        public IAsyncOperation<StorageFile> ToStorageFileAsync()
        {
            return StorageFile.CreateStreamedFileAsync(Name, async request =>
            {
                var ftpClient = _viewModel.GetFtpInstance();
                if (!await ftpClient.EnsureConnectedAsync())
                {
                    request.FailAndClose(StreamedFileFailureMode.CurrentlyUnavailable);
                    return;
                }
                try
                {
                    using var stream = request.AsStreamForWrite();
                    await ftpClient.DownloadAsync(stream, FtpPath);
                    await request.FlushAsync();
                }
                catch
                {
                    request.FailAndClose(StreamedFileFailureMode.Incomplete);
                }

            }, null);
        }

        public IAsyncAction RenameAsync(string desiredName)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                var ftpClient = _viewModel.GetFtpInstance();
                if (!await ftpClient.EnsureConnectedAsync())
                {
                    return;
                }

                if (!await ftpClient.MoveFileAsync(FtpPath,
                    $"{FtpHelpers.GetFtpDirectoryName(FtpPath)}/{desiredName}",
                    FtpRemoteExists.Skip,
                    cancellationToken))
                {
                    // TODO: handle existing
                }
            });
        }

        public IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                var ftpClient = _viewModel.GetFtpInstance();
                if (!await ftpClient.EnsureConnectedAsync())
                {
                    return;
                }

                if (!await ftpClient.MoveFileAsync(FtpPath,
                    $"{FtpHelpers.GetFtpDirectoryName(FtpPath)}/{desiredName}",
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

        public IAsyncAction DeleteAsync()
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                var ftpClient = _viewModel.GetFtpInstance();
                if (!await ftpClient.EnsureConnectedAsync())
                {
                    return;
                }

                await ftpClient.DeleteFileAsync(FtpPath, cancellationToken);
            });
        }

        public IAsyncAction DeleteAsync(StorageDeleteOption option)
        {
            return DeleteAsync();
        }

        public IAsyncOperation<BasicProperties> GetBasicPropertiesAsync()
        {
            throw new NotSupportedException($"Use {nameof(ToStorageFileAsync)} instead.");
        }

        public bool IsOfType(StorageItemTypes type) => type == StorageItemTypes.File;

        public Windows.Storage.FileAttributes Attributes { get; } = Windows.Storage.FileAttributes.Normal;

        public DateTimeOffset DateCreated { get; }

        public string Name { get; }

        public string Path { get; }

        public string FtpPath { get; }

        public IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode) => throw new NotSupportedException();
        
        public IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync() => throw new NotSupportedException();
        
        public IAsyncOperation<StorageFile> CopyAsync(IStorageFolder destinationFolder)
        {
            return CopyAsync(destinationFolder, Name, NameCollisionOption.FailIfExists);
        }
        
        public IAsyncOperation<StorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName)
        {
            return CopyAsync(destinationFolder, desiredNewName, NameCollisionOption.FailIfExists);
        }
        
        public IAsyncOperation<StorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                var ftpClient = _viewModel.GetFtpInstance();

                if (!await ftpClient.EnsureConnectedAsync())
                {
                    return null;
                }

                var createOption = option switch
                {
                    NameCollisionOption.FailIfExists => CreationCollisionOption.FailIfExists,
                    NameCollisionOption.GenerateUniqueName => CreationCollisionOption.GenerateUniqueName,
                    NameCollisionOption.ReplaceExisting => CreationCollisionOption.ReplaceExisting,
                    _ => CreationCollisionOption.FailIfExists
                };

                var file = await destinationFolder.CreateFileAsync(desiredNewName, createOption);
                var stream = await file.OpenStreamForWriteAsync();

                if (await ftpClient.DownloadAsync(stream, FtpPath, token: cancellationToken))
                {
                    return file;
                }

                return null;
            });
        }
        
        public IAsyncAction CopyAndReplaceAsync(IStorageFile fileToReplace)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                var ftpClient = _viewModel.GetFtpInstance();

                if (!await ftpClient.EnsureConnectedAsync())
                {
                    return;
                }

                var stream = await fileToReplace.OpenStreamForWriteAsync();
                await ftpClient.DownloadAsync(stream, FtpPath, token: cancellationToken);
            });
        }
        
        public IAsyncAction MoveAsync(IStorageFolder destinationFolder)
        {
            return MoveAsync(destinationFolder, Name, NameCollisionOption.FailIfExists);
        }

        public IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName)
        {
            return MoveAsync(destinationFolder, desiredNewName, NameCollisionOption.FailIfExists);
        }

        public IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                var ftpClient = _viewModel.GetFtpInstance();

                if (!await ftpClient.EnsureConnectedAsync())
                {
                    return;
                }

                var createOption = option switch
                {
                    NameCollisionOption.FailIfExists => CreationCollisionOption.FailIfExists,
                    NameCollisionOption.GenerateUniqueName => CreationCollisionOption.GenerateUniqueName,
                    NameCollisionOption.ReplaceExisting => CreationCollisionOption.ReplaceExisting,
                    _ => CreationCollisionOption.FailIfExists
                };

                var file = await destinationFolder.CreateFileAsync(desiredNewName, createOption);
                var stream = await file.OpenStreamForWriteAsync();

                if (await ftpClient.DownloadAsync(stream, FtpPath, token: cancellationToken))
                {
                    await ftpClient.DeleteFileAsync(FtpPath, cancellationToken);
                }
            });
        }

        public IAsyncAction MoveAndReplaceAsync(IStorageFile fileToReplace)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                var ftpClient = _viewModel.GetFtpInstance();

                if (!await ftpClient.EnsureConnectedAsync())
                {
                    return;
                }

                var stream = await fileToReplace.OpenStreamForWriteAsync();
                if (await ftpClient.DownloadAsync(stream, FtpPath, token: cancellationToken))
                {
                    await ftpClient.DeleteFileAsync(FtpPath, cancellationToken);
                }
            });
        }

        public string ContentType { get; } = "application/octet-stream";

        public string FileType { get; }

        public IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync() => throw new NotSupportedException();

        public IAsyncOperation<IInputStream> OpenSequentialReadAsync()
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                var ftpClient = _viewModel.GetFtpInstance();

                if (!await ftpClient.EnsureConnectedAsync())
                {
                    return null;
                }

                var stream = await ftpClient.OpenReadAsync(FtpPath, cancellationToken);

                return stream.AsInputStream();
            });
        }
    }
}
