// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;
using FluentFTP;
using System.IO;
using System.Runtime.CompilerServices;

namespace Files.App.Storage.Storables
{
	public sealed class FtpStorageFolder : FtpStorable, IModifiableFolder, IChildFolder, IDirectCopy, IDirectMove, IGetFirstByName
	{
		public FtpStorageFolder(string path, string name, IFolder? parent)
			: base(path, name, parent)
		{
		}

		/// <inheritdoc/>
		public async Task<IStorableChild> GetFirstByNameAsync(string folderName, CancellationToken cancellationToken = default)
		{
			using var ftpClient = GetFtpClient();
			await ftpClient.EnsureConnectedAsync(cancellationToken);

			var path = FtpHelpers.GetFtpPath(PathHelpers.Combine(Id, folderName));
			var item = await ftpClient.GetObjectInfo(path, token: cancellationToken);

			if (item is null)
				throw new FileNotFoundException();

			if (item.Type == FtpObjectType.Directory)
				return new FtpStorageFolder(path, item.Name, this);
			else
				return new FtpStorageFile(path, item.Name, this);

		}

		/// <inheritdoc/>
		public async IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType kind = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			using var ftpClient = GetFtpClient();
			await ftpClient.EnsureConnectedAsync(cancellationToken);

			if (kind == StorableType.File)
			{
				foreach (var item in await ftpClient.GetListing(Id, cancellationToken))
				{
					if (item.Type == FtpObjectType.File)
						yield return new FtpStorageFile(item.FullName, item.Name, this);
				}
			}
			else if (kind == StorableType.Folder)
			{
				foreach (var item in await ftpClient.GetListing(Id, cancellationToken))
				{
					if (item.Type == FtpObjectType.Directory)
						yield return new FtpStorageFolder(item.FullName, item.Name, this);
				}
			}
			else
			{
				foreach (var item in await ftpClient.GetListing(Id, cancellationToken))
				{
					if (item.Type == FtpObjectType.File)
						yield return new FtpStorageFile(item.FullName, item.Name, this);

					if (item.Type == FtpObjectType.Directory)
						yield return new FtpStorageFolder(item.FullName, item.Name, this);
				}
			}
		}

		/// <inheritdoc/>
		public Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromException<IFolderWatcher>(new NotSupportedException());
		}

		/// <inheritdoc/>
		public async Task DeleteAsync(IStorableChild item, CancellationToken cancellationToken = default)
		{
			using var ftpClient = GetFtpClient();
			await ftpClient.EnsureConnectedAsync(cancellationToken);

			if (item is IFile locatableFile)
			{
				await ftpClient.DeleteFile(locatableFile.Id, cancellationToken);
			}
			else if (item is IFolder locatableFolder)
			{
				await ftpClient.DeleteDirectory(locatableFolder.Id, cancellationToken);
			}
			else
			{
				throw new ArgumentException($"Could not delete {item}.");
			}
		}

		/// <inheritdoc/>
		public async Task<IStorableChild> CreateCopyOfAsync(IStorableChild itemToCopy, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			if (itemToCopy is IFile sourceFile)
			{
				var copiedFile = await CreateFileAsync(itemToCopy.Name, overwrite, cancellationToken);
				await sourceFile.CopyContentsToAsync(copiedFile, cancellationToken);

				return copiedFile;
			}
			else
			{
				throw new NotSupportedException("Copying folders is not supported.");
			}
		}

		/// <inheritdoc/>
		public async Task<IStorableChild> MoveFromAsync(IStorableChild itemToMove, IModifiableFolder source, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			using var ftpClient = GetFtpClient();
			await ftpClient.EnsureConnectedAsync(cancellationToken);

			var newItem = await CreateCopyOfAsync(itemToMove, overwrite, cancellationToken);
			await source.DeleteAsync(itemToMove, cancellationToken);

			return newItem;
		}

		/// <inheritdoc/>
		public async Task<IChildFile> CreateFileAsync(string desiredName, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			using var ftpClient = GetFtpClient();
			await ftpClient.EnsureConnectedAsync(cancellationToken);

			var newPath = $"{Id}/{desiredName}";
			if (overwrite && await ftpClient.FileExists(newPath, cancellationToken))
				throw new IOException("File already exists.");

			using var stream = new MemoryStream();
			var result = await ftpClient.UploadStream(stream, newPath, overwrite ? FtpRemoteExists.Overwrite : FtpRemoteExists.Skip, token: cancellationToken);

			if (result == FtpStatus.Success)
			{
				// Success
				return new FtpStorageFile(newPath, desiredName, this);
			}
			else if (result == FtpStatus.Skipped)
			{
				// Throw exception since flag CreationCollisionOption.GenerateUniqueName was not satisfied
				throw new IOException("Couldn't generate unique name. File skipped.");
			}
			else
			{
				// File creation failed
				throw new IOException("File creation failed.");
			}
		}

		/// <inheritdoc/>
		public async Task<IChildFolder> CreateFolderAsync(string desiredName, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			using var ftpClient = GetFtpClient();
			await ftpClient.EnsureConnectedAsync(cancellationToken);

			var newPath = $"{Id}/{desiredName}";
			if (overwrite && await ftpClient.DirectoryExists(newPath, cancellationToken))
				throw new IOException("Directory already exists.");

			var isSuccessful = await ftpClient.CreateDirectory(newPath, overwrite, cancellationToken);
			if (!isSuccessful)
				throw new IOException("Directory was not successfully created.");

			return new FtpStorageFolder(newPath, desiredName, this);
		}
	}
}
