// Copyright (c) Files Community
// Licensed under the MIT License.

using OwlCore.Storage.System.IO;

namespace Files.App.Storage.Storables
{
	/// <inheritdoc cref="IStorageService"/>
	[Obsolete("Use the new WindowsStorable")]
	public sealed class NativeStorageLegacyService : IStorageService
	{
		/// <inheritdoc/>
		public async Task<IFile> GetFileAsync(string id, CancellationToken cancellationToken = default)
		{
			await Task.CompletedTask;
			return new SystemFile(id);
		}

		/// <inheritdoc/>
		public async Task<IFolder> GetFolderAsync(string id, CancellationToken cancellationToken = default)
		{
			await Task.CompletedTask;
			return new SystemFolder(id);
		}
	}
}
