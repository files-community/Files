// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;
using System.IO;
using Windows.Storage;

namespace Files.App.Storage.NativeStorage
{
	/// <inheritdoc cref="IStorageService"/>
	public sealed class NativeStorageService : IStorageService
	{
		/// <inheritdoc/>
		public Task<IFile> GetFileAsync(string id, CancellationToken cancellationToken = default)
		{
			if (!File.Exists(id))
				throw new FileNotFoundException();

			return Task.FromResult<IFile>(new NativeFile(id));
		}

		/// <inheritdoc/>
		public async Task<IFolder> GetFolderAsync(string id, CancellationToken cancellationToken = default)
		{
			if (!Directory.Exists(id))
				throw new DirectoryNotFoundException();

			// A special folder should use the localized name
			if (PathHelpers.IsSpecialFolder(id))
			{
				var storageFolder = await TryGetStorageFolderAsync(id);
				return new NativeFolder(id, storageFolder?.DisplayName);
			}

			return new NativeFolder(id);

			async Task<StorageFolder?> TryGetStorageFolderAsync(string path)
			{
				try
				{
					return await StorageFolder.GetFolderFromPathAsync(path);
				}
				catch (Exception)
				{
					return null;
				}
			}
		}
	}
}
