using Files.Common;
using Files.Controllers;
using Files.DataModels.NavigationControlItems;
using Files.Filesystem;
using Files.Helpers;
using Files.Services;
using Files.UserControls;
using Files.ViewModels;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.DataModels
{
    public class SidebarPinnedModel
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        private SidebarPinnedController controller;

        private LocationItem favoriteSection;

        [JsonIgnore]
        public MainViewModel MainViewModel => App.MainViewModel;

        [JsonProperty("items")]
        public List<string> FavoriteItems { get; set; } = new List<string>();

        public void SetController(SidebarPinnedController controller)
        {
            this.controller = controller;
        }

        public SidebarPinnedModel()
        {
            favoriteSection = SidebarControl.SideBarItems.FirstOrDefault(x => x.Text == "SidebarFavorites".GetLocalized()) as LocationItem;
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

        private void RemoveFavoritesSideBarSection()
        {
            try
            {
                var item = (from n in SidebarControl.SideBarItems where n.Text.Equals("SidebarFavorites".GetLocalized()) select n).FirstOrDefault();
                if (!UserSettingsService.AppearanceSettingsService.ShowFavoritesSection && item != null)
                {
                    SidebarControl.SideBarItems.Remove(item);
                }
            }
            catch (Exception)
            { }
        }

        public async void UpdateFavoritesSectionVisibility()
        {
            if (UserSettingsService.AppearanceSettingsService.ShowFavoritesSection)
            {
                await AddAllItemsToSidebar();
            }
            else
            {
                RemoveFavoritesSideBarSection();
            }
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
                    Icon = await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => UIHelpers.GetIconResource(Constants.ImageRes.RecycleBin)),
                    Path = CommonPaths.RecycleBinPath
                };
                // Add recycle bin to sidebar, title is read from LocalSettings (provided by the fulltrust process)
                // TODO: the very first time the app is launched localized name not available
                if (!favoriteSection.ChildItems.Any(x => x.Path == CommonPaths.RecycleBinPath))
                {
                    await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => favoriteSection.ChildItems.Add(recycleBinItem));
                }
            }
            else
            {
                foreach (INavigationControlItem item in favoriteSection.ChildItems.ToList())
                {
                    if (item is LocationItem && item.Path == CommonPaths.RecycleBinPath)
                    {
                        await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => favoriteSection.ChildItems.Remove(item));
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
                    favoriteSection.ChildItems.RemoveAt(oldIndex);
                    favoriteSection.ChildItems.Insert(newIndex, locationItem);
                    Save();
                }
                catch (Exception ex) when (
                    ex is ArgumentException // Pinned item was invalid
                    || ex is FileNotFoundException // Pinned item was deleted
                    || ex is System.Runtime.InteropServices.COMException // Pinned item's drive was ejected
                    || (uint)ex.HResult == 0x8007000F // The system cannot find the drive specified
                    || (uint)ex.HResult == 0x800700A1) // The specified path is invalid (usually an mtp device was disconnected)
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
            return favoriteSection.ChildItems.IndexOf(locationItem);
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
            if (res || (FilesystemResult)FolderHelpers.CheckFolderAccessWithWin32(path))
            {
                var lastItem = favoriteSection.ChildItems.LastOrDefault(x => x.ItemType == NavigationControlItemType.Location && !x.Path.Equals(CommonPaths.RecycleBinPath));
                int insertIndex = lastItem != null ? favoriteSection.ChildItems.IndexOf(lastItem) + 1 : 0;
                var locationItem = new LocationItem
                {
                    Font = MainViewModel.FontName,
                    Path = path,
                    Section = SectionType.Favorites,
                    IsDefaultLocation = false,
                    Text = res.Result?.DisplayName ?? Path.GetFileName(path.TrimEnd('\\'))
                };

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

                if (!favoriteSection.ChildItems.Any(x => x.Path == locationItem.Path))
                {
                    await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => favoriteSection.ChildItems.Insert(insertIndex, locationItem));
                }
            }
            else
            {
                Debug.WriteLine($"Pinned item was invalid and will be removed from the file lines list soon: {res.ErrorCode}");
                RemoveItem(path);
            }
        }

        /// <summary>
        /// Adds the item to sidebar asynchronous.
        /// </summary>
        /// <param name="section">The section.</param>
        private void AddItemToSidebarAsync(LocationItem section)
        {
            var lastItem = favoriteSection.ChildItems.LastOrDefault(x => x.ItemType == NavigationControlItemType.Location && !x.Path.Equals(CommonPaths.RecycleBinPath));
            int insertIndex = lastItem != null ? favoriteSection.ChildItems.IndexOf(lastItem) + 1 : 0;

            if (!favoriteSection.ChildItems.Any(x => x.Section == section.Section))
            {
                favoriteSection.ChildItems.Insert(insertIndex, section);
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

            await SidebarControl.SideBarItemsSemaphore.WaitAsync();
            try
            {
                var homeSection = new LocationItem()
                {
                    Text = "SidebarHome".GetLocalized(),
                    Section = SectionType.Home,
                    Font = MainViewModel.FontName,
                    IsDefaultLocation = true,
                    Icon = await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => new BitmapImage(new Uri("ms-appx:///Assets/FluentIcons/Home.png"))),
                    Path = "Home".GetLocalized(),
                    ChildItems = new ObservableCollection<INavigationControlItem>()
                };
                favoriteSection ??= new LocationItem()
                {
                    Text = "SidebarFavorites".GetLocalized(),
                    Section = SectionType.Favorites,
                    SelectsOnInvoked = false,
                    Icon = await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => UIHelpers.GetIconResource(Constants.Shell32.QuickAccess)),
                    Font = MainViewModel.FontName,
                    ChildItems = new ObservableCollection<INavigationControlItem>()
                };

                if (homeSection != null)
                {
                    AddItemToSidebarAsync(homeSection);
                }

                if (!SidebarControl.SideBarItems.Any(x => x.Text == "SidebarFavorites".GetLocalized()))
                {
                    SidebarControl.SideBarItems.BeginBulkOperation();
                    var index = 0; // First section
                    SidebarControl.SideBarItems.Insert(index, favoriteSection);
                    await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() => SidebarControl.SideBarItems.EndBulkOperation());
                }
            }
            finally
            {
                SidebarControl.SideBarItemsSemaphore.Release();
            }

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
            // Remove unpinned items from sidebar
            for (int i = 0; i < favoriteSection.ChildItems.Count; i++)
            {
                if (favoriteSection.ChildItems[i] is LocationItem)
                {
                    var item = favoriteSection.ChildItems[i] as LocationItem;
                    if (!item.IsDefaultLocation && !FavoriteItems.Contains(item.Path))
                    {
                        favoriteSection.ChildItems.RemoveAt(i);
                    }
                }
            }
        }
    }
}