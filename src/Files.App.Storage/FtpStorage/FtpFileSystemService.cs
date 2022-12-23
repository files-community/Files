using Files.Sdk.Storage.LocatableStorage;
using Files.Sdk.Storage.Services;
using FluentFTP;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Storage.FtpStorage
{
	public sealed class FtpFileSystemService : IFileSystemService
	{
		/// <inheritdoc/>
		public Task<bool> IsFileSystemAccessibleAsync(CancellationToken cancellationToken = default)
			=> Task.FromResult(true); // TODO: Check if FTP is available

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
		public async Task<ILocatableFolder> GetFolderFromPathAsync(string path, CancellationToken cancellationToken = default)
		{
			using var ftpClient = await GetClient(path);
			var ftpPath = path.GetFtpPath();

			var item = await ftpClient.GetObjectInfo(ftpPath, token: cancellationToken);
			if (item is null || item.Type is not FtpObjectType.Directory)
				throw new DirectoryNotFoundException("Directory was not found from path.");

			return new FtpStorageFolder(ftpPath, item.Name);
		}

		/// <inheritdoc/>
		public async Task<ILocatableFile> GetFileFromPathAsync(string path, CancellationToken cancellationToken = default)
		{
			using var client = await GetClient(path, cancellationToken);
			var ftpPath = path.GetFtpPath();

			var item = await client.GetObjectInfo(ftpPath, token: cancellationToken);
			if (item is null || item.Type is not FtpObjectType.File)
				throw new FileNotFoundException("File was not found from path.");

			return new FtpStorageFile(ftpPath, item.Name);
		}

		private static async Task<AsyncFtpClient> GetClient(string path, CancellationToken cancellationToken = default)
		{
			using AsyncFtpClient client = path.GetFtpClient();
			await client.EnsureConnectedAsync(cancellationToken);
			return client;
		}
	}
}
