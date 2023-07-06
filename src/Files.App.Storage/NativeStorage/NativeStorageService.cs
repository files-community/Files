// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Sdk.Storage;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
		public Task<IFolder> GetFolderAsync(string id, CancellationToken cancellationToken = default)
		{
			if (!Directory.Exists(id))
				throw new DirectoryNotFoundException();

			return Task.FromResult<IFolder>(new NativeFolder(id));
		}
	}
}
