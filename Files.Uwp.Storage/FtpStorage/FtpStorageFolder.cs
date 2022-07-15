using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Files.Sdk.Storage;
using Files.Sdk.Storage.Enums;
using Files.Shared.Helpers;
using FluentFTP;

#nullable enable

namespace Files.Uwp.Storage.FtpStorage
{
    public sealed class FtpStorageFolder : FtpBaseStorage, IFolder
    {
        public FtpStorageFolder(string path, string name)
            : base(path, name)
        {
        }

        public override async Task<bool> RenameAsync(string newName, NameCollisionOption options)
        {
            using var ftpClient = GetFtpClient();
            if (!await ftpClient.EnsureConnectedAsync())
                return false;

            var destination = $"{PathHelpers.GetParentDir(Path)}/{newName}";
            var remoteExists = options == NameCollisionOption.ReplaceExisting ? FtpRemoteExists.Overwrite : FtpRemoteExists.Skip;
            var isSuccessful = await ftpClient.MoveDirectoryAsync(Path, destination, remoteExists);

            if (!isSuccessful && options == NameCollisionOption.GenerateUniqueName)
            {
                // TODO: handle name generation
            }

            return isSuccessful;
        }

        public override async Task<bool> DeleteAsync(bool permanently, CancellationToken cancellationToken = default)
        {
            using var ftpClient = GetFtpClient();
            if (!await ftpClient.EnsureConnectedAsync(cancellationToken))
                return false;

            try
            {
                await ftpClient.DeleteDirectoryAsync(Path, cancellationToken);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Task<IFile?> CreateFileAsync(string desiredName)
        {
            return CreateFileAsync(desiredName, CreationCollisionOption.FailIfExists);
        }

        public async Task<IFile?> CreateFileAsync(string desiredName, CreationCollisionOption options)
        {
            using var ftpClient = GetFtpClient();
            if (!await ftpClient.EnsureConnectedAsync())
                return null;

            using var stream = new MemoryStream();

            var remotePath = $"{Path}/{desiredName}";
            var ftpRemoteExists = options == CreationCollisionOption.ReplaceExisting ? FtpRemoteExists.Overwrite : FtpRemoteExists.Skip;
            var result = await ftpClient.UploadAsync(stream, remotePath, ftpRemoteExists);

            if (result == FtpStatus.Success)
            {
                return new FtpStorageFile($"{Path}/{desiredName}", desiredName);
            }
            else if (result == FtpStatus.Skipped)
            {
                // We don't want to throw an exception when the file already exists, just return null
                _ = options == CreationCollisionOption.FailIfExists;
                return null;
            }

            // File creation failed
            return null;
        }

        public Task<IFolder?> CreateFolderAsync(string desiredName)
        {
            return CreateFolderAsync(desiredName, CreationCollisionOption.FailIfExists);
        }

        public async Task<IFolder?> CreateFolderAsync(string desiredName, CreationCollisionOption options)
        {
            using var ftpClient = GetFtpClient();
            if (!await ftpClient.EnsureConnectedAsync())
                return null;

            var newPath = $"{Path}/{desiredName}";
            if (await ftpClient.DirectoryExistsAsync(newPath))
                return new FtpStorageFolder(newPath, desiredName);

            var replaceExisting = options == CreationCollisionOption.ReplaceExisting;
            var isSuccessful = await ftpClient.CreateDirectoryAsync(newPath, replaceExisting);
            if (!isSuccessful)
                return null;

            return new FtpStorageFolder(newPath, desiredName);
        }

        public async Task<IFile?> GetFileAsync(string fileName)
        {
            using var ftpClient = GetFtpClient();
            if (!await ftpClient.EnsureConnectedAsync())
                return null;

            var path = FtpHelpers.GetFtpPath(PathHelpers.Combine(Path, fileName));
            var item = await ftpClient.GetObjectInfoAsync(path);
            if (item is null)
                return null;

            if (item.Type != FtpFileSystemObjectType.File)
                return null;

            return new FtpStorageFile(path, item.Name);
        }

        public async Task<IFolder?> GetFolderAsync(string folderName)
        {
            using var ftpClient = GetFtpClient();
            if (!await ftpClient.EnsureConnectedAsync())
                return null;

            var path = FtpHelpers.GetFtpPath(PathHelpers.Combine(Path, folderName));
            var item = await ftpClient.GetObjectInfoAsync(path);
            if (item is null)
                return null;

            if (item.Type != FtpFileSystemObjectType.Directory)
                return null;

            return new FtpStorageFolder(path, item.Name);
        }

        public async IAsyncEnumerable<IFile> GetFilesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var ftpClient = GetFtpClient();
            if (!await ftpClient.EnsureConnectedAsync(cancellationToken))
                yield break;

            foreach (var item in await ftpClient.GetListingAsync(Path, cancellationToken))
            {
                if (item.Type == FtpFileSystemObjectType.File)
                    yield return new FtpStorageFile(item.FullName, item.Name);
            }
        }

        public async IAsyncEnumerable<IFolder> GetFoldersAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var ftpClient = GetFtpClient();
            if (!await ftpClient.EnsureConnectedAsync(cancellationToken))
                yield break;

            foreach (var item in await ftpClient.GetListingAsync(Path, cancellationToken))
            {
                if (item.Type == FtpFileSystemObjectType.Directory)
                    yield return new FtpStorageFolder(item.FullName, item.Name);
            }
        }

        public async IAsyncEnumerable<IBaseStorage> GetStorageAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var ftpClient = GetFtpClient();
            if (!await ftpClient.EnsureConnectedAsync(cancellationToken))
                yield break;

            foreach (var item in await ftpClient.GetListingAsync(Path, cancellationToken))
            {
                if (item.Type == FtpFileSystemObjectType.File)
                    yield return new FtpStorageFile(item.FullName, item.Name);

                if (item.Type == FtpFileSystemObjectType.Directory)
                    yield return new FtpStorageFolder(item.FullName, item.Name);
            }
        }
    }
}
