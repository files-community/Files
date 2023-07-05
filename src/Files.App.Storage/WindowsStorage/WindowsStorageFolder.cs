// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Sdk.Storage;
using Files.Sdk.Storage.Enums;
using Files.Sdk.Storage.ExtendableStorage;
using Files.Sdk.Storage.LocatableStorage;
using Files.Sdk.Storage.ModifiableStorage;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.App.Storage.WindowsStorage
{
	/// <inheritdoc cref="IFolder"/>
	public sealed class WindowsStorageFolder : WindowsStorable<StorageFolder>, ILocatableFolder, IModifiableFolder, IFolderExtended
	{
		public WindowsStorageFolder(StorageFolder storage)
			: base(storage)
		{
		}

		/// <inheritdoc/>
		public async Task<IFile> GetFileAsync(string fileName, CancellationToken cancellationToken = default)
		{
			var fileTask = storage.GetFileAsync(fileName).AsTask(cancellationToken);
			var file = await fileTask;

			return new WindowsStorageFile(file);
		}

		/// <inheritdoc/>
		public async Task<IFolder> GetFolderAsync(string folderName, CancellationToken cancellationToken = default)
		{
			var folderTask = storage.GetFolderAsync(folderName).AsTask(cancellationToken);
			var folder = await folderTask;

			return new WindowsStorageFolder(folder);
		}

		/// <inheritdoc/>
		public async IAsyncEnumerable<IStorable> GetItemsAsync(StorableKind kind = StorableKind.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			if (kind == StorableKind.Files)
			{
				var files = await storage.GetFilesAsync().AsTask(cancellationToken);
				foreach (var item in files)
				{
					yield return new WindowsStorageFile(item);
				}
			}
			else if (kind == StorableKind.Folders)
			{
				var folders = await storage.GetFoldersAsync().AsTask(cancellationToken);
				foreach (var item in folders)
				{
					yield return new WindowsStorageFolder(item);
				}
			}
			else
			{
				var items = await storage.GetItemsAsync().AsTask(cancellationToken);
				foreach (var item in items)
				{
					if (item is StorageFile storageFile)
						yield return new WindowsStorageFile(storageFile);

					if (item is StorageFolder storageFolder)
						yield return new WindowsStorageFolder(storageFolder);
				}
			}
		}

		/// <inheritdoc/>
		public async Task DeleteAsync(IStorable item, bool permanently = false, CancellationToken cancellationToken = default)
		{
			if (item is WindowsStorable<StorageFile> storageFile)
			{
				await storageFile.storage.DeleteAsync(GetWindowsStorageDeleteOption(permanently)).AsTask(cancellationToken);
			}
			else if (item is WindowsStorable<StorageFolder> storageFolder)
			{
				await storageFolder.storage.DeleteAsync(GetWindowsStorageDeleteOption(permanently)).AsTask(cancellationToken);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		/// <inheritdoc/>
		public async Task<IStorable> CreateCopyOfAsync(IStorable itemToCopy, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			if (itemToCopy is WindowsStorable<StorageFile> storageFile)
			{
				var copiedFileTask = storageFile.storage.CopyAsync(storage, itemToCopy.Name, GetWindowsNameCollisionOption(overwrite)).AsTask(cancellationToken);
				var copiedFile = await copiedFileTask;

				return new WindowsStorageFile(copiedFile);
			}

			throw new ArgumentException($"Could not copy type {itemToCopy.GetType()}");
		}

		/// <inheritdoc/>
		public async Task<IStorable> MoveFromAsync(IStorable itemToMove, IModifiableFolder source, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			if (itemToMove is WindowsStorable<StorageFile> storageFile)
			{
				await storageFile.storage.MoveAsync(storage, itemToMove.Name, GetWindowsNameCollisionOption(overwrite)).AsTask(cancellationToken);
				return new WindowsStorageFile(storageFile.storage);
			}

			throw new ArgumentException($"Could not copy type {itemToMove.GetType()}");
		}

		/// <inheritdoc/>
		public async Task<IFile> CreateFileAsync(string desiredName, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			var fileTask = storage.CreateFileAsync(desiredName, GetWindowsCreationCollisionOption(overwrite)).AsTask(cancellationToken);
			var file = await fileTask;

			return new WindowsStorageFile(file);
		}

		/// <inheritdoc/>
		public async Task<IFolder> CreateFolderAsync(string desiredName, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			var folderTask = storage.CreateFolderAsync(desiredName, GetWindowsCreationCollisionOption(overwrite)).AsTask(cancellationToken);
			var folder = await folderTask;

			return new WindowsStorageFolder(folder);
		}

		/// <inheritdoc/>
		public override async Task<ILocatableFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			var parentFolderTask = storage.GetParentAsync().AsTask(cancellationToken);
			var parentFolder = await parentFolderTask;

			return new WindowsStorageFolder(parentFolder);
		}

		private static StorageDeleteOption GetWindowsStorageDeleteOption(bool deletePermanentlyFlag)
		{
			return deletePermanentlyFlag ? StorageDeleteOption.PermanentDelete : StorageDeleteOption.Default;
		}

		private static NameCollisionOption GetWindowsNameCollisionOption(bool overwrite)
		{
			return overwrite ? NameCollisionOption.ReplaceExisting : NameCollisionOption.GenerateUniqueName;
		}

		private static CreationCollisionOption GetWindowsCreationCollisionOption(bool overwrite)
		{
			return overwrite ? CreationCollisionOption.ReplaceExisting : CreationCollisionOption.OpenIfExists;
		}
	}
}
