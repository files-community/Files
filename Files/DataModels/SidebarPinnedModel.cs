using Files.Filesystem;
using Files.UserControls;
using Files.ViewModels;
using Files.Views;
using Microsoft.Toolkit.Uwp.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;

namespace Files.DataModels
{
    public class SidebarPinnedModel
    {
        [JsonIgnore]
        public SettingsViewModel AppSettings => App.AppSettings;

        [JsonProperty("items")]
        public List<string> Items { get; set; } = new List<string>();

        /// <summary>
        /// Adds the default items to the navigation page
        /// </summary>
        public void AddDefaultItems()
        {
            //Items.Add(AppSettings.DesktopPath);
            Items.Add(AppSettings.DownloadsPath);
            Items.Add(AppSettings.DocumentsPath);
            Items.Add(AppSettings.PicturesPath);
            //Items.Add(AppSettings.MusicPath);
            //Items.Add(AppSettings.VideosPath);
        }

        /// <summary>
        /// Gets the items from the navigation page
        /// </summary>
        public List<string> GetItems()
        {
            return Items;
        }

        /// <summary>
        /// Adds the item to the navigation page
        /// </summary>
        /// <param name="item">Item to remove</param>
        public async void AddItem(string item)
        {
            if (!Items.Contains(item))
            {
                Items.Add(item);
                await AddItemToSidebarAsync(item);
                Save();
            }
        }

        /// <summary>
        /// Removes the item from the navigation page
        /// </summary>
        /// <param name="item">Item to remove</param>
        public void RemoveItem(string item)
        {
            if (Items.Contains(item))
            {
                Items.Remove(item);
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
                SidebarControl.Items.RemoveAt(oldIndex);
                SidebarControl.Items.Insert(newIndex, locationItem);
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

            // A backup of the items, because the swapping of items requires removing and inserting them in the corrent position
            var sidebarItemsBackup = new List<string>(this.Items);

            try
            {
                var indexOfFirstItemInMainPage = IndexOfItem(firstLocationItem);
                var indexOfSecondItemInMainPage = IndexOfItem(secondLocationItem);

                // Moves the items in the MainPage
                var result = MoveItem(firstLocationItem, indexOfFirstItemInMainPage, indexOfSecondItemInMainPage);

                // Moves the items in this model and saves the model
                if (result == true)
                {
                    var indexOfFirstItemInModel = this.Items.IndexOf(firstLocationItem.Path);
                    var indexOfSecondItemInModel = this.Items.IndexOf(secondLocationItem.Path);
                    if (indexOfFirstItemInModel >= 0 && indexOfSecondItemInModel >= 0)
                    {
                        this.Items.RemoveAt(indexOfFirstItemInModel);
                        this.Items.Insert(indexOfSecondItemInModel, firstLocationItem.Path);
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
                Debug.WriteLine($"An error occured while swapping pinned items in the navigation sidebar. {ex.Message}");
                this.Items = sidebarItemsBackup;
                this.RemoveStaleSidebarItems();
                this.AddAllItemsToSidebar();
            }
        }

        /// <summary>
        /// Returns the index of the location item in the navigation sidebar
        /// </summary>
        /// <param name="locationItem">The location item</param>
        /// <returns>Index of the item</returns>
        public int IndexOfItem(INavigationControlItem locationItem)
        {
            return SidebarControl.Items.IndexOf(locationItem);
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
        public void Save() => App.SidebarPinnedController.SaveModel();

        /// <summary>
        /// Adds the item do the navigation sidebar
        /// </summary>
        /// <param name="path">The path which to save</param>
        /// <returns>Task</returns>
        public async Task AddItemToSidebarAsync(string path)
        {
            var item = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(path));
            var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path, item));
            if (res)
            {
                int insertIndex = SidebarControl.Items.IndexOf(SidebarControl.Items.Last(x => x.ItemType == NavigationControlItemType.Location
                && !x.Path.Equals(App.AppSettings.RecycleBinPath))) + 1;
                var locationItem = new LocationItem
                {
                    Font = App.Current.Resources["FluentUIGlyphs"] as FontFamily,
                    Path = path,
                    Glyph = GetItemIcon(path),
                    IsDefaultLocation = false,
                    Text = res.Result.DisplayName
                };

                if (!SidebarControl.Items.Contains(locationItem))
                {
                    SidebarControl.Items.Insert(insertIndex, locationItem);
                }
            }
            else
            {
                Debug.WriteLine($"Pinned item was invalid and will be removed from the file lines list soon: {res.ErrorCode}");
                RemoveItem(path);
            }
        }

        /// <summary>
        /// Adds all items to the navigation sidebar
        /// </summary>
        public async void AddAllItemsToSidebar()
        {
            // Add pinned items first
            for (int i = 0; i < Items.Count(); i++)
            {
                string path = Items[i];
                await AddItemToSidebarAsync(path);
            }

            // Now, add section items
            SidebarControl.Items.Add(new HeaderItem()
            {
                Font = App.Current.Resources["FluentUIGlyphs"] as FontFamily,
                //    Glyph = GetItemIcon(path),
                IsDefaultLocation = true,
                IsItemExpanded = false,
                Text = "SidebarThisDevice".GetLocalized(),
                HeaderType = HeaderItem.HeaderItemType.ThisDevice,
                MenuItems = new System.Collections.ObjectModel.ObservableCollection<INavigationControlItem>()
                {
                    new LocationItem()
                    {
                        IsDefaultLocation = true, 
                        Font = App.Current.Resources["FluentUIGlyphs"] as FontFamily,
                        Glyph = GetItemIcon(AppSettings.DesktopPath),
                        Path = AppSettings.DesktopPath,
                        Text = "SidebarDesktop".GetLocalized()
                    },
                    new LocationItem()
                    {
                        IsDefaultLocation = true,
                        Font = App.Current.Resources["FluentUIGlyphs"] as FontFamily,
                        Glyph = GetItemIcon(AppSettings.DownloadsPath),
                        Path = AppSettings.DownloadsPath,
                        Text = "SidebarDownloads".GetLocalized()
                    },
                    new LocationItem()
                    {
                        IsDefaultLocation = true,
                        Font = App.Current.Resources["FluentUIGlyphs"] as FontFamily,
                        Glyph = GetItemIcon(AppSettings.DocumentsPath),
                        Path = AppSettings.DocumentsPath,
                        Text = "SidebarDocuments".GetLocalized()
                    },
                    new LocationItem()
                    {
                        IsDefaultLocation = true,
                        Font = App.Current.Resources["FluentUIGlyphs"] as FontFamily,
                        Glyph = GetItemIcon(AppSettings.PicturesPath),
                        Path = AppSettings.PicturesPath,
                        Text = "SidebarPictures".GetLocalized()
                    },
                    new LocationItem()
                    {
                        IsDefaultLocation = true,
                        Font = App.Current.Resources["FluentUIGlyphs"] as FontFamily,
                        Glyph = GetItemIcon(AppSettings.MusicPath),
                        Path = AppSettings.MusicPath,
                        Text = "SidebarMusic".GetLocalized()
                    },
                    new LocationItem()
                    {
                        IsDefaultLocation = true,
                        Font = App.Current.Resources["FluentUIGlyphs"] as FontFamily,
                        Glyph = GetItemIcon(AppSettings.VideosPath),
                        Path = AppSettings.VideosPath,
                        Text = "SidebarVideos".GetLocalized()
                    },
                    new LocationItem
                    {
                        Text = "SidebarRecycleBin".GetLocalized(),
                        Font = App.Current.Resources["RecycleBinIcons"] as FontFamily,
                        Glyph = "\uEF87",
                        IsDefaultLocation = true,
                        Path = AppSettings.RecycleBinPath
                    }
                }
            });

            SidebarControl.Items.Add(new HeaderItem()
            {
                Font = App.Current.Resources["FluentUIGlyphs"] as FontFamily,
                //    Glyph = GetItemIcon(path),
                IsDefaultLocation = true,
                IsItemExpanded = AppSettings.DrivesManager.Drives.Count <= 5,
                Text = "SidebarDrives".GetLocalized(),
                HeaderType = HeaderItem.HeaderItemType.Drives,
                MenuItems = (System.Collections.ObjectModel.ObservableCollection<INavigationControlItem>)AppSettings.DrivesManager.Drives.Where(x => x.Type != Filesystem.DriveType.Network)
            });

            if (AppSettings.CloudDrivesManager.Drives.Any())
            {
                SidebarControl.Items.Add(new HeaderItem()
                {
                    Font = App.Current.Resources["FluentUIGlyphs"] as FontFamily,
                    //    Glyph = GetItemIcon(path),
                    IsDefaultLocation = true,
                    IsItemExpanded = true,
                    Text = "SidebarCloudDrives".GetLocalized(),
                    HeaderType = HeaderItem.HeaderItemType.Cloud,
                    MenuItems = (System.Collections.ObjectModel.ObservableCollection<INavigationControlItem>)AppSettings.CloudDrivesManager.Drives
                });
            }

            if (AppSettings.DrivesManager.Drives.Any(x => x.Type == Filesystem.DriveType.Network))
            {
                SidebarControl.Items.Add(new HeaderItem()
                {
                    Font = App.Current.Resources["FluentUIGlyphs"] as FontFamily,
                    //    Glyph = GetItemIcon(path),
                    IsDefaultLocation = true,
                    IsItemExpanded = false,
                    Text = "SidebarNetwork".GetLocalized(),
                    HeaderType = HeaderItem.HeaderItemType.Network,
                    MenuItems = (System.Collections.ObjectModel.ObservableCollection<INavigationControlItem>)AppSettings.DrivesManager.Drives.Where(x => x.Type == Filesystem.DriveType.Network)
                });
            }
        }

        /// <summary>
        /// Removes stale items in the navigation sidebar
        /// </summary>
        public void RemoveStaleSidebarItems()
        {
            // Remove unpinned items from sidebar
            for (int i = 0; i < SidebarControl.Items.Count(); i++)
            {
                if (SidebarControl.Items[i] is LocationItem)
                {
                    var item = SidebarControl.Items[i] as LocationItem;
                    if (!item.IsDefaultLocation && !Items.Contains(item.Path))
                    {
                        SidebarControl.Items.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the icon for the items in the navigation sidebar
        /// </summary>
        /// <param name="path">The path in the sidebar</param>
        /// <returns>The icon code</returns>
        public string GetItemIcon(string path)
        {
            string iconCode;

            if (path.Equals(AppSettings.DesktopPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\ue9f1";
            }
            else if (path.Equals(AppSettings.DownloadsPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uE91c";
            }
            else if (path.Equals(AppSettings.DocumentsPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uea11";
            }
            else if (path.Equals(AppSettings.PicturesPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uea83";
            }
            else if (path.Equals(AppSettings.MusicPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uead4";
            }
            else if (path.Equals(AppSettings.VideosPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uec0d";
            }
            else if (Path.GetPathRoot(path).Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\ueb8b";
            }
            else
            {
                iconCode = "\uea55";
            }

            return iconCode;
        }
    }
}