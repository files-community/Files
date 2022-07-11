using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Sdk.Storage
{
    public interface IFolder : IBaseStorage
    {
        Task<IFile?> CreateFileAsync(string desiredName);
        
        Task<IFile?> CreateFileAsync(string desiredName, CreationCollisionOption options);

        Task<IFolder?> CreateFolderAsync(string desiredName);

        Task<IFolder?> CreateFolderAsync(string desiredName, CreationCollisionOption options);

        Task<IFile?> GetFileAsync(string fileName);

        Task<IFolder?> GetFolderAsync(string folderName);

        IAsyncEnumerable<IFile> GetFilesAsync(CancellationToken cancellationToken = default);

        IAsyncEnumerable<IFolder> GetFoldersAsync(CancellationToken cancellationToken = default);

        IAsyncEnumerable<IBaseStorage> GetStorageAsync(CancellationToken cancellationToken = default);

        //IFilePool? GetFilePool();

        //IFolderPool? GetFolderPool();
    }
}
