using Files.Backend.AppModels;
using Files.Sdk.Storage.LocatableStorage;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Backend.Services
{
    /// <summary>
    /// Represents a service used to manage file tags.
    /// </summary>
    public interface IFileTagsService
    {
        /// <summary>
        /// Checks if file tags are supported by the platform.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation. Value is true if file tags are supported, otherwise false.</returns>
        Task<bool> IsSupportedAsync();

        /// <summary>
        /// Sets or updates tag values for a given <paramref name="storable"/>.
        /// </summary>
        /// <param name="storable">The storable object to update tags for.</param>
        /// <param name="tags">The tags to set.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation. If successful, returns true, otherwise false.</returns>
        Task<bool> SetFileTagAsync(ILocatableStorable storable, string[] tags, CancellationToken cancellationToken);

        /// <summary>
        /// Gets all tags which are used to tag files and folders from the database.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
        /// <returns>Returns an async operation represented by <see cref="IAsyncEnumerable{T}"/> of type <see cref="string"/> that represents all tags.</returns>
        IAsyncEnumerable<string> GetTagsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets all file tag for that are tagged with a specific <paramref name="tag"/>.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
        /// <returns>Returns an async operation represented by <see cref="IAsyncEnumerable{T}"/> of type <see cref="TagModel"/> of tags with a given tag.</returns>
        IAsyncEnumerable<TagModel> GetFileTagsForTagAsync(string tag, CancellationToken cancellationToken);

        /// <summary>
        /// Gets all file/folder tags from the database.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
        /// <returns>Returns an async operation represented by <see cref="IAsyncEnumerable{T}"/> of type <see cref="TagModel"/> of tags saved in the database.</returns>
        IAsyncEnumerable<TagModel> GetAllFileTagsAsync(CancellationToken cancellationToken);
    }
}
