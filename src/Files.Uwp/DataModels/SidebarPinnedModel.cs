using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services.Settings;
using Files.Uwp.Controllers;
using Files.Uwp.DataModels.NavigationControlItems;
using Files.Uwp.Filesystem;
using Files.Uwp.Helpers;
using Files.Uwp.ViewModels;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;

namespace Files.Uwp.DataModels
{
    public class SidebarPinnedModel
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        private SidebarPinnedController controller;

        private SemaphoreSlim addSyncSemaphore;

        [JsonIgnore]
        public MainViewModel MainViewModel => App.MainViewModel;

        [JsonProperty("items")]
        public List<string> FavoriteItems { get; set; } = new List<string>();

        private readonly List<INavigationControlItem> favoriteList = new List<INavigationControlItem>();

        [JsonIgnore]
        public IReadOnlyList<INavigationControlItem> Favorites
        {
            get
            {
                lock (favoriteList)
                {
                    return favoriteList.ToList().AsReadOnly();
                }
            }
        }

        public void SetController(SidebarPinnedController controller)
        {
            this.controller = controller;
        }

        public SidebarPinnedModel()
        {
            addSyncSemaphore = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Adds the default items to the navigation page
        /// </summary>
        public void AddDefaultItems()
        {
            var udp = UserDataPaths.GetDefault();

            FavoriteItems.Add(CommonPaths.DesktopPath);
            FavoriteItems.Add(CommonPaths.DownloadsPath);
            FavoriteItems.Add(udp.Documents);
        }

        /// <summary>
        /// Gets the items from the navigation page
        /// </summary>
        public List<string> GetItems()
        {
            return FavoriteItems;
        }

        /// <summary>
        /// Adds the item to the navigation page
        /// </summary>
        /// <param name="item">Item to remove</param>
        public async void AddItem(string item)
        {
            // add to `FavoriteItems` and `favoritesList` must be atomic
            await addSyncSemaphore.WaitAsync();

            try
            {
                if (!string.IsNullOrEmpty(item) && !FavoriteItems.Contains(item))
                {
                    FavoriteItems.Add(item);
                    await AddItemToSidebarAsync(item);
                    Save();

                    if (item == CommonPaths.RecycleBinPath)
                    {
                        UserSettingsService.AppearanceSettingsService.PinRecycleBinToSidebar = true;
                    }
                }
            }
            finally
            {
                addSyncSemaphore.Release();
            }
        }

        /// <summary>
        /// Removes the item from the navigation page
        /// </summary>
        /// <param name="item">Item to remove</param>
        public void RemoveItem(string item)
        {
            if (FavoriteItems.Contains(item))
            {
                FavoriteItems.Remove(item);
                RemoveStaleSidebarItems();
                Save();

                if (item == CommonPaths.RecycleBinPath)
                {
                    UserSettingsService.AppearanceSettingsService.PinRecycleBinToSidebar = false;
                }
            }
        }

        /// <summary>
        /// Moves the location item in the Favorites sidebar section from the old position to the new position
        /// </summary>
        /// <param name="locationItem">Location item to move</param>
        /// <param name="oldIndex">The old position index of the location item</param>
        /// <param name="newIndex">The new position index of the location item</param>
        /// <returns>True if the move was successful</returns>
        public bool MoveItem(INavigationControlItem locationItem, int oldIndex, int newIndex)
        {
            if (locationItem is null || newIndex > FavoriteItems.Count)
            {
                return false;
            }

            // A backup of the items, because the swapping of items requires removing and inserting them in the correct position
            var sidebarItemsBackup = new List<string>(FavoriteItems);

            try
            {
                FavoriteItems.RemoveAt(oldIndex);
                FavoriteItems.Insert(newIndex, locationItem.Path);
                lock (favoriteList)
                {
                    favoriteList.RemoveAt(oldIndex);
                    favoriteList.Insert(newIndex, locationItem);
                }
                var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, locationItem, newIndex, oldIndex);
                controller.DataChanged?.Invoke(SectionType.Favorites, e);
                Save();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred while moving pinned items in the Favorites sidebar section. {ex.Message}");
                FavoriteItems = sidebarItemsBackup;
                RemoveStaleSidebarItems();
                _ = AddAllItemsToSidebar();
                return false;
            }
        }

        /// <summary>
        /// Swaps two location items in the navigation sidebar
        /// </summary>
        /// <param name="firstLocationItem">The first location item</param>
        /// <param name="secondLocationItem">The second location item</param>
        public void SwapItems(INavigationControlItem firstLocationItem, INavigationControlItem secondLocationItem)
        {
            if (firstLocationItem == null || secondLocationItem == null)
            {
                return;
            }

            var indexOfFirstItemInMainPage = IndexOfItem(firstLocationItem);
            var indexOfSecondItemInMainPage = IndexOfItem(secondLocationItem);

            // Moves the items in the MainPage
            MoveItem(firstLocationItem, indexOfFirstItemInMainPage, indexOfSecondItemInMainPage);
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

        /// <summary>
        /// Returns the index of the location item in the collection containing Navigation control items
        /// </summary>
        /// <param name="locationItem">The location item</param>
        /// <param name="collection">The collection in which to find the location item</param>
        /// <returns>Index of the item</returns>
        public int IndexOfItem(INavigationControlItem locationItem, List<INavigationControlItem> collection)
        {
            return collection.IndexOf(locationItem);
        }

        public void ShowHideRecycleBinItem(bool show)
        {
            bool isPinned = favoriteList.Any(x => x.Path == CommonPaths.RecycleBinPath);

            if (show && !isPinned)
            {
                AddItem(CommonPaths.RecycleBinPath);
            }
            else if (!show && isPinned)
            {
                RemoveItem(CommonPaths.RecycleBinPath);
            }
        }

        /// <summary>
        /// Saves the model
        /// </summary>
        public void Save() => controller?.SaveModel();

        /// <summary>
        /// Adds the item (from a path) to the navigation sidebar
        /// </summary>
        /// <param name="path">The path which to save</param>
        /// <returns>Task</returns>
        public async Task AddItemToSidebarAsync(string path)
        {
            var item = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(path));
            var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path, item));
            var locationItem = new LocationItem
            {
                Font = MainViewModel.FontName,
                Path = path,
                Section = SectionType.Favorites,
                MenuOptions = new ContextMenuOptions
                {
                    IsLocationItem = true,
                    ShowProperties = true,
                    ShowUnpinItem = true,
                    ShowShellItems = true,
                    ShowEmptyRecycleBin = path == CommonPaths.RecycleBinPath,
                },
                IsDefaultLocation = false,
                Text = res.Result?.DisplayName ?? Path.GetFileName(path.TrimEnd('\\'))
            };

            if (res || (FilesystemResult)FolderHelpers.CheckFolderAccessWithWin32(path))
            {
                locationItem.IsInvalid = false;
                if (res)
                {
                    var iconData = await FileThumbnailHelper.LoadIconFromStorageItemAsync(res.Result, 24u, Windows.Storage.FileProperties.ThumbnailMode.ListView);
                    if (iconData == null)
                    {
                        iconData = await FileThumbnailHelper.LoadIconFromStorageItemAsync(res.Result, 24u, Windows.Storage.FileProperties.ThumbnailMode.SingleItem);
                    }
                    locationItem.IconData = iconData;
                    locationItem.Icon = await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => locationItem.IconData.ToBitmapAsync());
                }
                if (locationItem.IconData == null)
                {
                    locationItem.IconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(path, 24u);
                    if (locationItem.IconData != null)
                    {
                        locationItem.Icon = await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => locationItem.IconData.ToBitmapAsync());
                    }
                }
            }
            else
            {
                locationItem.Icon = await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => UIHelpers.GetIconResource(Constants.ImageRes.Folder));
                locationItem.IsInvalid = true;
                Debug.WriteLine($"Pinned item was invalid {res.ErrorCode}, item: {path}");
            }

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
                {
                    return;
                }
                var lastItem = favoriteList.LastOrDefault(x => x.ItemType is NavigationControlItemType.Location);
                insertIndex = lastItem is not null ? favoriteList.IndexOf(lastItem) + 1 : 0;
                favoriteList.Insert(insertIndex, locationItem);
            }
            controller.DataChanged?.Invoke(SectionType.Favorites, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, locationItem, insertIndex));
        }

        /// <summary>
        /// Adds all items to the navigation sidebar
        /// </summary>
        public async Task AddAllItemsToSidebar()
        {
            if (UserSettingsService.AppearanceSettingsService.ShowFavoritesSection)
            {
                foreach (string path in FavoriteItems)
                {
                    await AddItemToSidebarAsync(path);
                }
                ShowHideRecycleBinItem(UserSettingsService.AppearanceSettingsService.PinRecycleBinToSidebar);
            }
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
                    if (!item.IsDefaultLocation && !FavoriteItems.Contains(item.Path))
                    {
                        lock (favoriteList)
                        {
                            favoriteList.Remove(item);
                        }
                        controller.DataChanged?.Invoke(SectionType.Favorites, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
                    }
                }
            }

            // Remove unpinned items from sidebar
            controller.DataChanged?.Invoke(SectionType.Favorites, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}