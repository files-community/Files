using Files.Common;
using Files.Controllers;
using Files.DataModels.NavigationControlItems;
using Files.Extensions;
using Files.Filesystem;
using Files.Helpers;
using Files.UserControls;
using Files.ViewModels;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Files.DataModels
{
    public class SidebarPinnedModel
    {
        public static IList<IconFileInfo> IconResources = null;

        private SidebarPinnedController controller;

        private LocationItem favoriteSection, homeSection;

        [JsonIgnore]
        public SettingsViewModel AppSettings => App.AppSettings;

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
        }

        /// <summary>
        /// Adds the default items to the navigation page
        /// </summary>
        public void AddDefaultItems()
        {
            var udp = UserDataPaths.GetDefault();

            FavoriteItems.Add(AppSettings.DesktopPath);
            FavoriteItems.Add(AppSettings.DownloadsPath);
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
            if (!FavoriteItems.Contains(item))
            {
                FavoriteItems.Add(item);
                await AddItemToSidebarAsync(item);
                Save();
            }
        }

        public async Task ShowHideRecycleBinItemAsync(bool show)
        {
            await SidebarControl.SideBarItemsSemaphore.WaitAsync();
            try
            {
                if (show)
                {
                    var recycleBinItem = new LocationItem
                    {
                        Text = ApplicationData.Current.LocalSettings.Values.Get("RecycleBin_Title", "Recycle Bin"),
                        Font = Application.Current.Resources["RecycleBinIcons"] as FontFamily,
                        IsDefaultLocation = true,
                        Icon = UIHelpers.GetImageForIconOrNull(IconResources?.FirstOrDefault(x => x.Index == Constants.ImageRes.RecycleBin).Image),
                        Path = App.AppSettings.RecycleBinPath
                    };
                    // Add recycle bin to sidebar, title is read from LocalSettings (provided by the fulltrust process)
                    // TODO: the very first time the app is launched localized name not available
                    if (!favoriteSection.ChildItems.Any(x => x.Path == App.AppSettings.RecycleBinPath))
                    {
                        favoriteSection.ChildItems.Add(recycleBinItem);
                    }
                }
                else
                {
                    foreach (INavigationControlItem item in favoriteSection.ChildItems.ToList())
                    {
                        if (item is LocationItem && item.Path == App.AppSettings.RecycleBinPath)
                        {
                            favoriteSection.ChildItems.Remove(item);
                        }
                    }
                }
            }
            finally
            {
                SidebarControl.SideBarItemsSemaphore.Release();
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
        /// Moves the location item in the navigation sidebar from the old position to the new position
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

            if (oldIndex >= 0 && newIndex >= 0)
            {
                favoriteSection.ChildItems.RemoveAt(oldIndex);
                favoriteSection.ChildItems.Insert(newIndex, locationItem);
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

            // A backup of the items, because the swapping of items requires removing and inserting them in the correct position
            var sidebarItemsBackup = new List<string>(this.FavoriteItems);

            try
            {
                var indexOfFirstItemInMainPage = IndexOfItem(firstLocationItem);
                var indexOfSecondItemInMainPage = IndexOfItem(secondLocationItem);

                // Moves the items in the MainPage
                var result = MoveItem(firstLocationItem, indexOfFirstItemInMainPage, indexOfSecondItemInMainPage);

                // Moves the items in this model and saves the model
                if (result == true)
                {
                    var indexOfFirstItemInModel = this.FavoriteItems.IndexOf(firstLocationItem.Path);
                    var indexOfSecondItemInModel = this.FavoriteItems.IndexOf(secondLocationItem.Path);
                    if (indexOfFirstItemInModel >= 0 && indexOfSecondItemInModel >= 0)
                    {
                        this.FavoriteItems.RemoveAt(indexOfFirstItemInModel);
                        this.FavoriteItems.Insert(indexOfSecondItemInModel, firstLocationItem.Path);
                    }

                    Save();
                }
            }
            catch (Exception ex) when (
                ex is ArgumentException // Pinned item was invalid
                || ex is FileNotFoundException // Pinned item was deleted
                || ex is System.Runtime.InteropServices.COMException // Pinned item's drive was ejected
                || (uint)ex.HResult == 0x8007000F // The system cannot find the drive specified
                || (uint)ex.HResult == 0x800700A1) // The specified path is invalid (usually an mtp device was disconnected)
            {
                Debug.WriteLine($"An error occurred while swapping pinned items in the navigation sidebar. {ex.Message}");
                this.FavoriteItems = sidebarItemsBackup;
                this.RemoveStaleSidebarItems();
                _ = this.AddAllItemsToSidebar();
            }
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
                var lastItem = favoriteSection.ChildItems.LastOrDefault(x => x.ItemType == NavigationControlItemType.Location && !x.Path.Equals(App.AppSettings.RecycleBinPath));
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
                    using var thumbnail = await res.Result.GetThumbnailAsync(
                        Windows.Storage.FileProperties.ThumbnailMode.ListView,
                        24,
                        Windows.Storage.FileProperties.ThumbnailOptions.ResizeThumbnail);

                    if (thumbnail != null)
                    {
                        locationItem.IconData = await thumbnail.ToByteArrayAsync();
                        locationItem.Icon = await locationItem.IconData.ToBitmapAsync();
                    }
                }
                else
                {
                    locationItem.IconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(path, 24u);
                    if (locationItem.IconData != null)
                    {
                        locationItem.Icon = await locationItem.IconData.ToBitmapAsync();
                    }
                }

                if (!favoriteSection.ChildItems.Contains(locationItem))
                {
                    favoriteSection.ChildItems.Insert(insertIndex, locationItem);
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
            var lastItem = favoriteSection.ChildItems.LastOrDefault(x => x.ItemType == NavigationControlItemType.Location && !x.Path.Equals(App.AppSettings.RecycleBinPath));
            int insertIndex = lastItem != null ? favoriteSection.ChildItems.IndexOf(lastItem) + 1 : 0;

            if (!favoriteSection.ChildItems.Contains(section))
            {
                favoriteSection.ChildItems.Insert(insertIndex, section);
            }
        }

        /// <summary>
        /// Adds all items to the navigation sidebar
        /// </summary>
        public async Task AddAllItemsToSidebar()
        {
            await SidebarControl.SideBarItemsSemaphore.WaitAsync();

            if (IconResources is null)
            {
                IconResources = (await SidebarViewModel.LoadSidebarIconResources())?.ToList();
            }

            homeSection = new LocationItem()
            {
                Text = "SidebarHome".GetLocalized(),
                Section = SectionType.Home,
                Font = MainViewModel.FontName,
                IsDefaultLocation = true,
                Icon = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/FluentIcons/Home.png")),
                Path = "Home".GetLocalized(),
                ChildItems = new ObservableCollection<INavigationControlItem>()
            };
            favoriteSection = new LocationItem()
            {
                Text = "SidebarFavorites".GetLocalized(),
                Section = SectionType.Favorites,
                SelectsOnInvoked = false,
                Icon = UIHelpers.GetImageForIconOrNull(IconResources?.FirstOrDefault(x => x.Index == Constants.Shell32.QuickAccess).Image),
                Font = MainViewModel.FontName,
                ChildItems = new ObservableCollection<INavigationControlItem>()
            };
            try
            {
                SidebarControl.SideBarItems.BeginBulkOperation();

                if (homeSection != null)
                {
                    AddItemToSidebarAsync(homeSection);
                }

                for (int i = 0; i < FavoriteItems.Count(); i++)
                {
                    string path = FavoriteItems[i];
                    await AddItemToSidebarAsync(path);
                }

                if (!SidebarControl.SideBarItems.Contains(favoriteSection))
                {
                    SidebarControl.SideBarItems.Add(favoriteSection);
                }

                SidebarControl.SideBarItems.EndBulkOperation();
            }
            finally
            {
                SidebarControl.SideBarItemsSemaphore.Release();
            }

            await ShowHideRecycleBinItemAsync(App.AppSettings.PinRecycleBinToSideBar);
        }

        /// <summary>
        /// Removes stale items in the navigation sidebar
        /// </summary>
        public void RemoveStaleSidebarItems()
        {
            // Remove unpinned items from sidebar
            for (int i = 0; i < favoriteSection.ChildItems.Count(); i++)
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