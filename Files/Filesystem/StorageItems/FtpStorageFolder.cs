using Files.Helpers;
using Files.ViewModels;
using FluentFTP;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Collections.Generic;
using System.IO;

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

        public IAsyncOperation<StorageFile> CreateFileAsync(string desiredName) => throw new NotSupportedException();

        public IAsyncOperation<StorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options) => throw new NotSupportedException();
        
        public IAsyncOperation<StorageFolder> CreateFolderAsync(string desiredName)
        {
            return CreateFolderAsync(desiredName, CreationCollisionOption.FailIfExists);
        }

        public IAsyncOperation<StorageFolder> CreateFolderAsync(string desiredName, CreationCollisionOption options)
        {
            return AsyncInfo.Run<StorageFolder>(async (cancellationToken) =>
            {
                var ftpClient = _viewModel.GetFtpInstance();
                if (!await ftpClient.EnsureConnectedAsync())
                {
                    return null;
                }

                await ftpClient.CreateDirectoryAsync($"{FtpPath}/{desiredName}", 
                    options == CreationCollisionOption.ReplaceExisting, 
                    cancellationToken);

                return null;
            });
        }

        private StreamedFileDataRequestedHandler FtpDataStreamingHandler(string name)
        {
            return async request =>
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
                    await ftpClient.DownloadAsync(stream, $"{Path}/{name}");
                    await request.FlushAsync();
                    request.Dispose();
                }
                catch
                {
                    request.FailAndClose(StreamedFileFailureMode.Incomplete);
                }
            };
        }

        public IAsyncOperation<StorageFile> GetFileAsync(string name)
        {
            return StorageFile.CreateStreamedFileAsync(name, FtpDataStreamingHandler(name), null);
        }

        public IAsyncOperation<StorageFolder> GetFolderAsync(string name) => throw new NotSupportedException();

        public IAsyncOperation<IStorageItem> GetItemAsync(string name) => throw new NotSupportedException();
        public IAsyncOperation<IReadOnlyList<StorageFile>> GetFilesAsync() => throw new NotSupportedException();
        public IAsyncOperation<IReadOnlyList<StorageFolder>> GetFoldersAsync() => throw new NotSupportedException();
        public IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync() => throw new NotSupportedException();
        
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

                if (!await ftpClient.MoveDirectoryAsync(FtpPath,
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

                await ftpClient.DeleteDirectoryAsync(FtpPath, cancellationToken);
            });
        }

        public IAsyncAction DeleteAsync(StorageDeleteOption option)
        {
            return DeleteAsync();
        }
        
        public IAsyncOperation<BasicProperties> GetBasicPropertiesAsync() => throw new NotSupportedException();
        
        public bool IsOfType(StorageItemTypes type) => type == StorageItemTypes.Folder;

        public Windows.Storage.FileAttributes Attributes { get; } = Windows.Storage.FileAttributes.Directory;

        public DateTimeOffset DateCreated { get; }

        public string Name { get; }

        public string Path { get; }

        public string FtpPath { get; }
    }
}
