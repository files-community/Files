using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Files.Sdk.Storage.LocatableStorage;
using Files.Sdk.Storage.Services;
using FluentFTP;

#nullable enable

namespace Files.Uwp.Storage.FtpStorage
{
    public sealed class FtpFileSystemService : IFileSystemService
    {
        public Task<bool> IsFileSystemAccessibleAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true); // TODO: Check if FTP is available
        }

        public async Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default)
        {
            try
            {
                _ = await GetFileFromPathAsync(path, cancellationToken);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default)
        {
            try
            {
                _ = await GetFolderFromPathAsync(path, cancellationToken);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<ILocatableFolder> GetFolderFromPathAsync(string path, CancellationToken cancellationToken = default)
        {
            using var ftpClient = FtpHelpers.GetFtpClient(path);
            await ftpClient.EnsureConnectedAsync(cancellationToken);

            var ftpPath = FtpHelpers.GetFtpPath(path);
            var item = await ftpClient.GetObjectInfoAsync(ftpPath, token: cancellationToken);
            if (item is null || item.Type != FtpObjectType.Directory)
                throw new DirectoryNotFoundException("Directory was not found from path.");

            return new FtpStorageFolder(ftpPath, item.Name);
        }

        public async Task<ILocatableFile> GetFileFromPathAsync(string path, CancellationToken cancellationToken = default)
        {
            using var ftpClient = FtpHelpers.GetFtpClient(path);
            await ftpClient.EnsureConnectedAsync(cancellationToken);

            var ftpPath = FtpHelpers.GetFtpPath(path);
            var item = await ftpClient.GetObjectInfoAsync(ftpPath, token: cancellationToken);
            if (item is null || item.Type != FtpObjectType.File)
                throw new FileNotFoundException("File was not found from path.");

            return new FtpStorageFile(ftpPath, item.Name);
        }
    }
}
