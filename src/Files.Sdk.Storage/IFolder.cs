// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Sdk.Storage.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Sdk.Storage
{
	/// <summary>
	/// Represents a folder on the file system.
	/// </summary>
	public interface IFolder : IStorable
	{
		/// <summary>
		/// Gets a file in the current directory by name.
		/// </summary>
		/// <param name="fileName">The name of the file.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. If file is found and access is granted, returns <see cref="IFile"/> otherwise null.</returns>
		Task<IFile> GetFileAsync(string fileName, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets a folder in the current directory by name.
		/// </summary>
		/// <param name="folderName">The name of the folder.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. If folder is found and access is granted, returns <see cref="IFolder"/> otherwise null.</returns>
		Task<IFolder> GetFolderAsync(string folderName, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets all items of this directory.
		/// </summary>
		/// <param name="kind">The type of items to enumerate.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>Returns an async operation represented by <see cref="IAsyncEnumerable{T}"/> of type <see cref="IStorable"/> of items in the directory.</returns>
		IAsyncEnumerable<IStorable> GetItemsAsync(StorableKind kind = StorableKind.All, CancellationToken cancellationToken = default);
	}
}
