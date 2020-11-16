using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Files.Filesystem.FilesystemHistory;

namespace Files.Filesystem
{
    /// <summary>
    /// This interface provides Files filesystem operations
    /// <br/>
    /// <br/>
    /// Each operation returns <see cref="Task{IStorageHistory}"/> and the <see cref="IStorageHistory"/> is NOT saved automatically
    /// </summary>
    public interface IFilesystemOperations : IDisposable
    // TODO Maybe replace IProgress<float> with custom IProgress<FilesystemProgress> class?
    // It would gave us the ability to extend the reported progress by e.g.: transfer speed
    {
        /// <summary>
        /// Creates an item from <paramref name="fullPath"/> determined by <paramref name="itemType"/>
        /// </summary>
        /// <param name="fullPath">The fullPath to the item</param>
        /// <param name="itemType">The type of item to create</param>
        /// <param name="errorCode">Status of the operation</param>
        /// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
        /// <returns><see cref="IStorageHistory"/> where:
        /// <br/>
        /// Source: The created item full path (<see cref="string"/>)
        /// <br/>
        /// Destination: The <see cref="FilesystemItemType"/> (as <see cref="string"/>) of created item
        /// </returns>
        Task<IStorageHistory> CreateAsync(string fullPath, FilesystemItemType itemType, IProgress<FilesystemErrorCode> errorCode, CancellationToken cancellationToken);

        /// <summary>
        /// Copies <paramref name="source"/> to <paramref name="destination"/> fullPath
        /// </summary>
        /// <param name="source">The source item to be copied</param>
        /// <param name="destination">The destination fullPath</param>
        /// <param name="progress">Progress of the operation</param>
        /// <param name="errorCode">Status of the operation</param>
        /// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
        /// <returns><see cref="IStorageHistory"/> where:
        /// <br/>
        /// Source: The <paramref name="source"/> item full path (<see cref="string"/>)
        /// <br/>
        /// Destination: The <paramref name="destination"/> item full path (<see cref="string"/>) the <paramref name="source"/> was copied
        /// </returns>
        Task<IStorageHistory> CopyAsync(IStorageItem source, string destination, IProgress<float> progress, IProgress<FilesystemErrorCode> errorCode, CancellationToken cancellationToken);

        /// <summary>
        /// Moves <paramref name="source"/> to <paramref name="destination"/> fullPath
        /// </summary>
        /// <param name="source">The source item to be moved</param>
        /// <param name="destination">The destination fullPath</param>
        /// <param name="progress">Progress of the operation</param>
        /// <param name="errorCode">Status of the operation</param>
        /// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
        /// <returns><see cref="IStorageHistory"/> where:
        /// <br/>
        /// Source: The source item full path (<see cref="string"/>)
        /// <br/>
        /// Destination: The <paramref name="destination"/> item full path (<see cref="string"/>) the <paramref name="source"/> was moved
        /// </returns>
        Task<IStorageHistory> MoveAsync(IStorageItem source, string destination, IProgress<float> progress, IProgress<FilesystemErrorCode> errorCode, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source to delete</param>
        /// <param name="progress">Progress of the operation</param>
        /// <param name="errorCode">Status of the operation</param>
        /// <param name="permanently">Determines whether <paramref name="source"/> is be deleted permanently</param>
        /// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
        /// <returns><see cref="IStorageHistory"/> where:
        /// <br/>
        /// Source: The deleted item full path (<see cref="string"/>)
        /// <br/>
        /// Destination:
        /// <br/>
        /// Returns null if <paramref name="permanently"/> was true
        /// <br/>
        /// If <paramref name="permanently"/> was false, returns path to recycled item followed by <see cref="FilesystemItemType"/> (separated by | symbol):
        /// <br/>
        /// <br/>
        /// <code>&lt;RecycleBinPath&gt;|&lt;<see cref="FilesystemItemType"/>&gt;</code>
        /// </returns>
        Task<IStorageHistory> DeleteAsync(IStorageItem source, IProgress<float> progress, IProgress<FilesystemErrorCode> errorCode, bool permanently, CancellationToken cancellationToken);

        /// <summary>
        /// Renames <paramref name="source"/> with <paramref name="newName"/>
        /// </summary>
        /// <param name="source">The item to rename</param>
        /// <param name="newName">Desired new name</param>
        /// <param name="collision">Determines what to do if item already exists</param>
        /// <param name="errorCode">Status of the operation</param>
        /// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
        /// <returns><see cref="IStorageHistory"/> where:
        /// <br/>
        /// Source: The original item full path (<see cref="string"/>)
        /// <br/>
        /// Destination: The renamed item full path (<see cref="string"/>)
        /// </returns>
        Task<IStorageHistory> RenameAsync(IStorageItem source, string newName, NameCollisionOption collision, IProgress<FilesystemErrorCode> errorCode, CancellationToken cancellationToken);

        /// <summary>
        /// Restores <paramref name="source"/> from the RecycleBin to <paramref name="destination"/> fullPath
        /// </summary>
        /// <param name="source">The source Recycle Bin item followed by <see cref="FilesystemItemType"/> (separated by | symbol):
        /// <br/>
        /// <br/>
        /// &lt;RecycleBinPath&gt;|&lt;<see cref="FilesystemItemType"/>&gt;</param>
        /// <param name="destination">The destination fullPath to restore to</param>
        /// <param name="progress">Progress of the operation</param>
        /// <param name="errorCode">Status of the operation</param>
        /// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
        /// <returns><see cref="IStorageHistory"/> where:
        /// <br/>
        /// Source: The trash item full path
        /// <br/>
        /// Destination: The <paramref name="destination"/> item full path (<see cref="string"/>) the <paramref name="source"/> has been restored
        /// </returns>
        Task<IStorageHistory> RestoreFromTrashAsync(string source, string destination, IProgress<float> progress, IProgress<FilesystemErrorCode> errorCode, CancellationToken cancellationToken);
    }
}
