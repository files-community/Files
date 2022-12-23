using Files.App.Storage.Extensions;
using Files.Sdk.Storage;
using Files.Sdk.Storage.Enums;
using Files.Sdk.Storage.LocatableStorage;
using Files.Sdk.Storage.ModifiableStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using CreationCollisionOption = Files.Sdk.Storage.Enums.CreationCollisionOption;

namespace Files.App.Storage.WindowsStorage
{
	/// <inheritdoc cref="IFolder"/>
	public sealed class WindowsStorageFolder : WindowsStorable<StorageFolder>, ILocatableFolder, IModifiableFolder
	{
		public WindowsStorageFolder(StorageFolder storage) : base(storage) {}

		/// <inheritdoc/>
		public override async Task<ILocatableFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			var parent = await Storage.GetParentAsync().AsTask(cancellationToken);
			return new WindowsStorageFolder(parent);
		}

		/// <inheritdoc/>
		public async Task<IFolder> GetFolderAsync(string folderName, CancellationToken cancellationToken = default)
		{
			var folder = await Storage.GetFolderAsync(folderName).AsTask(cancellationToken);
			return new WindowsStorageFolder(folder);
		}

		/// <inheritdoc/>
		public async Task<IFile> GetFileAsync(string fileName, CancellationToken cancellationToken = default)
		{
			var file = await Storage.GetFileAsync(fileName).AsTask(cancellationToken);
			return new WindowsStorageFile(file);
		}

		/// <inheritdoc/>
		public async IAsyncEnumerable<IStorable> GetItemsAsync
			(StorableKind kind = StorableKind.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var items = await EnumerateItemsAsync();
			foreach (var item in items)
			{
				if (item is StorageFile storageFile)
					yield return new WindowsStorageFile(storageFile);

				if (item is StorageFolder storageFolder)
					yield return new WindowsStorageFolder(storageFolder);
			}

			async Task<IEnumerable<IStorageItem>> EnumerateItemsAsync()
			{
				var items = await Storage.GetItemsAsync().AsTask(cancellationToken);
				if (items is null)
					return Enumerable.Empty<IStorageItem>();

				return kind switch
				{
					StorableKind.Folders => items.OfType<StorageFolder>(),
					StorableKind.Files => items.OfType<StorageFile>(),
					_ => items,
				};
			}
		}

		/// <inheritdoc/>
		public async Task DeleteAsync(IStorable item, bool permanently = false, CancellationToken cancellationToken = default)
		{
			var option = permanently ? StorageDeleteOption.PermanentDelete : StorageDeleteOption.Default;

			if (item is WindowsStorable<StorageFile> storageFile)
				await storageFile.Storage.DeleteAsync(option).AsTask(cancellationToken);
			else if (item is WindowsStorable<StorageFolder> storageFolder)
				await storageFolder.Storage.DeleteAsync(option).AsTask(cancellationToken);
			else
				throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public async Task<IStorable> CreateCopyOfAsync(IStorable itemToCopy,
			CreationCollisionOption collisionOption = default, CancellationToken cancellationToken = default)
		{
			if (itemToCopy is WindowsStorable<StorageFile> storageFile)
			{
				var option = collisionOption.ToWindowsNameCollisionOption();
				var copiedFile = await storageFile.Storage.CopyAsync(Storage, itemToCopy.Name, option).AsTask(cancellationToken);
				return new WindowsStorageFile(copiedFile);
			}

			throw new ArgumentException($"Could not copy type {itemToCopy.GetType()}");
		}

		/// <inheritdoc/>
		public async Task<IStorable> MoveFromAsync(IStorable itemToMove,
			IModifiableFolder source, CreationCollisionOption collisionOption = default, CancellationToken cancellationToken = default)
		{
			if (itemToMove is WindowsStorable<StorageFile> storageFile)
			{
				var option = collisionOption.ToWindowsNameCollisionOption();
				await storageFile.Storage.MoveAsync(Storage, itemToMove.Name, option).AsTask(cancellationToken);
				return new WindowsStorageFile(storageFile.Storage);
			}

			throw new ArgumentException($"Could not copy type {itemToMove.GetType()}");
		}

		/// <inheritdoc/>
		public async Task<IFolder> CreateFolderAsync(string desiredName,
			CreationCollisionOption collisionOption = default, CancellationToken cancellationToken = default)
		{
			var option = collisionOption.ToWindowsCreationCollisionOption();
			var folder = await Storage.CreateFolderAsync(desiredName, option).AsTask(cancellationToken);
			return new WindowsStorageFolder(folder);
		}

		/// <inheritdoc/>
		public async Task<IFile> CreateFileAsync(string desiredName,
			CreationCollisionOption collisionOption = default, CancellationToken cancellationToken = default)
		{
			var option = collisionOption.ToWindowsCreationCollisionOption();
			var file = await Storage.CreateFileAsync(desiredName, option).AsTask(cancellationToken);
			return new WindowsStorageFile(file);
		}
	}
}
