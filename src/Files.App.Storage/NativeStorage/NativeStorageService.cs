// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Sdk.Storage;
using Files.Sdk.Storage.LocatableStorage;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Storage.NativeStorage
{
	/// <inheritdoc cref="IStorageService"/>
	public sealed class NativeStorageService : IStorageService
	{
		/// <inheritdoc/>
		public Task<bool> IsAccessibleAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult(true);
		}

		/// <inheritdoc/>
		public Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default)
		{
			var fileExists = File.Exists(path);
			return Task.FromResult(fileExists);
		}

		/// <inheritdoc/>
		public Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken = default)
		{
			var directoryExists = Directory.Exists(path);
			return Task.FromResult(directoryExists);
		}

		/// <inheritdoc/>
		public Task<ILocatableFolder> GetFolderFromPathAsync(string path, CancellationToken cancellationToken = default)
		{
			if (!Directory.Exists(path))
				throw new DirectoryNotFoundException($"Directory for '{path}' was not found.");

			return Task.FromResult<ILocatableFolder>(new NativeFolder(path));
		}

		/// <inheritdoc/>
		public Task<ILocatableFile> GetFileFromPathAsync(string path, CancellationToken cancellationToken = default)
		{
			if (!File.Exists(path))
				throw new FileNotFoundException($"File for '{path}' was not found.");

			return Task.FromResult<ILocatableFile>(new NativeFile(path));
		}
	}
}
