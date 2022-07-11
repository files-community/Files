using System;
using System.Collections.Generic;
using System.Threading;

namespace Files.Sdk.Storage.StorageEnumeration
{
    public interface IStorageEnumerator : IDisposable
    {
        IFolder SourceFolder { get; }

        IAsyncEnumerable<IFile> EnumerateFilesAsync(CancellationToken cancellationToken = default);

        IAsyncEnumerable<IFolder> EnumerateFoldersAsync(CancellationToken cancellationToken = default);

        IAsyncEnumerable<IBaseStorage> EnumerateStorageAsync(CancellationToken cancellationToken = default);
    }
}
