using Files.Helpers;
using Files.ViewModels;
using FluentFTP;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.BulkAccess;

namespace Files.Filesystem.StorageItems
{
    class FtpRandomAccessStreamWithContentType : IRandomAccessStreamWithContentType
    {
        private readonly IRandomAccessStream _stream;
        public FtpRandomAccessStreamWithContentType(IRandomAccessStream stream)
        {
            _stream = stream;
        }

        public IInputStream GetInputStreamAt(ulong position) => _stream.GetInputStreamAt(position);
        public IOutputStream GetOutputStreamAt(ulong position) => _stream.GetOutputStreamAt(position);
        public void Seek(ulong position) => _stream.Seek(position);
        public IRandomAccessStream CloneStream() => _stream.CloneStream();

        public bool CanRead => _stream.CanRead;

        public bool CanWrite => _stream.CanWrite;

        public ulong Position => _stream.Position;

        public ulong Size { get => _stream.Size; set => _stream.Size = value; }

        public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options) => _stream.ReadAsync(buffer, count, options);
        public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer) => _stream.WriteAsync(buffer);
        public IAsyncOperation<bool> FlushAsync() => _stream.FlushAsync();
        public void Dispose() => _stream.Dispose();

        public string ContentType { get; } = "application/octet-stream";
    }

    class FtpStorageFile : IStorageFile
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
            FileType = _ftpItem.ItemType;
        }

        public IAsyncAction RenameAsync(string desiredName)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                var ftpClient = _viewModel.GetFtpInstance();
                if (!ftpClient.IsConnected)
                {
                    return;
                }

                if (!await ftpClient.MoveFileAsync(Path,
                    $"{FtpHelpers.GetFtpDirectoryName(Path)}/{desiredName}",
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
                if (!ftpClient.IsConnected)
                {
                    return;
                }

                if (!await ftpClient.MoveFileAsync(Path,
                    $"{FtpHelpers.GetFtpDirectoryName(Path)}/{desiredName}",
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
                if (!ftpClient.IsConnected)
                {
                    return;
                }

                await ftpClient.DeleteFileAsync(Path, cancellationToken);
            });
        }
        public IAsyncAction DeleteAsync(StorageDeleteOption option)
        {
            return DeleteAsync();
        }

        public IAsyncOperation<BasicProperties> GetBasicPropertiesAsync()
        {
            return AsyncInfo.Run<BasicProperties>(async (cancellationToken) =>
            {
                // TODO: sigh... how to implement this?
                return null;
            });
        }

        public bool IsOfType(StorageItemTypes type) => type == StorageItemTypes.File;

        public Windows.Storage.FileAttributes Attributes { get; } = Windows.Storage.FileAttributes.Normal;

        public DateTimeOffset DateCreated { get; }

        public string Name { get; }

        public string Path { get; }

        public IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode)
        {
            var ftpClient = _viewModel.GetFtpInstance();

            var asyncInfo = AsyncInfo.Run(async cancellationToken =>
            {
                if (!ftpClient.IsConnected)
                {
                    return null;
                }

                if (accessMode == FileAccessMode.Read)
                {
                    var stream = await ftpClient.OpenReadAsync(Path, FtpDataType.Binary, cancellationToken);
                    return stream.AsRandomAccessStream();
                }
                else
                {
                    var stream = await ftpClient.OpenWriteAsync(Path, FtpDataType.Binary, cancellationToken);
                    return stream.AsRandomAccessStream();
                }
            });

            asyncInfo.Completed = (info, status) =>
            {
                if (!ftpClient.IsConnected || accessMode == FileAccessMode.Read)
                {
                    return;
                }

                if (status == AsyncStatus.Completed)
                {
                    ftpClient.GetReply();
                }
            };

            return asyncInfo;
        }
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

                if (!ftpClient.IsConnected)
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

                if (await ftpClient.DownloadAsync(stream, Path, token: cancellationToken))
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

                if (!ftpClient.IsConnected)
                {
                    return;
                }

                var stream = await fileToReplace.OpenStreamForWriteAsync();
                await ftpClient.DownloadAsync(stream, Path, token: cancellationToken);
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

                if (!ftpClient.IsConnected)
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

                if (await ftpClient.DownloadAsync(stream, Path, token: cancellationToken))
                {
                    await ftpClient.DeleteFileAsync(Path, cancellationToken);
                }
            });
        }
        public IAsyncAction MoveAndReplaceAsync(IStorageFile fileToReplace)
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                var ftpClient = _viewModel.GetFtpInstance();

                if (!ftpClient.IsConnected)
                {
                    return;
                }

                var stream = await fileToReplace.OpenStreamForWriteAsync();
                if (await ftpClient.DownloadAsync(stream, Path, token: cancellationToken))
                {
                    await ftpClient.DeleteFileAsync(Path, cancellationToken);
                }
            });
        }

        public string ContentType { get; } = "application/octet-stream";

        public string FileType { get; }

        public IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync()
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                var ftpClient = _viewModel.GetFtpInstance();

                if (!ftpClient.IsConnected)
                {
                    return null;
                }

                var stream = await ftpClient.OpenReadAsync(Path, cancellationToken);
                return new FtpRandomAccessStreamWithContentType(stream.AsRandomAccessStream()) as IRandomAccessStreamWithContentType;
            });
        }

        public IAsyncOperation<IInputStream> OpenSequentialReadAsync()
        {
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                var ftpClient = _viewModel.GetFtpInstance();

                if (!ftpClient.IsConnected)
                {
                    return null;
                }

                var stream = await ftpClient.OpenReadAsync(Path, cancellationToken);

                return stream.AsInputStream();
            });
        }
    }
}
