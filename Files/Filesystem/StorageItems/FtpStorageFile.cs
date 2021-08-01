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
using System.Collections.Generic;

namespace Files.Filesystem.StorageItems
{
    sealed class FtpStorageFolder : IStorageFolder
    {
        private readonly ItemViewModel _viewModel;

        public FtpStorageFolder(ItemViewModel viewModel, FtpItem ftpItem)
        {
            _viewModel = viewModel;
            DateCreated = ftpItem.ItemDateCreatedReal;
            Name = ftpItem.ItemName;
            Path = ftpItem.ItemPath;
            FtpPath = FtpHelpers.GetFtpPath(ftpItem.ItemPath);
        }

        public FtpStorageFolder(ItemViewModel viewModel, IStorageItemWithPath item)
        {
            _viewModel = viewModel;
            Name = System.IO.Path.GetFileName(item.Path);
            Path = item.Path;
            FtpPath = FtpHelpers.GetFtpPath(item.Path);
        }

        public IAsyncOperation<StorageFile> CreateFileAsync(string desiredName) => throw new NotImplementedException();
        public IAsyncOperation<StorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options) => throw new NotImplementedException();
        public IAsyncOperation<StorageFolder> CreateFolderAsync(string desiredName) => throw new NotImplementedException();
        public IAsyncOperation<StorageFolder> CreateFolderAsync(string desiredName, CreationCollisionOption options) => throw new NotImplementedException();
        public IAsyncOperation<StorageFile> GetFileAsync(string name) => throw new NotImplementedException();
        public IAsyncOperation<StorageFolder> GetFolderAsync(string name) => throw new NotImplementedException();
        public IAsyncOperation<IStorageItem> GetItemAsync(string name) => throw new NotImplementedException();
        public IAsyncOperation<IReadOnlyList<StorageFile>> GetFilesAsync() => throw new NotImplementedException();
        public IAsyncOperation<IReadOnlyList<StorageFolder>> GetFoldersAsync() => throw new NotImplementedException();
        public IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync() => throw new NotImplementedException();
        public IAsyncAction RenameAsync(string desiredName) => throw new NotImplementedException();
        public IAsyncAction RenameAsync(string desiredName, NameCollisionOption option) => throw new NotImplementedException();
        public IAsyncAction DeleteAsync() => throw new NotImplementedException();
        public IAsyncAction DeleteAsync(StorageDeleteOption option) => throw new NotImplementedException();
        public IAsyncOperation<BasicProperties> GetBasicPropertiesAsync() => throw new NotSupportedException();
        public bool IsOfType(StorageItemTypes type) => type == StorageItemTypes.Folder;

        public Windows.Storage.FileAttributes Attributes { get; } = Windows.Storage.FileAttributes.Directory;

        public DateTimeOffset DateCreated { get; }

        public string Name { get; }

        public string Path { get; }

        public string FtpPath { get; }
    }

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
