// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Storage;
using Files.Core.Storage.LocatableStorage;
using FluentFTP;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Storage.FtpStorage
{
	/// <inheritdoc cref="IFtpStorageService"/>
	public sealed class FtpStorageService : IFtpStorageService
	{
		/// <inheritdoc/>
		public async Task<IFolder> GetFolderAsync(string id, CancellationToken cancellationToken = default)
		{
			using var ftpClient = FtpHelpers.GetFtpClient(id);
			await ftpClient.EnsureConnectedAsync(cancellationToken);

			var ftpPath = FtpHelpers.GetFtpPath(id);
			var item = await ftpClient.GetObjectInfo(ftpPath, token: cancellationToken);
			if (item is null || item.Type != FtpObjectType.Directory)
				throw new DirectoryNotFoundException("Directory was not found from path.");

			return new FtpStorageFolder(ftpPath, item.Name, null);
		}

		/// <inheritdoc/>
		public async Task<IFile> GetFileAsync(string id, CancellationToken cancellationToken = default)
		{
			using var ftpClient = FtpHelpers.GetFtpClient(id);
			await ftpClient.EnsureConnectedAsync(cancellationToken);

			var ftpPath = FtpHelpers.GetFtpPath(id);
			var item = await ftpClient.GetObjectInfo(ftpPath, token: cancellationToken);
			if (item is null || item.Type != FtpObjectType.File)
				throw new FileNotFoundException("File was not found from path.");

			return new FtpStorageFile(ftpPath, item.Name, null);
		}
	}
}
