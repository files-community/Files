using Files.Filesystem.FilesystemHistory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Files.Filesystem
{
    public interface IFilesystemHelpers : IDisposable
    {
        /// <summary>
        /// Creates an item from <paramref name="fullPath"/> determined by <paramref name="itemType"/>
        /// </summary>
        /// <param name="fullPath">The fullPath to the item</param>
        /// <param name="itemType">The type of item to create</param>
        /// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
        /// <returns><see cref="ReturnResult"/> of performed operation</returns>
        Task<ReturnResult> CreateAsync(string fullPath, FilesystemItemType itemType, bool registerHistory);

        #region Delete

        /// <summary>
        /// Deletes provided <paramref name="source"/>
        /// </summary>
        /// <param name="source">The <paramref name="source"/> to delete</param>
        /// <param name="showDialog">Determines whether to show delete confirmation dialog</param>
        /// <param name="permanently">Determines whether <paramref name="source"/> is be deleted permanently</param>
        /// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
        /// <returns><see cref="ReturnResult"/> of performed operation</returns>
        Task<ReturnResult> DeleteItemsAsync(IEnumerable<IStorageItem> source, bool showDialog, bool permanently, bool registerHistory);

        /// <summary>
        /// Deletes provided <paramref name="source"/>
        /// </summary>
        /// <param name="source">The <paramref name="source"/> to delete</param>
        /// <param name="showDialog">Determines whether to show delete confirmation dialog</param>
        /// <param name="permanently">Determines whether <paramref name="source"/> is be deleted permanently</param>
        /// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
        /// <returns><see cref="ReturnResult"/> of performed operation</returns>
        Task<ReturnResult> DeleteItemAsync(IStorageItem source, bool showDialog, bool permanently, bool registerHistory);

        #endregion

        /// <summary>
        /// Restores <paramref name="source"/> from the RecycleBin to <paramref name="destination"/> fullPath
        /// </summary>
        /// <param name="source">The source Recycle Bin item followed by <see cref="FilesystemItemType"/> (separated by | symbol):
        /// <br/>
        /// <br/>
        /// &lt;RecycleBinPath&gt;|&lt;<see cref="FilesystemItemType"/>&gt;</param>
        /// <param name="destination">The destination fullPath to restore to</param>
        /// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
        /// <returns><see cref="ReturnResult"/> of performed operation</returns>
        Task<ReturnResult> RestoreFromTrashAsync(string source, string destination, bool registerHistory);

        /// <summary>
        /// Performs relevant operation based on <paramref name="operation"/>
        /// </summary>
        /// <param name="operation">The operation</param>
        /// <param name="packageView">The package view data</param>
        /// <param name="destination">Destination directory to perform the operation
        /// <br/>
        /// <br/>
        /// Note:
        /// <br/>
        /// The <paramref name="destination"/> is NOT fullPath</param>
        /// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
        /// <returns><see cref="ReturnResult"/> of performed operation</returns>
        Task<ReturnResult> PerformOperationTypeAsync(DataPackageOperation operation, DataPackageView packageView, string destination, bool registerHistory);

        #region Copy

        /// <summary>
        /// Copies <paramref name="source"/> to <paramref name="destination"/> fullPath
        /// </summary>
        /// <param name="source">The source items to be copied</param>
        /// <param name="destination">The destination fullPath</param>
        /// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
        /// <returns><see cref="ReturnResult"/> of performed operation</returns>
        Task<ReturnResult> CopyItemsAsync(IEnumerable<IStorageItem> source, IEnumerable<string> destination, bool registerHistory);

        /// <summary>
        /// Copies <paramref name="source"/> to <paramref name="destination"/> fullPath
        /// </summary>
        /// <param name="source">The source item to be copied</param>
        /// <param name="destination">The destination fullPath</param>
        /// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
        /// <returns><see cref="ReturnResult"/> of performed operation</returns>
        Task<ReturnResult> CopyItemAsync(IStorageItem source, string destination, bool registerHistory);

        /// <summary>
        /// Copies items from clipboard to <paramref name="destination"/> fullPath
        /// </summary>
        /// <param name="packageView">Clipboard data</param>
        /// <param name="destination">Destination directory to perform the operation
        /// <br/>
        /// <br/>
        /// Note:
        /// <br/>
        /// The <paramref name="destination"/> is NOT fullPath</param>
        /// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
        /// <returns><see cref="ReturnResult"/> of performed operation</returns>
        Task<ReturnResult> CopyItemsFromClipboard(DataPackageView packageView, string destination, bool registerHistory);

        #endregion

        #region Move

        /// <summary>
        /// Moves <paramref name="source"/> to <paramref name="destination"/> fullPath
        /// </summary>
        /// <param name="source">The source items to be moved</param>
        /// <param name="destination">The destination fullPath</param>
        /// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
        /// <returns><see cref="ReturnResult"/> of performed operation</returns>
        Task<ReturnResult> MoveItemsAsync(IEnumerable<IStorageItem> source, IEnumerable<string> destination, bool registerHistory);

        /// <summary>
        /// Moves <paramref name="source"/> to <paramref name="destination"/> fullPath
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination">The destination fullPath</param>
        /// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
        /// <returns><see cref="ReturnResult"/> of performed operation</returns>
        Task<ReturnResult> MoveItemAsync(IStorageItem source, string destination, bool registerHistory);

        /// <summary>
        /// Moves items from clipboard to <paramref name="destination"/> fullPath
        /// </summary>
        /// <param name="packageView">Clipboard data</param>
        /// <param name="destination">Destination directory to perform the operation
        /// <br/>
        /// <br/>
        /// Note:
        /// <br/>
        /// The <paramref name="destination"/> is NOT fullPath</param>
        /// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
        /// <returns><see cref="ReturnResult"/> of performed operation</returns>
        Task<ReturnResult> MoveItemsFromClipboard(DataPackageView packageView, string destination, bool registerHistory);

        #endregion

        /// <summary>
        /// Renames <paramref name="source"/> with <paramref name="newName"/>
        /// </summary>
        /// <param name="source">The item to rename</param>
        /// <param name="newName">Desired new name</param>
        /// <param name="collision">Determines what to do if item already exists</param>
        /// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
        /// <returns><see cref="ReturnResult"/> of performed operation</returns>
        Task<ReturnResult> RenameAsync(IStorageItem source, string newName, NameCollisionOption collision, bool registerHistory);
    }
}