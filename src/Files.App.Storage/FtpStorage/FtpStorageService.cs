// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Sdk.Storage;
using Files.Sdk.Storage.LocatableStorage;
using FluentFTP;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Storage.FtpStorage
{
	public sealed class FtpStorageService : IFtpStorageService
	{
		public Task<bool> IsAccessibleAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult(true); // TODO: Check if FTP is available
		}

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

		public async Task<ILocatableFolder> GetFolderFromPathAsync(string path, CancellationToken cancellationToken = default)
		{
			using var ftpClient = FtpHelpers.GetFtpClient(path);
			await ftpClient.EnsureConnectedAsync(cancellationToken);

			var ftpPath = FtpHelpers.GetFtpPath(path);
			var item = await ftpClient.GetObjectInfo(ftpPath, token: cancellationToken);
			if (item is null || item.Type != FtpObjectType.Directory)
				throw new DirectoryNotFoundException("Directory was not found from path.");

			return new FtpStorageFolder(ftpPath, item.Name);
		}

		public async Task<ILocatableFile> GetFileFromPathAsync(string path, CancellationToken cancellationToken = default)
		{
			using var ftpClient = FtpHelpers.GetFtpClient(path);
			await ftpClient.EnsureConnectedAsync(cancellationToken);

			var ftpPath = FtpHelpers.GetFtpPath(path);
			var item = await ftpClient.GetObjectInfo(ftpPath, token: cancellationToken);
			if (item is null || item.Type != FtpObjectType.File)
				throw new FileNotFoundException("File was not found from path.");

			return new FtpStorageFile(ftpPath, item.Name);
		}
	}
}
