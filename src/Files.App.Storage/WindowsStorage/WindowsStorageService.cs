using Files.Sdk.Storage;
using Files.Sdk.Storage.LocatableStorage;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.App.Storage.WindowsStorage
{
	/// <inheritdoc cref="IStorageService"/>
	internal sealed class WindowsStorageService : IStorageService
	{
		/// <inheritdoc/>
		public Task<bool> IsAccessibleAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult(true);
		}

		/// <inheritdoc/>
		public async Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default)
		{
			try
			{
				_ = await GetFileFromPathAsync(path, cancellationToken);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		/// <inheritdoc/>
		public async Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default)
		{
			try
			{
				_ = await GetFolderFromPathAsync(path, cancellationToken);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		/// <inheritdoc/>
		public async Task<ILocatableFolder> GetFolderFromPathAsync(string path, CancellationToken cancellationToken = default)
		{
			var folderTask = StorageFolder.GetFolderFromPathAsync(path).AsTask(cancellationToken);
			var folder = await folderTask;

			return new WindowsStorageFolder(folder);
		}

		/// <inheritdoc/>
		public async Task<ILocatableFile> GetFileFromPathAsync(string path, CancellationToken cancellationToken = default)
		{
			var fileTask = StorageFile.GetFileFromPathAsync(path).AsTask(cancellationToken);
			var file = await fileTask;

			return new WindowsStorageFile(file);
		}
	}
}
