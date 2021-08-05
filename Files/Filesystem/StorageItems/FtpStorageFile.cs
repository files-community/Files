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
using System.Threading.Tasks;

namespace Files.Filesystem.StorageItems
{

    sealed class FtpStorageFile : IStorageFile
    {
        private readonly ItemViewModel _viewModel;

        public FtpStorageFile(ItemViewModel viewModel, FtpItem ftpItem)
        {
            _viewModel = viewModel;
            DateCreated = ftpItem.ItemDateCreatedReal;
            Name = ftpItem.ItemName;
            Path = ftpItem.ItemPath;
            FtpPath = FtpHelpers.GetFtpPath(ftpItem.ItemPath);
            FileType = ftpItem.ItemType;
        }

        public FtpStorageFile(ItemViewModel viewModel, IStorageItemWithPath item)
        {
            _viewModel = viewModel;
            Name = System.IO.Path.GetFileName(item.Path);
            Path = item.Path;
            FtpPath = FtpHelpers.GetFtpPath(item.Path);
            FileType = "FTP File";
        }

        private async void FtpDataStreamingHandler(StreamedFileDataRequest request)
        {
            try
            {
                var ftpClient = _viewModel.GetFtpInstance();

                if (!await ftpClient.EnsureConnectedAsync())
                {
                    request.FailAndClose(StreamedFileFailureMode.CurrentlyUnavailable);
                    return;
                }

                using var stream = request.AsStreamForWrite();
                await ftpClient.DownloadAsync(stream, FtpPath);
                await request.FlushAsync();
                request.Dispose();
            }
            catch
            {
                request.FailAndClose(StreamedFileFailureMode.Incomplete);
            }
        }

        public IAsyncOperation<StorageFile> ToStorageFileAsync()
        {
            return StorageFile.CreateStreamedFileAsync(Name, FtpDataStreamingHandler, null);
        }

        public IAsyncAction RenameAsync(string desiredName)
        {
            return RenameAsync(desiredName, NameCollisionOption.FailIfExists);
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

        public IAsyncOperation<BasicProperties> GetBasicPropertiesAsync() => throw new NotSupportedException();

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
        
        public IAsyncAction CopyAndReplaceAsync(IStorageFile fileToReplace) => throw new NotSupportedException();
        public IAsyncAction MoveAsync(IStorageFolder destinationFolder) => throw new NotSupportedException();
        public IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName) => throw new NotSupportedException();
        public IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option) => throw new NotSupportedException();
        public IAsyncAction MoveAndReplaceAsync(IStorageFile fileToReplace) => throw new NotSupportedException();

        public string ContentType { get; } = "application/octet-stream";

        public string FileType { get; }

        public IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync() => throw new NotSupportedException();
        public IAsyncOperation<IInputStream> OpenSequentialReadAsync() => throw new NotSupportedException();
    }
}
