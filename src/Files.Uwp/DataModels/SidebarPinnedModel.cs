using Files.Shared.Extensions;
using Files.Uwp.Controllers;
using Files.Uwp.DataModels.NavigationControlItems;
using Files.Uwp.Filesystem;
using Files.Uwp.Helpers;
using Files.Backend.Services.Settings;
using Files.Uwp.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using System.Collections.Specialized;

namespace Files.Uwp.DataModels
{
    public class SidebarPinnedModel
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        private SidebarPinnedController controller;

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
            if (!string.IsNullOrEmpty(item) && !FavoriteItems.Contains(item))
            {
                FavoriteItems.Add(item);
                await AddItemToSidebarAsync(item);
                Save();
            }
        }

        public async Task ShowHideRecycleBinItemAsync(bool show)
        {
            if (show)
            {
                var recycleBinItem = new LocationItem
                {
                    Text = ApplicationData.Current.LocalSettings.Values.Get("RecycleBin_Title", "Recycle Bin"),
                    IsDefaultLocation = true,
                    MenuOptions = new ContextMenuOptions
                    {
                        IsLocationItem = true,
                        ShowUnpinItem = true,
                        ShowShellItems = true,
                        ShowEmptyRecycleBin = true
                    },
                    GetIconData = async () => (await UIHelpers.GetIconResourceInfo(Constants.ImageRes.RecycleBin))?.IconDataBytes,
                    Path = CommonPaths.RecycleBinPath
                };
                // Add recycle bin to sidebar, title is read from LocalSettings (provided by the fulltrust process)
                // TODO: the very first time the app is launched localized name not available
                if (!favoriteList.Any(x => x.Path == CommonPaths.RecycleBinPath))
                {
                    favoriteList.Add(recycleBinItem);
                    controller.DataChanged?.Invoke(SectionType.Favorites, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, recycleBinItem));
                }
            }
            else
            {
                foreach (INavigationControlItem item in favoriteList.ToList())
                {
                    if (item is LocationItem && item.Path == CommonPaths.RecycleBinPath)
                    {
                        favoriteList.Remove(item);
                        controller.DataChanged?.Invoke(SectionType.Favorites, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
                    }
                }
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
            if (locationItem == null)
            {
                return false;
            }

            if (oldIndex >= 1 && newIndex >= 1 && newIndex <= FavoriteItems.Count)
            {
                // A backup of the items, because the swapping of items requires removing and inserting them in the correct position
                var sidebarItemsBackup = new List<string>(FavoriteItems);

                try
                {
                    FavoriteItems.RemoveAt(oldIndex - 1);
                    FavoriteItems.Insert(newIndex - 1, locationItem.Path);
                    favoriteList.RemoveAt(oldIndex);
                    favoriteList.Insert(newIndex, locationItem);
                    controller.DataChanged?.Invoke(SectionType.Favorites, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, locationItem, newIndex, oldIndex));
                    Save();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"An error occurred while moving pinned items in the Favorites sidebar section. {ex.Message}");
                    FavoriteItems = sidebarItemsBackup;
                    RemoveStaleSidebarItems();
                    _ = AddAllItemsToSidebar();
                    return false;
                }

                return true;
            }

            return false;
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
            return favoriteList.FindIndex(x => x.Path == locationItem.Path);
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

        /// <summary>
        /// Saves the model
        /// </summary>
        public void Save() => controller?.SaveModel();

        /// <summary>
        /// Adds the item do the navigation sidebar
        /// </summary>
        /// <param name="path">The path which to save</param>
        /// <returns>Task</returns>
        public async Task AddItemToSidebarAsync(string path)
        {
            var item = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(path));
            var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path, item));
            var lastItem = favoriteList.LastOrDefault(x => x.ItemType == NavigationControlItemType.Location && !x.Path.Equals(CommonPaths.RecycleBinPath));
            int insertIndex = lastItem != null ? favoriteList.IndexOf(lastItem) + 1 : 0;
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
                    IsItemMovable = true
                },
                IsDefaultLocation = false,
                Text = res.Result?.DisplayName ?? Path.GetFileName(path.TrimEnd('\\'))
            };

            if (res || (FilesystemResult)FolderHelpers.CheckFolderAccessWithWin32(path))
            {
                locationItem.IsInvalid = false;
                locationItem.GetIconData = async () =>
                {
                    byte[] iconData = null;
                    if (res)
                    {
                        iconData = await FileThumbnailHelper.LoadIconFromStorageItemAsync(res.Result, 24u, Windows.Storage.FileProperties.ThumbnailMode.ListView);
                        if (iconData == null)
                        {
                            iconData = await FileThumbnailHelper.LoadIconFromStorageItemAsync(res.Result, 24u, Windows.Storage.FileProperties.ThumbnailMode.SingleItem);
                        }
                    }
                    if (iconData == null)
                    {
                        iconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(path, 24u);
                    }
                    return iconData;
                };
            }
            else
            {
                locationItem.GetIconData = async () => (await UIHelpers.GetIconResourceInfo(Constants.ImageRes.Folder))?.IconDataBytes;
                locationItem.IsInvalid = true;
                Debug.WriteLine($"Pinned item was invalid {res.ErrorCode}, item: {path}");
            }

            if (!favoriteList.Any(x => x.Path == locationItem.Path))
            {
                favoriteList.Insert(insertIndex, locationItem);
                controller.DataChanged?.Invoke(SectionType.Favorites, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, locationItem, insertIndex));
            }
        }

        /// <summary>
        /// Adds the item to sidebar asynchronous.
        /// </summary>
        /// <param name="section">The section.</param>
        private void AddLocationItemToSidebar(LocationItem section)
        {
            var lastItem = favoriteList.LastOrDefault(x => x.ItemType == NavigationControlItemType.Location && !x.Path.Equals(CommonPaths.RecycleBinPath));
            int insertIndex = lastItem != null ? favoriteList.IndexOf(lastItem) + 1 : 0;

            if (!favoriteList.Any(x => x.Section == section.Section))
            {
                favoriteList.Insert(insertIndex, section);
                controller.DataChanged?.Invoke(SectionType.Favorites, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, section, insertIndex));
            }
        }

        /// <summary>
        /// Adds all items to the navigation sidebar
        /// </summary>
        public async Task AddAllItemsToSidebar()
        {
            if (!UserSettingsService.AppearanceSettingsService.ShowFavoritesSection)
            {
                return;
            }

            var homeSection = new LocationItem()
            {
                Text = "Home".GetLocalized(),
                Section = SectionType.Home,
                MenuOptions = new ContextMenuOptions
                {
                    IsLocationItem = true
                },
                Font = MainViewModel.FontName,
                IsDefaultLocation = true,
                IconSource = new Uri("ms-appx:///Assets/FluentIcons/Home.png"),
                Path = "Home".GetLocalized()
            };
            AddLocationItemToSidebar(homeSection);

            for (int i = 0; i < FavoriteItems.Count; i++)
            {
                string path = FavoriteItems[i];
                await AddItemToSidebarAsync(path);
            }

            await ShowHideRecycleBinItemAsync(UserSettingsService.AppearanceSettingsService.PinRecycleBinToSidebar);
        }

        /// <summary>
        /// Removes stale items in the navigation sidebar
        /// </summary>
        public void RemoveStaleSidebarItems()
        {
            // Remove unpinned items from favoriteList
            // Reverse iteration to avoid skipping elements while removing
            for (int i = favoriteList.Count - 1; i >= 0; i--)
            {
                var childItem = favoriteList[i];
                if (childItem is LocationItem item)
                {
                    if (!item.IsDefaultLocation && !FavoriteItems.Contains(item.Path))
                    {
                        favoriteList.RemoveAt(i);
                        controller.DataChanged?.Invoke(SectionType.Favorites, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
                    }
                }
            }

            // Remove unpinned items from sidebar
            controller.DataChanged?.Invoke(SectionType.Favorites, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}