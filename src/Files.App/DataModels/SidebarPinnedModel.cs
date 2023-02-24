using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Files.App.Controllers;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.ServicesImplementation;
using Files.Backend.Services.Settings;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;

namespace Files.App.DataModels
{
	public class SidebarPinnedModel
	{
		private IUserSettingsService userSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private IQuickAccessService QuickAccessService { get; } = Ioc.Default.GetRequiredService<IQuickAccessService>();

		public EventHandler<NotifyCollectionChangedEventArgs>? DataChanged;

		private readonly SemaphoreSlim addSyncSemaphore = new(1, 1);

		public List<string> FavoriteItems { get; set; } = new List<string>();

		private readonly List<INavigationControlItem> favoriteList = new();

		[JsonIgnore]
		public IReadOnlyList<INavigationControlItem> Favorites
		{
			get
			{
				lock (favoriteList)
					return favoriteList.ToList().AsReadOnly();
			}
		}

		/// <summary>
		/// Updates items with the pinned items from the explorer sidebar
		/// </summary>
		public async Task UpdateItemsWithExplorer()
		{
			await addSyncSemaphore.WaitAsync();

			try
			{
				FavoriteItems = (await QuickAccessService.GetPinnedFoldersAsync())
					.Where(link => (bool?)link.Properties["System.Home.IsPinned"] ?? false)
					.Select(link => link.FilePath).ToList();
				RemoveStaleSidebarItems();
				await AddAllItemsToSidebar();
			}
			finally
			{
				addSyncSemaphore.Release();
			}
		}

		/// <summary>
		/// Returns the index of the location item in the navigation sidebar
		/// </summary>
		/// <param name="locationItem">The location item</param>
		/// <returns>Index of the item</returns>
		public int IndexOfItem(INavigationControlItem locationItem)
		{
			lock (favoriteList)
			{
				return favoriteList.FindIndex(x => x.Path == locationItem.Path);
			}
		}

		public async Task<LocationItem> CreateLocationItemFromPathAsync(string path)
		{
			var item = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(path));
			var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path, item));
			LocationItem locationItem;

			if (string.Equals(path, CommonPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
				locationItem = LocationItem.Create<RecycleBinLocationItem>();
			else
				locationItem = LocationItem.Create<LocationItem>();

			locationItem.Font = App.AppModel.SymbolFontFamily;
			locationItem.Path = path;
			locationItem.Section = SectionType.Favorites;
			locationItem.MenuOptions = new ContextMenuOptions
			{
				IsLocationItem = true,
				ShowProperties = true,
				ShowUnpinItem = true,
				ShowShellItems = true,
				ShowEmptyRecycleBin = string.Equals(path, CommonPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase)
			};
			locationItem.IsDefaultLocation = false;
			locationItem.Text = res.Result?.DisplayName ?? Path.GetFileName(path.TrimEnd('\\'));

			if (res || (FilesystemResult)FolderHelpers.CheckFolderAccessWithWin32(path))
			{
				locationItem.IsInvalid = false;
				if (res)
				{
					var iconData = await FileThumbnailHelper.LoadIconFromStorageItemAsync(res.Result, 96u, ThumbnailMode.ListView);
					locationItem.IconData = iconData;
					locationItem.Icon = await App.Window.DispatcherQueue.EnqueueAsync(() => locationItem.IconData.ToBitmapAsync());
				}

				if (locationItem.IconData is null)
				{
					locationItem.IconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(path, 96u);

					if (locationItem.IconData is not null)
						locationItem.Icon = await App.Window.DispatcherQueue.EnqueueAsync(() => locationItem.IconData.ToBitmapAsync());
				}
			}
			else
			{
				locationItem.Icon = await App.Window.DispatcherQueue.EnqueueAsync(() => UIHelpers.GetIconResource(Constants.ImageRes.Folder));
				locationItem.IsInvalid = true;
				Debug.WriteLine($"Pinned item was invalid {res.ErrorCode}, item: {path}");
			}

			return locationItem;
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
			lock (favoriteList)
			{
				if (favoriteList.Any(x => x.Path == locationItem.Path))
					return;

				var lastItem = favoriteList.LastOrDefault(x => x.ItemType is NavigationControlItemType.Location);
				insertIndex = lastItem is not null ? favoriteList.IndexOf(lastItem) + 1 : 0;
				favoriteList.Insert(insertIndex, locationItem);
			}

			DataChanged?.Invoke(SectionType.Favorites, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, locationItem, insertIndex));
		}

		/// <summary>
		/// Adds all items to the navigation sidebar
		/// </summary>
		public async Task AddAllItemsToSidebar()
		{
			if (userSettingsService.PreferencesSettingsService.ShowFavoritesSection)
				foreach (string path in FavoriteItems)
					await AddItemToSidebarAsync(path);
		}

		/// <summary>
		/// Removes stale items in the navigation sidebar
		/// </summary>
		public void RemoveStaleSidebarItems()
		{
			// Remove unpinned items from favoriteList
			foreach (var childItem in Favorites)
			{
				if (childItem is LocationItem item)
				{
					lock (favoriteList)
					{
						favoriteList.Remove(item);
					}
				}
			}

			// Remove unpinned items from sidebar
			DataChanged?.Invoke(SectionType.Favorites, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		public async void LoadAsync(object? sender, FileSystemEventArgs e)
			=> await LoadAsync();

		public async Task LoadAsync()
			=> await UpdateItemsWithExplorer();
	}
}
