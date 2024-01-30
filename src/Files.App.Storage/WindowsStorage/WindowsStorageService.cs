// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Storage;

namespace Files.App.Storage
{
	/// <inheritdoc cref="IStorageService"/>
	internal sealed class WindowsStorageService : IStorageService
	{
		/// <inheritdoc/>
		public async Task<IFile> GetFileAsync(string id, CancellationToken cancellationToken = default)
		{
			var file = await StorageFile.GetFileFromPathAsync(id).AsTask(cancellationToken);
			return new WindowsStorageFile(file);
		}

		/// <inheritdoc/>
		public async Task<IFolder> GetFolderAsync(string id, CancellationToken cancellationToken = default)
		{
			var folder = await StorageFolder.GetFolderFromPathAsync(id).AsTask(cancellationToken);
			return new WindowsStorageFolder(folder);
		}
	}
}
