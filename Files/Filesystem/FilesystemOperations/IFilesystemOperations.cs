using System;
using Files.Filesystem.FilesystemHistory;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Filesystem.FilesystemOperations
{
    public interface IFilesystemOperations
    // TODO Maybe replace IProgress<float> with custom IProgress<FilesystemProgress> class?
    // It would gave us the ability to extend the reported progress by e.g.: transfer speed
    {
        Task<IStorageHistory> CreateAsync(string fullPath, FilesystemItemType itemType, IProgress<Status> status, CancellationToken cancellationToken);

        Task<IStorageHistory> CopyAsync(IStorageItem source, IStorageItem destination, IProgress<float> progress, IProgress<Status> status, CancellationToken cancellationToken);

        Task<IStorageHistory> MoveAsync(IStorageItem source, IStorageItem destination, IProgress<float> progress, IProgress<Status> status, CancellationToken cancellationToken);

        Task<IStorageHistory> DeleteAsync(IStorageItem source, IProgress<float> progress, IProgress<Status> status, bool showDialog, bool pernamently, CancellationToken cancellationToken);

        Task<IStorageHistory> RenameAsync(IStorageItem source, string newName, bool replace, IProgress<Status> status, CancellationToken cancellationToken);

        Task<IStorageHistory> RestoreFromTrashAsync(IStorageItem source, IStorageItem destination, IProgress<Status> status, CancellationToken cancellationToken);
    }
}
