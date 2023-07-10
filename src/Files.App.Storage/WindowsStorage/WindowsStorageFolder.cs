// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Storage;
using Files.Core.Storage.Enums;
using Files.Core.Storage.LocatableStorage;
using Files.Core.Storage.ModifiableStorage;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Files.Core.Storage.DirectStorage;
using Files.Core.Storage.ExtendableStorage;
using Files.Core.Storage.NestedStorage;

namespace Files.App.Storage.WindowsStorage
{
	/// <inheritdoc cref="IFolder"/>
	public sealed class WindowsStorageFolder : WindowsStorable<StorageFolder>, ILocatableFolder, IFolderExtended, INestedFolder, IDirectCopy, IDirectMove
	{
		// TODO: Implement IMutableFolder

		public WindowsStorageFolder(StorageFolder storage)
			: base(storage)
		{
		}

		/// <inheritdoc/>
		public async Task<INestedFile> GetFileAsync(string fileName, CancellationToken cancellationToken = default)
		{
			var file = await storage.GetFileAsync(fileName).AsTask(cancellationToken);
			return new WindowsStorageFile(file);
		}

		/// <inheritdoc/>
		public async Task<INestedFolder> GetFolderAsync(string folderName, CancellationToken cancellationToken = default)
		{
			var folder = await storage.GetFolderAsync(folderName).AsTask(cancellationToken);
			return new WindowsStorageFolder(folder);
		}

		/// <inheritdoc/>
		public async IAsyncEnumerable<INestedStorable> GetItemsAsync(StorableKind kind = StorableKind.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			switch (kind)
			{
				case StorableKind.Files:
				{
					var files = await storage.GetFilesAsync().AsTask(cancellationToken);
					foreach (var item in files)
					{
						yield return new WindowsStorageFile(item);
					}

					break;
				}

				case StorableKind.Folders:
				{
					var folders = await storage.GetFoldersAsync().AsTask(cancellationToken);
					foreach (var item in folders)
					{
						yield return new WindowsStorageFolder(item);
					}

					break;
				}

				case StorableKind.All:
				{
					var items = await storage.GetItemsAsync().AsTask(cancellationToken);
					foreach (var item in items)
					{
						if (item is StorageFile storageFile)
							yield return new WindowsStorageFile(storageFile);

						if (item is StorageFolder storageFolder)
							yield return new WindowsStorageFolder(storageFolder);
					}

					break;
				}

				default:
					yield break;
			}
		}

		/// <inheritdoc/>
		public Task DeleteAsync(INestedStorable item, bool permanently = default, CancellationToken cancellationToken = default)
		{
			return item switch
			{
				WindowsStorable<StorageFile> storageFile => storageFile.storage
					.DeleteAsync(GetWindowsStorageDeleteOption(permanently))
					.AsTask(cancellationToken),

				WindowsStorable<StorageFolder> storageFolder => storageFolder.storage
					.DeleteAsync(GetWindowsStorageDeleteOption(permanently))
					.AsTask(cancellationToken),

				_ => throw new NotImplementedException()
			};
		}

		/// <inheritdoc/>
		public async Task<INestedStorable> CreateCopyOfAsync(INestedStorable itemToCopy, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			if (itemToCopy is WindowsStorable<StorageFile> sourceFile)
			{
				var copiedFile = await sourceFile.storage.CopyAsync(storage, itemToCopy.Name, GetWindowsNameCollisionOption(overwrite)).AsTask(cancellationToken);
				return new WindowsStorageFile(copiedFile);
			}

			throw new ArgumentException($"Could not copy type {itemToCopy.GetType()}");
		}

		/// <inheritdoc/>
		public async Task<INestedStorable> MoveFromAsync(INestedStorable itemToMove, IModifiableFolder source, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			if (itemToMove is WindowsStorable<StorageFile> sourceFile)
			{
				await sourceFile.storage.MoveAsync(storage, itemToMove.Name, GetWindowsNameCollisionOption(overwrite)).AsTask(cancellationToken);
				return new WindowsStorageFile(sourceFile.storage);
			}

			throw new ArgumentException($"Could not copy type {itemToMove.GetType()}");
		}

		/// <inheritdoc/>
		public async Task<INestedFile> CreateFileAsync(string desiredName, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			var file = await storage.CreateFileAsync(desiredName, GetWindowsCreationCollisionOption(overwrite)).AsTask(cancellationToken);
			return new WindowsStorageFile(file);
		}

		/// <inheritdoc/>
		public async Task<INestedFolder> CreateFolderAsync(string desiredName, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			var folder = await storage.CreateFolderAsync(desiredName, GetWindowsCreationCollisionOption(overwrite)).AsTask(cancellationToken);
			return new WindowsStorageFolder(folder);
		}

		/// <inheritdoc/>
		public override async Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			var parentFolder = await storage.GetParentAsync().AsTask(cancellationToken);
			return new WindowsStorageFolder(parentFolder);
		}

		private static StorageDeleteOption GetWindowsStorageDeleteOption(bool permanently)
		{
			return permanently ? StorageDeleteOption.PermanentDelete : StorageDeleteOption.Default;
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
