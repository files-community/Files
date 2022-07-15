using System;
using System.Threading.Tasks;
using Files.Sdk.Storage;
using Files.Sdk.Storage.Services;
using FluentFTP;

#nullable enable

namespace Files.Uwp.Storage.FtpStorage
{
    public sealed class FtpFileSystemService : IFileSystemService
    {
        public Task<bool> IsFileSystemAccessible()
        {
            return Task.FromResult(true); // TODO: Check if FTP is available
        }

        public async Task<bool> FileExistsAsync(string path)
        {
            return await GetFileFromPathAsync(path) is not null;
        }

        public async Task<bool> DirectoryExistsAsync(string path)
        {
            return await GetFolderFromPathAsync(path) is not null;
        }

        public async Task<IFolder?> GetFolderFromPathAsync(string path)
        {
            using var ftpClient = FtpHelpers.GetFtpClient(path);
            if (!await ftpClient.EnsureConnectedAsync())
                return null;

            var ftpPath = FtpHelpers.GetFtpPath(path);
            var item = await ftpClient.GetObjectInfoAsync(ftpPath);
            if (item is null)
                return null;

            if (item.Type != FtpFileSystemObjectType.Directory)
                return null;

            return new FtpStorageFolder(ftpPath, item.Name);
        }

        public async Task<IFile?> GetFileFromPathAsync(string path)
        {
            using var ftpClient = FtpHelpers.GetFtpClient(path);
            if (!await ftpClient.EnsureConnectedAsync())
                return null;

            var ftpPath = FtpHelpers.GetFtpPath(path);
            var item = await ftpClient.GetObjectInfoAsync(ftpPath);
            if (item is null)
                return null;

            if (item.Type != FtpFileSystemObjectType.File)
                return null;

            return new FtpStorageFile(ftpPath, item.Name);
        }

        public Task<IDisposable?> ObtainLockAsync(IBaseStorage storage)
        {
            return Task.FromResult<IDisposable?>(null);
        }
    }
}
