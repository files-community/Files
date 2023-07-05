// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Sdk.Storage.LocatableStorage;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Sdk.Storage.Extensions
{
	public static partial class StorageExtensions
	{
		/// <summary>
		/// Checks whether the directory exists at a given path and retrieves the folder, otherwise retrieves the file.
		/// </summary>
		/// <param name="storageService">The service.</param>
		/// <param name="path">Path to get the storable at.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. Value is <see cref="ILocatableStorable"/> that represents the item.</returns>
		public static async Task<ILocatableStorable> GetStorableFromPathAsync(this IStorageService storageService, string path, CancellationToken cancellationToken = default)
		{
			if (await storageService.DirectoryExistsAsync(path, cancellationToken))
			{
				return await storageService.GetFolderFromPathAsync(path, cancellationToken);
			}
			else
			{
				return await storageService.GetFileFromPathAsync(path, cancellationToken);
			}
		}

		/// <summary>
		/// Checks whether the directory exists at a given path and tries to retrieve the folder, otherwise tries to retrieve the file.
		/// </summary>
		/// <param name="storageService">The service.</param>
		/// <param name="path">Path to get the storable at.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. If successful, value is <see cref="ILocatableStorable"/> that represents the item, otherwise null.</returns>
		public static async Task<ILocatableStorable?> TryGetStorableFromPathAsync(this IStorageService storageService, string path, CancellationToken cancellationToken = default)
		{
			return (ILocatableStorable?)await storageService.TryGetFolderFromPathAsync(path, cancellationToken) ?? await storageService.TryGetFileFromPathAsync(path, cancellationToken);
		}

		/// <inheritdoc cref="IStorageService.GetFolderFromPathAsync"/>
		public static async Task<ILocatableFolder?> TryGetFolderFromPathAsync(this IStorageService storageService, string path, CancellationToken cancellationToken = default)
		{
			try
			{
				return await storageService.GetFolderFromPathAsync(path, cancellationToken);
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <inheritdoc cref="IStorageService.GetFileFromPathAsync"/>
		public static async Task<ILocatableFile?> TryGetFileFromPathAsync(this IStorageService storageService, string path, CancellationToken cancellationToken = default)
		{
			try
			{
				return await storageService.GetFileFromPathAsync(path, cancellationToken);
			}
			catch (Exception)
			{
				return null;
			}
		}
	}
}
