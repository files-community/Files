using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Files.Sdk.Storage;
using Files.Sdk.Storage.Enums;
using Files.Shared.Helpers;
using FluentFTP;

#nullable enable

namespace Files.Uwp.Storage.FtpStorage
{
    public sealed class FtpStorageFile : FtpBaseStorage, IFile
    {
        public string Extension { get; }

        public FtpStorageFile(string path, string name)
            : base(path, name)
        {
            Extension = System.IO.Path.GetExtension(Name);
        }

        public override async Task<bool> RenameAsync(string newName, NameCollisionOption options)
        {
            using var ftpClient = GetFtpClient();
            if (!await ftpClient.EnsureConnectedAsync())
                return false;

            var destination = $"{PathHelpers.GetParentDir(Path)}/{newName}";
            var remoteExists = options == NameCollisionOption.ReplaceExisting ? FtpRemoteExists.Overwrite : FtpRemoteExists.Skip;
            var isSuccessful = await ftpClient.MoveFileAsync(Path, destination, remoteExists);

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
                await ftpClient.DeleteFileAsync(Path, cancellationToken);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Task<Stream?> OpenStreamAsync(FileAccess access)
        {
            return OpenStreamAsync(access, FileShare.None);
        }

        public async Task<Stream?> OpenStreamAsync(FileAccess access, FileShare share)
        {
            using var ftpClient = GetFtpClient();
            if (!await ftpClient.EnsureConnectedAsync())
                return null;

            if (access.HasFlag(FileAccess.Write))
            {
                return await ftpClient.OpenWriteAsync(Path);
            }
            else if (access.HasFlag(FileAccess.Read))
            {
                return await ftpClient.OpenReadAsync(Path);
            }
            else
            {
                return null;
            }
        }

        public Task<Stream?> GetThumbnailStreamAsync(uint requestedSize)
        {
            return Task.FromResult<Stream?>(null);
        }
    }
}
