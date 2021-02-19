using Files.Enums;
using Files.Filesystem.FilesystemHistory;
using Files.Filesystem.StorageFileHelpers;
using FluentFTP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Filesystem.FilesystemOperations
{
    public class FtpFilesystemOperaions : IFilesystemOperations
    {
        public IFtpClient FtpClient { get; set; }

        private async Task<bool> EnsureConnected()
        {
            if (FtpClient is null)
            {
                return false;
            }

            await FtpClient.AutoConnectAsync();
            return true;
        }

        public async Task<IStorageHistory> CopyAsync(IStorageItem source, string destination, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken)
        {
            if (!(source is FtpStorageItem item) || !await EnsureConnected())
            {
                return null;
            }

            throw new NotImplementedException();
        }
        public async Task<IStorageHistory> CopyAsync(IStorageItemWithPath source, string destination, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken) => throw new NotImplementedException();
        public async Task<IStorageHistory> CreateAsync(IStorageItemWithPath source, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken) => throw new NotImplementedException();
        public async Task<IStorageHistory> DeleteAsync(IStorageItem source, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, bool permanently, CancellationToken cancellationToken) => throw new NotImplementedException();
        public async Task<IStorageHistory> DeleteAsync(IStorageItemWithPath source, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, bool permanently, CancellationToken cancellationToken) => throw new NotImplementedException();

        public void Dispose()
        {

        }

        public async Task<IStorageHistory> MoveAsync(IStorageItem source, string destination, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken) => throw new NotImplementedException();
        public async Task<IStorageHistory> MoveAsync(IStorageItemWithPath source, string destination, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken) => throw new NotImplementedException();
        public async Task<IStorageHistory> RenameAsync(IStorageItem source, string newName, NameCollisionOption collision, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken) => throw new NotImplementedException();
        public async Task<IStorageHistory> RenameAsync(IStorageItemWithPath source, string newName, NameCollisionOption collision, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken) => throw new NotImplementedException();
        public async Task<IStorageHistory> RestoreFromTrashAsync(IStorageItemWithPath source, string destination, IProgress<float> progress, IProgress<FileSystemStatusCode> errorCode, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
