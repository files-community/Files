// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.IO;

namespace Files.App.Data.Models
{
	public sealed class PinnedFoldersManager
	{
		private IUserSettingsService userSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private IQuickAccessService QuickAccessService { get; } = Ioc.Default.GetRequiredService<IQuickAccessService>();

		public EventHandler<NotifyCollectionChangedEventArgs>? DataChanged;

		private readonly SemaphoreSlim addSyncSemaphore = new(1, 1);

		public List<string> PinnedFolders { get; set; } = [];

		private int _syncSuspendCount;

		/// <summary>
		/// Returns true when sync is suspended
		/// </summary>
		public bool IsSyncSuspended => _syncSuspendCount > 0;

		/// <summary>
		/// Suspends sync operations until the returned value is disposed
		/// </summary>
		public IDisposable SuspendSync()
		{
			Interlocked.Increment(ref _syncSuspendCount);
			return new SyncSuspensionScope(this);
		}

		private sealed class SyncSuspensionScope(PinnedFoldersManager owner) : IDisposable
		{
			private int _disposed;

			public void Dispose()
			{
				if (Interlocked.Exchange(ref _disposed, 1) == 0)
					Interlocked.Decrement(ref owner._syncSuspendCount);
			}
		}

		public readonly List<INavigationControlItem> _PinnedFolderItems = [];

		[JsonIgnore]
		public IReadOnlyList<INavigationControlItem> PinnedFolderItems
		{
			get
			{
				lock (_PinnedFolderItems)
					return _PinnedFolderItems.ToList().AsReadOnly();
			}
		}

		/// <summary>
		/// Updates items with the pinned items from the explorer sidebar
		/// </summary>
		public async Task UpdateItemsWithExplorerAsync()
		{
			if (IsSyncSuspended)
				return;

			await addSyncSemaphore.WaitAsync();

			try
			{
				var formerPinnedFolders = PinnedFolders.ToList();

				PinnedFolders = (await QuickAccessService.GetPinnedFoldersAsync())
					.Where(link => (bool?)link.Properties["System.Home.IsPinned"] ?? false)
					.Select(link => link.FilePath).ToList();

				if (formerPinnedFolders.SequenceEqual(PinnedFolders))
					return;
				if (formerPinnedFolders.Count == PinnedFolders.Count &&
					new HashSet<string>(formerPinnedFolders, StringComparer.OrdinalIgnoreCase)
						.SetEquals(PinnedFolders))
				{
					ApplyReorderToPinnedItems();
					return;
				}

				RemoveStaleSidebarItems();
				foreach (var path in PinnedFolders)
				{
					bool exists;
					lock (_PinnedFolderItems)
					{
						exists = _PinnedFolderItems.Any(x => x.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
					}
					if (!exists)
						await AddItemToSidebarAsync(path);
				}
				ApplyReorderToPinnedItems();
			}
			finally
			{
				addSyncSemaphore.Release();
			}
		}

		/// <summary>
		/// Reorders <see cref="_PinnedFolderItems"/> and <see cref="PinnedFolders"/> to match
		/// <paramref name="newOrder"/> without firing <see cref="DataChanged"/> events.
		/// Only intended to be called from <c>SidebarViewModel</c>.
		/// </summary>
		internal void UpdateOrderSilently(string[] newOrder)
		{
			lock (_PinnedFolderItems)
			{
				ReorderPinnedItemsCore(newOrder, moves: null);
			}

			PinnedFolders = newOrder.ToList();
		}

		private void ApplyReorderToPinnedItems()
		{
			var moves = new List<(INavigationControlItem item, int newIndex, int oldIndex)>();

			lock (_PinnedFolderItems)
			{
				ReorderPinnedItemsCore(PinnedFolders, moves);
			}

			foreach (var move in moves)
			{
				DataChanged?.Invoke(SectionType.Pinned, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, move.item, move.newIndex, move.oldIndex));
			}
		}

		/// <summary>
		/// Reorders <see cref="_PinnedFolderItems"/> to match <paramref name="desiredOrder"/>.
		/// Must be called while holding the <c>_PinnedFolderItems</c> lock
		/// </summary>
		private void ReorderPinnedItemsCore(IList<string> desiredOrder, List<(INavigationControlItem item, int newIndex, int oldIndex)>? moves)
		{
			int baseIndex = GetPinnedItemsBaseIndex();

			for (int i = 0; i < desiredOrder.Count; i++)
			{
				var path = desiredOrder[i];
				var currentItem = _PinnedFolderItems.FirstOrDefault(x => x.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
				if (currentItem is null)
					continue;

				int oldIndex = _PinnedFolderItems.IndexOf(currentItem);
				int newIndex = baseIndex + i;

				if (oldIndex != newIndex && newIndex < _PinnedFolderItems.Count)
				{
					_PinnedFolderItems.RemoveAt(oldIndex);
					_PinnedFolderItems.Insert(newIndex, currentItem);
					moves?.Add((currentItem, newIndex, oldIndex));
				}
			}
		}

		/// <summary>
		/// Returns the base index of user-pinned items in <see cref="_PinnedFolderItems"/>.
		/// Must be called while holding the <c>_PinnedFolderItems</c> lock.
		/// <para>
		/// Invariant assumed: default-location items always appear before user-pinned items and
		/// are never interspersed with them. If the first non-default item is found, that is the
		/// base index.
		/// </para>
		/// </summary>
		private int GetPinnedItemsBaseIndex()
		{
			int baseIndex = _PinnedFolderItems.FindIndex(x => x is LocationItem item && !item.IsDefaultLocation);
			if (baseIndex == -1)
			{
				baseIndex = _PinnedFolderItems.FindLastIndex(x => x is LocationItem item && item.IsDefaultLocation);
				baseIndex = baseIndex == -1 ? 0 : baseIndex + 1;
			}
			return baseIndex;
		}

		/// <summary>
		/// Returns the index of the location item in the navigation sidebar
		/// </summary>
		/// <param name="locationItem">The location item</param>
		/// <returns>Index of the item</returns>
		public int IndexOfItem(INavigationControlItem locationItem)
		{
			lock (_PinnedFolderItems)
			{
				return _PinnedFolderItems.FindIndex(x => x.Path == locationItem.Path);
			}
		}

		public async Task<LocationItem> CreateLocationItemFromPathAsync(string path)
		{
			var item = await FilesystemTasks.Wrap(() => DriveHelpers.GetRootFromPathAsync(path));
			var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path, item));
			LocationItem locationItem;

			if (string.Equals(path, Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
				locationItem = LocationItem.Create<RecycleBinLocationItem>();
			else
			{
				locationItem = LocationItem.Create<LocationItem>();

				if (path.Equals(Constants.UserEnvironmentPaths.MyComputerPath, StringComparison.OrdinalIgnoreCase))
					locationItem.Text = Strings.ThisPC.GetLocalizedResource();
				else if (path.Equals(Constants.UserEnvironmentPaths.NetworkFolderPath, StringComparison.OrdinalIgnoreCase))
					locationItem.Text = Strings.Network.GetLocalizedResource();
			}

			locationItem.Path = path;
			locationItem.Section = SectionType.Pinned;
			locationItem.MenuOptions = new ContextMenuOptions
			{
				IsLocationItem = true,
				ShowProperties = true,
				ShowUnpinItem = true,
				ShowShellItems = true,
				ShowEmptyRecycleBin = string.Equals(path, Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase)
			};
			locationItem.IsDefaultLocation = false;
			locationItem.Text = res?.Result?.DisplayName ?? Path.GetFileName(path.TrimEnd('\\'));

			if (res)
			{
				locationItem.IsInvalid = false;
				if (res.Result is not null)
					await LoadIconForLocationItemAsync(locationItem, res.Result.Path);
			}
			else
			{
				locationItem.IsInvalid = true;
				Debug.WriteLine($"Pinned item was invalid {res?.ErrorCode}, item: {path}");
				await LoadDefaultIconForLocationItemAsync(locationItem);
			}

			return locationItem;
		}

		private async Task LoadIconForLocationItemAsync(LocationItem locationItem, string path)
		{
			try
			{
				var result = await FileThumbnailHelper.GetIconAsync(
					path,
					Constants.ShellIconSizes.Small,
					true,
					IconOptions.ReturnIconOnly | IconOptions.UseCurrentScale);

				locationItem.IconData = result;

				var bitmapImage = await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => locationItem.IconData.ToBitmapAsync(), Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
				if (bitmapImage is not null)
					locationItem.Icon = bitmapImage;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error loading icon for {path}: {ex.Message}");
			}
		}

		private async Task LoadDefaultIconForLocationItemAsync(LocationItem locationItem)
		{
			try
			{
				var defaultIcon = await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => UIHelpers.GetSidebarIconResource(Constants.ImageRes.Folder));
				locationItem.Icon = defaultIcon;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error loading default icon: {ex.Message}");
			}
		}

		/// <summary>
		/// Adds the item (from a path) to the navigation sidebar
		/// </summary>
		/// <param name="path">The path which to save</param>
		/// <returns>Task</returns>
		public async Task AddItemToSidebarAsync(string path)
		{
			var locationItem = await CreateLocationItemFromPathAsync(path);

			AddLocationItemToSidebar(locationItem);
		}

		/// <summary>
		/// Adds the location item to the navigation sidebar
		/// </summary>
		/// <param name="locationItem">The location item which to save</param>
		private void AddLocationItemToSidebar(LocationItem locationItem)
		{
			int insertIndex = -1;
			lock (_PinnedFolderItems)
			{
				if (_PinnedFolderItems.Any(x => x.Path == locationItem.Path))
					return;

				var lastItem = _PinnedFolderItems.LastOrDefault(x => x.ItemType is NavigationControlItemType.Location);
				insertIndex = lastItem is not null ? _PinnedFolderItems.IndexOf(lastItem) + 1 : 0;
				_PinnedFolderItems.Insert(insertIndex, locationItem);
			}

			DataChanged?.Invoke(SectionType.Pinned, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, locationItem, insertIndex));
		}

		/// <summary>
		/// Adds all items to the navigation sidebar
		/// </summary>
		public async Task AddAllItemsToSidebarAsync()
		{
			if (userSettingsService.GeneralSettingsService.ShowPinnedSection)
				foreach (string path in PinnedFolders)
					await AddItemToSidebarAsync(path);
		}

		/// <summary>
		/// Removes stale items in the navigation sidebar
		/// </summary>
		public void RemoveStaleSidebarItems()
		{
			foreach (var childItem in PinnedFolderItems.ToList())
			{
				if (childItem is LocationItem item && !item.IsDefaultLocation && !PinnedFolders.Contains(item.Path))
				{
					lock (_PinnedFolderItems)
					{
						_PinnedFolderItems.Remove(item);
					}
					DataChanged?.Invoke(SectionType.Pinned, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
				}
			}
		}

		public async void LoadAsync(object? sender, FileSystemEventArgs e)
		{
			try
			{
				await LoadAsync();
				var pinnedFolders = await QuickAccessService.GetPinnedFoldersAsync();
				App.QuickAccessManager.UpdateQuickAccessWidget?.Invoke(null, new ModifyQuickAccessEventArgs(pinnedFolders.ToArray(), true)
				{
					Reset = true
				});
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "Error loading pinned folders from watcher");
			}
		}

		public async Task LoadAsync()
		{
			await UpdateItemsWithExplorerAsync();
		}
	}
}
