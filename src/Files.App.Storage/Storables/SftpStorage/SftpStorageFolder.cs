// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Storage.FtpStorage;
using Files.Shared.Helpers;
using System.IO;
using System.Runtime.CompilerServices;

namespace Files.App.Storage.SftpStorage
{
	public sealed class SftpStorageFolder : SftpStorable, ILocatableFolder, IModifiableFolder, IFolderExtended, INestedFolder, IDirectCopy, IDirectMove
	{
		public SftpStorageFolder(string path, string name, IFolder? parent)
			: base(path, name, parent)
		{
		}

		/// <inheritdoc/>
		public async Task<INestedFile> GetFileAsync(string fileName, CancellationToken cancellationToken = default)
		{
			using var sftpClient = GetSftpClient();
			await sftpClient.EnsureConnectedAsync(cancellationToken);

			var path = SftpHelpers.GetSftpPath(PathHelpers.Combine(Path, fileName));
			var item = await Task.Run(() => sftpClient.Get(path), cancellationToken);

			if (item is null || item.IsDirectory)
				throw new FileNotFoundException();

			return new SftpStorageFile(path, item.Name, this);
		}

		/// <inheritdoc/>
		public async Task<INestedFolder> GetFolderAsync(string folderName, CancellationToken cancellationToken = default)
		{
			using var sftpClient = GetSftpClient();
			await sftpClient.EnsureConnectedAsync(cancellationToken);

			var path = FtpHelpers.GetFtpPath(PathHelpers.Combine(Path, folderName));
			var item = await Task.Run(() => sftpClient.Get(path), cancellationToken);

			if (item is null || !item.IsDirectory)
				throw new DirectoryNotFoundException();

			return new SftpStorageFolder(path, item.Name, this);
		}

		/// <inheritdoc/>
		public async IAsyncEnumerable<INestedStorable> GetItemsAsync(StorableKind kind = StorableKind.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			using var sftpClient = GetSftpClient();
			await sftpClient.EnsureConnectedAsync(cancellationToken);

			if (kind == StorableKind.Files)
			{
				await foreach (var item in sftpClient.ListDirectoryAsync(Path, cancellationToken))
				{
					if (!item.IsDirectory)
						yield return new SftpStorageFile(item.FullName, item.Name, this);
				}
			}
			else if (kind == StorableKind.Folders)
			{
				await foreach (var item in sftpClient.ListDirectoryAsync(Path, cancellationToken))
				{
					if (item.IsDirectory)
						yield return new SftpStorageFolder(item.FullName, item.Name, this);
				}
			}
			else
			{
				await foreach (var item in sftpClient.ListDirectoryAsync(Path, cancellationToken))
				{
					if (!item.IsDirectory)
						yield return new SftpStorageFile(item.FullName, item.Name, this);

					if (item.IsDirectory)
						yield return new SftpStorageFolder(item.FullName, item.Name, this);
				}
			}
		}

		/// <inheritdoc/>
		public async Task DeleteAsync(INestedStorable item, bool permanently = false, CancellationToken cancellationToken = default)
		{
			using var sftpClient = GetSftpClient();
			await sftpClient.EnsureConnectedAsync(cancellationToken);

			if (item is ILocatableFile locatableFile)
			{
				await sftpClient.DeleteFileAsync(locatableFile.Path, cancellationToken);
			}
			else if (item is ILocatableFolder locatableFolder)
			{
				// SSH.NET doesn't have an async equalivent for DeleteDirectory, for now a Task.Run could do.
				await Task.Run(() => sftpClient.DeleteDirectory(locatableFolder.Path), cancellationToken);
			}
			else
			{
				throw new ArgumentException($"Could not delete {item}.");
			}
		}

		/// <inheritdoc/>
		public async Task<INestedStorable> CreateCopyOfAsync(INestedStorable itemToCopy, bool overwrite = default, CancellationToken cancellationToken = default)
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
		public async Task<INestedStorable> MoveFromAsync(INestedStorable itemToMove, IModifiableFolder source, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			using var sftpClient = GetSftpClient();
			await sftpClient.EnsureConnectedAsync(cancellationToken);

			var newItem = await CreateCopyOfAsync(itemToMove, overwrite, cancellationToken);
			await source.DeleteAsync(itemToMove, true, cancellationToken);

			return newItem;
		}

		/// <inheritdoc/>
		public async Task<INestedFile> CreateFileAsync(string desiredName, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			using var sftpClient = GetSftpClient();
			await sftpClient.EnsureConnectedAsync(cancellationToken);

			var newPath = $"{Path}/{desiredName}";
			if (overwrite && await Task.Run(() => sftpClient.Exists(newPath)))
				throw new IOException("File already exists.");

			using var stream = new MemoryStream();

			try
			{
				await Task.Run(() => sftpClient.UploadFile(stream, newPath), cancellationToken);
				return new SftpStorageFile(newPath, desiredName, this);
			}
			catch
			{
				// File creation failed
				throw new IOException("File creation failed.");
			}
		}

		/// <inheritdoc/>
		public async Task<INestedFolder> CreateFolderAsync(string desiredName, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			using var sftpClient = GetSftpClient();
			await sftpClient.EnsureConnectedAsync(cancellationToken);

			var newPath = $"{Path}/{desiredName}";
			if (overwrite && await Task.Run(() => sftpClient.Exists(newPath), cancellationToken))
				throw new IOException("Directory already exists.");

			// SSH.NET doesn't have an async equalivent for CreateDirectory, for now a Task.Run could do.
			await Task.Run(() => sftpClient.CreateDirectory(newPath), cancellationToken);

			return new SftpStorageFolder(newPath, desiredName, this);
		}
	}
}
