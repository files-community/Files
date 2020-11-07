using System;
using Files.Filesystem.FilesystemHistory;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Filesystem
{
    /// <summary>
    /// This interface provides Files filesystem operations
    /// <br/>
    /// <br/>
    /// Each operation returns <see cref="Task{IStorageHistory}"/> and the <see cref="IStorageHistory"/> is not saved automatically
    /// </summary>
    public interface IFilesystemOperations
    // TODO Maybe replace IProgress<float> with custom IProgress<FilesystemProgress> class?
    // It would gave us the ability to extend the reported progress by e.g.: transfer speed
    {
        /// <summary>
        /// Creates an item from <paramref name="fullPath"/> determined by <paramref name="itemType"/>
        /// </summary>
        /// <param name="fullPath">The path to the item</param>
        /// <param name="itemType">The itemtype to create</param>
        /// <param name="status">Status of the operation</param>
        /// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
        /// <returns><see cref="IStorageHistory"/>
        /// <br/>
        /// Source: The created item path
        /// <br/>
        /// Destination: null
        /// </returns>
        Task<IStorageHistory> CreateAsync(string fullPath, FilesystemItemType itemType, IProgress<Status> status, CancellationToken cancellationToken);

        /// <summary>
        /// Copies <paramref name="source"/> item to <paramref name="destination"/> directory
        /// </summary>
        /// <param name="source">The source item to be copied</param>
        /// <param name="destination">The destination directory to copy to</param>
        /// <param name="progress">Progress of the operation</param>
        /// <param name="status">Status of the operation</param>
        /// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
        /// <returns><see cref="IStorageHistory"/>
        /// <br/>
        /// Source: Source item
        /// <br/>
        /// Destination: The destination directory <paramref name="source"/> has been copied to
        /// </returns>
        Task<IStorageHistory> CopyAsync(IStorageItem source, IStorageItem destination, IProgress<float> progress, IProgress<Status> status, CancellationToken cancellationToken);

        /// <summary>
        /// Moves <paramref name="source"/> item to <paramref name="destination"/> directory
        /// </summary>
        /// <param name="source">The source item to be moved</param>
        /// <param name="destination">The destination directory to move to</param>
        /// <param name="progress">Progress of the operation</param>
        /// <param name="status">Status of the operation</param>
        /// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
        /// <returns><see cref="IStorageHistory"/>
        /// <br/>
        /// Source: Source item
        /// <br/>
        /// Destination: The destination directory <paramref name="source"/> has been moved to
        /// </returns>
        Task<IStorageHistory> MoveAsync(IStorageItem source, IStorageItem destination, IProgress<float> progress, IProgress<Status> status, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source to delete</param>
        /// <param name="progress">Progress of the operation</param>
        /// <param name="status">Status of the operation</param>
        /// <param name="showDialog">Determines whether the delete warning dialog should be shown</param>
        /// <param name="permanently">Determines whether an item is deleted permanently</param>
        /// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
        /// <returns><see cref="IStorageHistory"/>
        /// <br/>
        /// Source: The deleted item
        /// <br/>
        /// Destination: null  // TODO: Return destination here as RecycleBinItem?
        /// </returns>
        Task<IStorageHistory> DeleteAsync(IStorageItem source, IProgress<float> progress, IProgress<Status> status, bool showDialog, bool permanently, CancellationToken cancellationToken);

        /// <summary>
        /// Renames <paramref name="source"/> with <paramref name="newName"/>
        /// </summary>
        /// <param name="source">The item to rename</param>
        /// <param name="newName">Desired new name</param>
        /// <param name="replace">Determines whether the item is replaced</param>
        /// <param name="status">Status of the operation</param>
        /// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
        /// <returns><see cref="IStorageHistory"/>
        /// <br/>
        /// Source: Original name (string)
        /// <br/>
        /// Destination: Destination path
        /// </returns>
        Task<IStorageHistory> RenameAsync(IStorageItem source, string newName, bool replace, IProgress<Status> status, CancellationToken cancellationToken);

        /// <summary>
        /// Restores <paramref name="source"/> from trash to <paramref name="destination"/> directory
        /// </summary>
        /// <param name="source">The trash item</param>
        /// <param name="destination">The destination directory to restore to</param>
        /// <param name="progress">Progress of the operation</param>
        /// <param name="status">Status of the operation</param>
        /// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
        /// <returns><see cref="IStorageHistory"/>
        /// <br/>
        /// Source: The <paramref name="source"/> (virtual) trash item
        /// <br/>
        /// Destination: The <paramref name="destination"/> directory the item has been moved to
        /// </returns>
        Task<IStorageHistory> RestoreFromTrashAsync(RecycleBinItem source, IStorageItem destination, IProgress<float> progress, IProgress<Status> status, CancellationToken cancellationToken);
    }
}
