using Files.App.Storage.Extensions;
using Files.Sdk.Storage;
using Files.Sdk.Storage.Enums;
using Files.Sdk.Storage.Extensions;
using Files.Sdk.Storage.LocatableStorage;
using Files.Sdk.Storage.ModifiableStorage;
using Files.Shared.Helpers;
using FluentFTP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Storage.FtpStorage
{
	/// <inheritdoc cref="IFolder"/>
	public sealed class FtpStorageFolder : FtpStorable, ILocatableFolder, IModifiableFolder
	{
		public FtpStorageFolder(string path, string name) : base(path, name) {}

		/// <inheritdoc/>
		public async Task<IFolder> GetFolderAsync(string folderName, CancellationToken cancellationToken = default)
		{
			using var client = await GetFtpClient(cancellationToken);

			var path = Path.Combine(folderName).GetFtpPath();
			var item = await client.GetObjectInfo(path, token: cancellationToken);

			if (item is null || item.Type is not FtpObjectType.Directory)
				throw new DirectoryNotFoundException();

			return new FtpStorageFolder(path, item.Name);
		}

		/// <inheritdoc/>
		public async Task<IFile> GetFileAsync(string fileName, CancellationToken cancellationToken = default)
		{
			using var client = await GetFtpClient(cancellationToken);

			var path = Path.Combine(fileName).GetFtpPath();
			var item = await client.GetObjectInfo(path, token: cancellationToken);

			if (item is null || item.Type is not FtpObjectType.File)
				throw new FileNotFoundException();

			return new FtpStorageFile(path, item.Name);
		}

		/// <inheritdoc/>
		public async IAsyncEnumerable<IStorable> GetItemsAsync
			(StorableKind kind = StorableKind.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var items = await EnumerateItemsAsync();
			foreach (var item in items)
			{
				if (item.Type is FtpObjectType.File)
					yield return new FtpStorageFile(item.FullName, item.Name);
				if (item.Type is FtpObjectType.Directory)
					yield return new FtpStorageFolder(item.FullName, item.Name);
			}

			async Task<IEnumerable<FtpListItem>> EnumerateItemsAsync()
			{
				using var client = await GetFtpClient(cancellationToken);
				var items = await client.GetListing(Path, cancellationToken);

				return kind switch
				{
					StorableKind.Files => items.Where(item => item.Type is FtpObjectType.File),
					StorableKind.Folders => items.Where(item => item.Type is FtpObjectType.Directory),
					_ => items,
				};
			}
		}

		/// <inheritdoc/>
		public async Task DeleteAsync(IStorable item, bool permanently = false, CancellationToken cancellationToken = default)
		{
			using var client = await GetFtpClient(cancellationToken);

			if (item is ILocatableFile locatableFile)
				await client.DeleteFile(locatableFile.Path, cancellationToken);
			else if (item is ILocatableFolder locatableFolder)
				await client.DeleteDirectory(locatableFolder.Path, cancellationToken);
			else
				throw new ArgumentException($"Could not delete {item}.");
		}

		/// <inheritdoc/>
		public async Task<IStorable> CreateCopyOfAsync(IStorable itemToCopy,
			CreationCollisionOption collisionOption = default, CancellationToken cancellationToken = default)
		{
			if (itemToCopy is IFile sourceFile)
			{
				var copiedFile = await CreateFileAsync(itemToCopy.Name, collisionOption, cancellationToken);
				await sourceFile.CopyContentsToAsync(copiedFile, cancellationToken);

				return copiedFile;
			}
			else
				throw new NotSupportedException("Copying folders is not supported.");
		}

		/// <inheritdoc/>
		public async Task<IStorable> MoveFromAsync(IStorable itemToMove,
			IModifiableFolder source, CreationCollisionOption collisionOption = default, CancellationToken cancellationToken = default)
		{
			using var client = await GetFtpClient(cancellationToken);

			var newItem = await CreateCopyOfAsync(itemToMove, collisionOption, cancellationToken);
			await source.DeleteAsync(itemToMove, true, cancellationToken);

			return newItem;
		}

		/// <inheritdoc/>
		public async Task<IFolder> CreateFolderAsync(string desiredName,
			CreationCollisionOption collisionOption = default, CancellationToken cancellationToken = default)
		{
			using var client = await GetFtpClient(cancellationToken);

			var newPath = $"{Path}/{desiredName}";
			if (await client.DirectoryExists(newPath, cancellationToken))
			{
				if (collisionOption is CreationCollisionOption.FailIfExists)
					throw new IOException("Directory already exists.");

				if (collisionOption is CreationCollisionOption.OpenIfExists)
					return new FtpStorageFolder(newPath, desiredName);
			}

			var replaceExisting = collisionOption is CreationCollisionOption.ReplaceExisting;
			var isSuccessful = await client.CreateDirectory(newPath, replaceExisting, cancellationToken);
			if (!isSuccessful)
				throw new IOException("Directory was not successfully created.");

			return new FtpStorageFolder(newPath, desiredName);
		}

		/// <inheritdoc/>
		public async Task<IFile> CreateFileAsync(string desiredName,
			CreationCollisionOption collisionOption = default, CancellationToken cancellationToken = default)
		{
			using var client = await GetFtpClient(cancellationToken);

			var newPath = $"{Path}/{desiredName}";
			if (await client.FileExists(newPath, cancellationToken))
			{
				if (collisionOption is CreationCollisionOption.FailIfExists)
					throw new IOException("File already exists.");

				if (collisionOption is CreationCollisionOption.OpenIfExists)
					return new FtpStorageFile(newPath, desiredName);
			}

			using var stream = new MemoryStream();
			var option = collisionOption.ToFtpRemoteExists();
			var result = await client.UploadStream(stream, newPath, option, token: cancellationToken);

			return result switch
			{
				FtpStatus.Success => new FtpStorageFile(newPath, desiredName),
				FtpStatus.Skipped => throw new IOException("Couldn't generate unique name. File skipped."),
				_ => throw new IOException("File creation failed."),
			};
		}
	}
}
