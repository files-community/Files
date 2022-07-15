using System.Threading;
using System.Threading.Tasks;
using Files.Sdk.Storage;
using Files.Sdk.Storage.Enums;
using Files.Sdk.Storage.StorageProperties;
using FluentFTP;

#nullable enable

namespace Files.Uwp.Storage.FtpStorage
{
    public abstract class FtpBaseStorage : IBaseStorage
    {
        public string Path { get; protected internal set; }

        public string Name { get; protected internal set; }

        public IStoragePropertiesCollection? Properties { get; protected init; }

        protected internal FtpBaseStorage(string path, string name)
        {
            Path = FtpHelpers.GetFtpPath(path);
            Name = name;
        }

        public virtual Task<IFolder?> GetParentAsync()
        {
            return Task.FromResult<IFolder?>(null);
        }

        public abstract Task<bool> RenameAsync(string newName, NameCollisionOption options);

        public abstract Task<bool> DeleteAsync(bool permanently, CancellationToken cancellationToken = default);

        protected internal FtpClient GetFtpClient()
        {
            return FtpHelpers.GetFtpClient(Path);
        }
    }
}
