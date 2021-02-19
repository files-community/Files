using FluentFTP;
using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.System.Threading;

namespace Files.Filesystem.StorageFileHelpers
{
    public class FtpStorageItem : IStorageItem
    {
        public FtpStorageItem(IFtpClient ftpClient, string name, string path, FileAttributes attributes, DateTimeOffset dateCreated)
        {
            FtpClient = ftpClient;
            Name = name;
            Path = path;
            Attributes = attributes;
            DateCreated = dateCreated;
        }

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

        public IAsyncAction RenameAsync(string desiredName)
        {
            return ThreadPool.RunAsync(async (x) =>
            {
                if (!await EnsureConnected())
                {
                    return;
                }

                await FtpClient.RenameAsync(Path, string.Join("/", Path.GetFtpDirectoryName(), desiredName));
            });
        }

        public IAsyncAction RenameAsync(string desiredName, NameCollisionOption option) => throw new NotImplementedException();
        public IAsyncAction DeleteAsync()
        {
            return ThreadPool.RunAsync(async (x) =>
            {
                if (!await EnsureConnected())
                {
                    return;
                }
                if ((Attributes & FileAttributes.Directory) != 0)
                {
                    await FtpClient.DeleteDirectoryAsync(Path);
                }
                else
                {
                    await FtpClient.DeleteFileAsync(Path);
                }
            });
        }

        public IAsyncAction DeleteAsync(StorageDeleteOption option) => throw new NotImplementedException();
        public IAsyncOperation<BasicProperties> GetBasicPropertiesAsync() => throw new NotImplementedException();
        public bool IsOfType(StorageItemTypes type)
        {
            switch (type)
            {
                case StorageItemTypes.File:
                    return (Attributes & FileAttributes.Directory) == 0;
                case StorageItemTypes.Folder:
                    return (Attributes & FileAttributes.Directory) != 0;
                default:
                    return false;
            }
        }

        public FileAttributes Attributes { get; private set; }

        public DateTimeOffset DateCreated { get; private set; }

        public string Name { get; private set; }

        public string Path { get; private set; }
    }
}
