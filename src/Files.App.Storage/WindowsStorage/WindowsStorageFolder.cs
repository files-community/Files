// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Storage.NativeStorage;
using Files.Sdk.Storage;
using Files.Sdk.Storage.Enums;
using Files.Sdk.Storage.LocatableStorage;
using Files.Sdk.Storage.ModifiableStorage;
using Files.Sdk.Storage.MutableStorage;
using Files.Shared.Extensions;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Vanara.Windows.Shell;
using CreationCollisionOption = Files.Sdk.Storage.Enums.CreationCollisionOption;

namespace Files.App.Storage.WindowsStorage
{
	/// <inheritdoc cref="IFolder"/>
	public sealed class WindowsStorageFolder : NativeStorable<StorageFolder>, ILocatableFolder, IModifiableFolder, IMutableFolder
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
			if (item is NativeStorable<StorageFile> storageFile)
			{
				await storageFile.storage.DeleteAsync(GetWindowsStorageDeleteOption(permanently)).AsTask(cancellationToken);
			}
			else if (item is NativeStorable<StorageFolder> storageFolder)
			{
				await storageFolder.storage.DeleteAsync(GetWindowsStorageDeleteOption(permanently)).AsTask(cancellationToken);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		/// <inheritdoc/>
		public async Task<IStorable> CreateCopyOfAsync(IStorable itemToCopy, CreationCollisionOption collisionOption = default, CancellationToken cancellationToken = default)
		{
			if (itemToCopy is NativeStorable<StorageFile> storageFile)
			{
				var copiedFileTask = storageFile.storage.CopyAsync(storage, itemToCopy.Name, GetWindowsNameCollisionOption(collisionOption)).AsTask(cancellationToken);
				var copiedFile = await copiedFileTask;

				return new WindowsStorageFile(copiedFile);
			}

			throw new ArgumentException($"Could not copy type {itemToCopy.GetType()}");
		}

		/// <inheritdoc/>
		public async Task<IStorable> MoveFromAsync(IStorable itemToMove, IModifiableFolder source, CreationCollisionOption collisionOption = default, CancellationToken cancellationToken = default)
		{
			if (itemToMove is NativeStorable<StorageFile> storageFile)
			{
				await storageFile.storage.MoveAsync(storage, itemToMove.Name, GetWindowsNameCollisionOption(collisionOption)).AsTask(cancellationToken);
				return new WindowsStorageFile(storageFile.storage);
			}

			throw new ArgumentException($"Could not copy type {itemToMove.GetType()}");
		}

		/// <inheritdoc/>
		public async Task<IFile> CreateFileAsync(string desiredName, CreationCollisionOption collisionOption = default, CancellationToken cancellationToken = default)
		{
			var fileTask = storage.CreateFileAsync(desiredName, GetWindowsCreationCollisionOption(collisionOption)).AsTask(cancellationToken);
			var file = await fileTask;

			return new WindowsStorageFile(file);
		}

		/// <inheritdoc/>
		public async Task<IFolder> CreateFolderAsync(string desiredName, CreationCollisionOption collisionOption = default, CancellationToken cancellationToken = default)
		{
			var folderTask = storage.CreateFolderAsync(desiredName, GetWindowsCreationCollisionOption(collisionOption)).AsTask(cancellationToken);
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

		private static Windows.Storage.NameCollisionOption GetWindowsNameCollisionOption(
			CreationCollisionOption options)
		{
			return options switch
			{
				CreationCollisionOption.GenerateUniqueName => Windows.Storage.NameCollisionOption.GenerateUniqueName,
				CreationCollisionOption.ReplaceExisting => Windows.Storage.NameCollisionOption.ReplaceExisting,
				CreationCollisionOption.FailIfExists => Windows.Storage.NameCollisionOption.FailIfExists,
				_ => throw new ArgumentOutOfRangeException(nameof(options))
			};
		}

		private static Windows.Storage.CreationCollisionOption GetWindowsCreationCollisionOption(
			CreationCollisionOption options)
		{
			return options switch
			{
				CreationCollisionOption.GenerateUniqueName => Windows.Storage.CreationCollisionOption.GenerateUniqueName,
				CreationCollisionOption.ReplaceExisting => Windows.Storage.CreationCollisionOption.ReplaceExisting,
				CreationCollisionOption.OpenIfExists => Windows.Storage.CreationCollisionOption.OpenIfExists,
				CreationCollisionOption.FailIfExists => Windows.Storage.CreationCollisionOption.FailIfExists,
				_ => throw new ArgumentOutOfRangeException(nameof(options))
			};
		}

		public Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult<IFolderWatcher>(new WindowsStorageFolderWatcher(this));
		}

		public async IAsyncEnumerable<IStorable> SearchAsync(string userQuery, SearchDepth depth = SearchDepth.Shallow)
		{
			IEnumerable<ShellItem> itemSearchLocations = 
				(depth == SearchDepth.Deep) 
				? new ShellFolder(Path).EnumerateChildren(FolderItemFilter.Folders)
				: new List<ShellItem> { new ShellItem(Path) };

			var resultsShellItem = ShellSearch.GetSearchResults(SearchCondition.CreateFromStructuredQuery(userQuery), Name, itemSearchLocations?.Select(x => new ShellFolder(x)));
			
			if (resultsShellItem is not null)
			{
				var service = new WindowsStorageService();
				var shellResults = new ShellFolder(resultsShellItem).EnumerateChildren(FolderItemFilter.FlatList | FolderItemFilter.Folders | FolderItemFilter.NonFolders | FolderItemFilter.FastItems).Select(x => service.GetItemFromPathAsync(x.FileSystemPath));

				foreach (var item in shellResults)
				{
					yield return await item;
				}
			}
			else
			{
				var result = storage.CreateItemQueryWithOptions(new Windows.Storage.Search.QueryOptions(Windows.Storage.Search.CommonFolderQuery.DefaultQuery)
				{
					FolderDepth = depth == SearchDepth.Deep ? Windows.Storage.Search.FolderDepth.Deep : Windows.Storage.Search.FolderDepth.Shallow,
					UserSearchFilter = userQuery,
					IndexerOption = Windows.Storage.Search.IndexerOption.UseIndexerWhenAvailable
				});

				foreach (var item in await result.GetItemsAsync())
				{
					yield return await new WindowsStorageService().GetItemFromPathAsync(item.Path);
				}

				List<IStorable> results = new List<IStorable>();

				if (depth == SearchDepth.Deep)
				{
					async Task<IStorable> SearchInternalAsync(WindowsStorageFolder folder)
					{
						await foreach (IStorable item in folder.GetItemsAsync(StorableKind.All))
						{
							if (item is WindowsStorageFolder folderChild)
							{
								results.Add(await SearchInternalAsync(folderChild));
								return item;
							}
							else
							{
								return item;
							}
						}
						return folder;
					}
					await SearchInternalAsync(this);
				}
				else
				{
					await foreach (var item in GetItemsAsync(StorableKind.All))
					{
						results.Add(item);
					}
				}

				foreach (var item in results.Where(x => x.Name.Contains(userQuery)))
				{
					yield return item;
				}
			}
		}
	}
}
