// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;

namespace Files.App.Storage.SftpStorage
{
	/// <inheritdoc cref="IFtpStorageService"/>
	public sealed class SftpStorageService : ISftpStorageService
	{
		/// <inheritdoc/>
		public async Task<IFolder> GetFolderAsync(string id, CancellationToken cancellationToken = default)
		{
			using var sftpClient = SftpHelpers.GetSftpClient(id);
			await sftpClient.EnsureConnectedAsync(cancellationToken);

			var ftpPath = SftpHelpers.GetSftpPath(id);
			var item = await Task.Run(() => sftpClient.Get(ftpPath), cancellationToken);
			if (item is null || !item.IsDirectory)
				throw new DirectoryNotFoundException("Directory was not found from path.");

			return new SftpStorageFolder(ftpPath, item.Name, null);
		}

		/// <inheritdoc/>
		public async Task<IFile> GetFileAsync(string id, CancellationToken cancellationToken = default)
		{
			using var sftpClient = SftpHelpers.GetSftpClient(id);
			await sftpClient.EnsureConnectedAsync(cancellationToken);

			var ftpPath = SftpHelpers.GetSftpPath(id);
			var item = await Task.Run(() => sftpClient.Get(ftpPath), cancellationToken);
			if (item is null || item.IsDirectory)
				throw new FileNotFoundException("File was not found from path.");

			return new SftpStorageFile(ftpPath, item.Name, null);
		}
	}
}
