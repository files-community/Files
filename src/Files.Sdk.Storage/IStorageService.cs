// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Sdk.Storage.LocatableStorage;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Sdk.Storage
{
	/// <summary>
	/// Provides an abstract layer for accessing the file system.
	/// </summary>
	public interface IStorageService
	{
		/// <summary>
		/// Checks if access to the file system is granted. Additionally, tries to obtain the permission to access rights to the file system.
		/// </summary>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. If access is granted returns true, otherwise false.</returns>
		Task<bool> IsAccessibleAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Check if file exists at specified <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The path to the file.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. If file exists, returns true otherwise false.</returns>
		Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default);

		/// <summary>
		/// Check if directory exists at specified <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The path to the directory.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. If the directory exists, returns true otherwise false.</returns>
		Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets the folder at the specified <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The path to the folder.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. Value is <see cref="ILocatableFolder"/>, otherwise an exception is thrown.</returns>
		Task<ILocatableFolder> GetFolderFromPathAsync(string path, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets the file at the specified <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The path to the file.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. Value is <see cref="ILocatableFile"/>, otherwise an exception is thrown.</returns>
		Task<ILocatableFile> GetFileFromPathAsync(string path, CancellationToken cancellationToken = default);
	}
}
