using Files.Sdk.Storage;
using Files.Sdk.Storage.Enums;
using Files.Sdk.Storage.Extensions;
using Files.Sdk.Storage.LocatableStorage;
using Files.Sdk.Storage.ModifiableStorage;
using Files.Core.Helpers;
using FluentFTP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Storage.FtpStorage
{
	public sealed class FtpStorageFolder : FtpStorable, ILocatableFolder, IModifiableFolder
	{
		public FtpStorageFolder(string path, string name)
			: base(path, name)
		{
		}

		/// <inheritdoc/>
		public async Task<IFile> GetFileAsync(string fileName, CancellationToken cancellationToken = default)
		{
			using var ftpClient = GetFtpClient();
			await ftpClient.EnsureConnectedAsync(cancellationToken);

			var path = FtpHelpers.GetFtpPath(PathHelpers.Combine(Path, fileName));
			var item = await ftpClient.GetObjectInfo(path, token: cancellationToken);

			if (item is null || item.Type != FtpObjectType.File)
				throw new FileNotFoundException();

			return new FtpStorageFile(path, item.Name);
		}

		/// <inheritdoc/>
		public async Task<IFolder> GetFolderAsync(string folderName, CancellationToken cancellationToken = default)
		{
			using var ftpClient = GetFtpClient();
			await ftpClient.EnsureConnectedAsync(cancellationToken);

			var path = FtpHelpers.GetFtpPath(PathHelpers.Combine(Path, folderName));
			var item = await ftpClient.GetObjectInfo(path, token: cancellationToken);

			if (item is null || item.Type != FtpObjectType.Directory)
				throw new DirectoryNotFoundException();

			return new FtpStorageFolder(path, item.Name);
		}

		/// <inheritdoc/>
		public async IAsyncEnumerable<IStorable> GetItemsAsync(StorableKind kind = StorableKind.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			using var ftpClient = GetFtpClient();
			await ftpClient.EnsureConnectedAsync(cancellationToken);

			if (kind == StorableKind.Files)
			{
				foreach (var item in await ftpClient.GetListing(Path, cancellationToken))
				{
					if (item.Type == FtpObjectType.File)
						yield return new FtpStorageFile(item.FullName, item.Name);
				}
			}
			else if (kind == StorableKind.Folders)
			{
				foreach (var item in await ftpClient.GetListing(Path, cancellationToken))
				{
					if (item.Type == FtpObjectType.Directory)
						yield return new FtpStorageFolder(item.FullName, item.Name);
				}
			}
			else
			{
				foreach (var item in await ftpClient.GetListing(Path, cancellationToken))
				{
					if (item.Type == FtpObjectType.File)
						yield return new FtpStorageFile(item.FullName, item.Name);

					if (item.Type == FtpObjectType.Directory)
						yield return new FtpStorageFolder(item.FullName, item.Name);
				}
			}
		}

		/// <inheritdoc/>
		public async Task DeleteAsync(IStorable item, bool permanently = false, CancellationToken cancellationToken = default)
		{
			using var ftpClient = GetFtpClient();
			await ftpClient.EnsureConnectedAsync(cancellationToken);

			if (item is ILocatableFile locatableFile)
			{
				await ftpClient.DeleteFile(locatableFile.Path, cancellationToken);
			}
			else if (item is ILocatableFolder locatableFolder)
			{
				await ftpClient.DeleteDirectory(locatableFolder.Path, cancellationToken);
			}
			else
			{
				throw new ArgumentException($"Could not delete {item}.");
			}
		}

		/// <inheritdoc/>
		public async Task<IStorable> CreateCopyOfAsync(IStorable itemToCopy, CreationCollisionOption collisionOption = default, CancellationToken cancellationToken = default)
		{
			if (itemToCopy is IFile sourceFile)
			{
				var copiedFile = await CreateFileAsync(itemToCopy.Name, collisionOption, cancellationToken);
				await sourceFile.CopyContentsToAsync(copiedFile, cancellationToken);

				return copiedFile;
			}
			else
			{
				throw new NotSupportedException("Copying folders is not supported.");
			}
		}

		/// <inheritdoc/>
		public async Task<IStorable> MoveFromAsync(IStorable itemToMove, IModifiableFolder source, CreationCollisionOption collisionOption = default, CancellationToken cancellationToken = default)
		{
			using var ftpClient = GetFtpClient();
			await ftpClient.EnsureConnectedAsync(cancellationToken);

			var newItem = await CreateCopyOfAsync(itemToMove, collisionOption, cancellationToken);
			await source.DeleteAsync(itemToMove, true, cancellationToken);

			return newItem;
		}

		/// <inheritdoc/>
		public async Task<IFile> CreateFileAsync(string desiredName, CreationCollisionOption collisionOption = default, CancellationToken cancellationToken = default)
		{
			using var ftpClient = GetFtpClient();
			await ftpClient.EnsureConnectedAsync(cancellationToken);

			var newPath = $"{Path}/{desiredName}";
			if (await ftpClient.FileExists(newPath, cancellationToken))
			{
				if (collisionOption == CreationCollisionOption.FailIfExists)
					throw new IOException("File already exists.");

				if (collisionOption == CreationCollisionOption.OpenIfExists)
					return new FtpStorageFile(newPath, desiredName);
			}

			using var stream = new MemoryStream();
			var replaceExisting = collisionOption == CreationCollisionOption.ReplaceExisting;
			var result = await ftpClient.UploadStream(stream, newPath, replaceExisting ? FtpRemoteExists.Overwrite : FtpRemoteExists.Skip, token: cancellationToken);

			if (result == FtpStatus.Success)
			{
				// Success
				return new FtpStorageFile(newPath, desiredName);
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
		public async Task<IFolder> CreateFolderAsync(string desiredName, CreationCollisionOption collisionOption = default, CancellationToken cancellationToken = default)
		{
			using var ftpClient = GetFtpClient();
			await ftpClient.EnsureConnectedAsync(cancellationToken);

			var newPath = $"{Path}/{desiredName}";
			if (await ftpClient.DirectoryExists(newPath, cancellationToken))
			{
				if (collisionOption == CreationCollisionOption.FailIfExists)
					throw new IOException("Directory already exists.");

				if (collisionOption == CreationCollisionOption.OpenIfExists)
					return new FtpStorageFolder(newPath, desiredName);
			}

			var replaceExisting = collisionOption == CreationCollisionOption.ReplaceExisting;
			var isSuccessful = await ftpClient.CreateDirectory(newPath, replaceExisting, cancellationToken);
			if (!isSuccessful)
				throw new IOException("Directory was not successfully created.");

			return new FtpStorageFolder(newPath, desiredName);
		}
	}
}
