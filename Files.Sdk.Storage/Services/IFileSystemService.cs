using System;
using System.Threading.Tasks;

namespace Files.Sdk.Storage.Services
{
public interface IFileSystemService
    {
        Task<bool> IsFileSystemAccessible();

        Task<bool> FileExistsAsync(string path);

        Task<bool> DirectoryExistsAsync(string path);

        Task<IFolder?> GetFolderFromPathAsync(string path);

        Task<IFile?> GetFileFromPathAsync(string path);

        Task<IDisposable?> ObtainLockAsync(IBaseStorage storage);
    }
}
