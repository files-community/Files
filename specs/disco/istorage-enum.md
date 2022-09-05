## Code examples

```cs
using System;
using System.Collections.Generic;
using System.Threading;

namespace SecureFolderFS.Sdk.Storage.StorageEnumeration
{
    /// <summary>
    /// Enumerates storage objects of a given directory.
    /// <remarks>
    /// This interface can be implemented to provide complex enumeration of directories as well as being a substitute for built-in <see cref="IFolder"/> enumeration.</remarks>
    /// </summary>
    public interface IStorageEnumerator : IDisposable
    {
        /// <summary>
        /// Gets the folder where enumeration takes place.
        /// </summary>
        IFolder SourceFolder { get; }

        /// <summary>
        /// Enumerates the <see cref="SourceFolder"/> for files.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
        /// <returns>Returns an async operation represented by <see cref="IAsyncEnumerable{T}"/> of type <see cref="IFile"/> of all files discovered by the enumerator.</returns>
        IAsyncEnumerable<EnumerationResult<IFile>> EnumerateFilesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Enumerates the <see cref="SourceFolder"/> for folders.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
        /// <returns>Returns an async operation represented by <see cref="IAsyncEnumerable{T}"/> of type <see cref="IFolder"/> of all folders discovered by the enumerator.</returns>
        IAsyncEnumerable<EnumerationResult<IFolder>> EnumerateFoldersAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Enumerates the <see cref="SourceFolder"/> for items.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
        /// <returns>Returns an async operation represented by <see cref="IAsyncEnumerable{T}"/> of type <see cref="IStorable"/> of all items discovered by the enumerator.</returns>
        IAsyncEnumerable<EnumerationResult<IStorable>> EnumerateStorageAsync(CancellationToken cancellationToken = default);
    }
}

class EnumerationResult<T> where T : IStorable
{
  T Storable { get; }
  IStoragePropertiesCollection? Properties { get; }
}
```