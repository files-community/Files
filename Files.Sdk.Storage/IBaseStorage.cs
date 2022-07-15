using System.Threading;
using System.Threading.Tasks;
using Files.Sdk.Storage.Enums;
using Files.Sdk.Storage.StorageProperties;

namespace Files.Sdk.Storage
{
    public interface IBaseStorage
    {
        string Path { get; }

        string Name { get; }

        IStoragePropertiesCollection? Properties { get; }

        Task<IFolder?> GetParentAsync();

        Task<bool> RenameAsync(string newName, NameCollisionOption options);

        Task<bool> DeleteAsync(bool permanently, CancellationToken cancellationToken = default);
    }
}
