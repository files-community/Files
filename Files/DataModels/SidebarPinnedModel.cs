using Files.Enums;
using Files.Filesystem;
using Files.View_Models;
using Files.Views;
using Microsoft.Toolkit.Uwp.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media;

namespace Files.DataModels
{
    public class SidebarPinnedModel
    {
        [JsonIgnore]
        public SettingsViewModel AppSettings => App.AppSettings;

        [JsonProperty("items")]
        public List<string> Items { get; set; } = new List<string>();

        private SidebarSortOption _sidebarSortOption;

        /// <summary>
        /// The sort option for the sidebar items
        /// </summary>
        public SidebarSortOption SidebarSortOption
        {
            get => (SidebarSortOption)_sidebarSortOption;
            set
            {
                _sidebarSortOption = value;
                SortItemsAsync();
            }
        }

        private SortDirection _sidebarSortDirection;

        /// <summary>
        /// The sort direction for the sidebar items
        /// </summary>
        public SortDirection SidebarSortDirection
        {
            get => (SortDirection)_sidebarSortDirection;
            set
            {
                _sidebarSortDirection = value;
                SortItemsAsync();
            }
        }

        /// <summary>
        /// Adds the default items to the navigation page
        /// </summary>
        public void AddDefaultItems()
        {
            Items.Add(AppSettings.DesktopPath);
            Items.Add(AppSettings.DownloadsPath);
            Items.Add(AppSettings.DocumentsPath);
            Items.Add(AppSettings.PicturesPath);
            Items.Add(AppSettings.MusicPath);
            Items.Add(AppSettings.VideosPath);
        }

        /// <summary>
        /// Gets the item sfrom the navigation page
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
                await AddItemToSidebar(item);
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
        /// Moves the location item to the index position
        /// </summary>
        /// <param name="item">Location item text to move</param>
        /// <param name="newIndex">New index for the location item</param>
        public void MoveItem(string item, int newIndex)
        {
            var locationItem = MainPage.sideBarItems.FirstOrDefault(x => x.Text == item);
            MoveItem(locationItem, newIndex);
        }

        /// <summary>
        /// Moves the location item to the index position
        /// </summary>
        /// <param name="locationItem">Location item to move</param>
        /// <param name="newIndex">New index for the location item</param>
        public void MoveItem(INavigationControlItem locationItem, int newIndex)
        {
            if (locationItem == null || newIndex < 0)
            {
                return;
            }

            var oldIndex = IndexOfItem(locationItem);
            if (oldIndex >= 0)
            {
                MainPage.sideBarItems.RemoveAt(oldIndex);
                MainPage.sideBarItems.Insert(newIndex, locationItem);
                Save();
            }
        }

        /// <summary>
        /// Sorts all items in the navigation bar async
        /// </summary>
        /// <returns>Task</returns>
        public async Task SortItemsAsync()
        {
            await SortSidebarLocationItems();
        }

        /// <summary>
        /// Sorts the location items in the navigation bar
        /// </summary>
        /// <returns>Task</returns>
        public async Task SortSidebarLocationItems()
        {
            static object orderByFunc(LocationItem item) => item;
            Func<LocationItem, object> orderFunc = orderByFunc;
            IOrderedEnumerable<LocationItem> ordered;
            List<LocationItem> orderedList;

            switch (this.SidebarSortOption)
            {
                case Enums.SidebarSortOption.Name:
                    orderFunc = item => item.Text;
                    break;

                case Enums.SidebarSortOption.DateAdded:
                    orderFunc = item => item.DateAdded;
                    break;

                case Enums.SidebarSortOption.Custom:
                    orderFunc = item => item;
                    break;
            }

            var originalSideBarLocationItems = new List<LocationItem>();
            var originalSideBarItems = MainPage.sideBarItems.ToList();

            // Selects only the location items from the navigation bar
            for (var i = 0; i < MainPage.sideBarItems.Count; i++)
            {
                var sideBarItem = MainPage.sideBarItems[i];
                if (sideBarItem is LocationItem locationItem && locationItem.IsDefaultLocation == false)
                {
                    originalSideBarLocationItems.Add(locationItem);
                }
            }

            if(originalSideBarLocationItems.Count <= 0)
            {
                return;
            }

            var sideBarItemsToOrder = new List<LocationItem>(originalSideBarLocationItems);

            // Orders the location items from the navigation bar with the sort option and direction

            if (this.SidebarSortDirection == SortDirection.Ascending)
            {
                ordered = sideBarItemsToOrder.OrderBy(orderFunc);
            }
            else
            {
                ordered = sideBarItemsToOrder.OrderByDescending(orderFunc);
            }

            orderedList = ordered.ToList();

            // Swaps the location items in the navigation bar
            for (var i = 0; i < originalSideBarLocationItems.Count; i++)
            {
                var locationItem = originalSideBarLocationItems[i];

                var index = IndexOfItem(locationItem, originalSideBarItems);
                if (index >= 0)
                {
                    MainPage.sideBarItems.RemoveAt(index);
                    MainPage.sideBarItems.Insert(index, orderedList[i]);
                }
            }

            Save();
        }

        /// <summary>
        /// Returns the index of the location item in the navigation sidebar
        /// </summary>
        /// <param name="locationItem">The location item</param>
        /// <returns>Index of the item</returns>
        public int IndexOfItem(INavigationControlItem locationItem)
        {
            return MainPage.sideBarItems.IndexOf(locationItem);

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

        public void Save() => App.SidebarPinnedController.SaveModel();

        /// <summary>
        /// Adds the item do the navigation sidebar
        /// </summary>
        /// <param name="path">The path which to save</param>
        /// <returns>Task</returns>
        public async Task AddItemToSidebar(string path)
        {
            try
            {
                var item = await DrivesManager.GetRootFromPath(path);
                StorageFolder folder = await StorageFileExtensions.GetFolderFromPathAsync(path, item);
                int insertIndex = MainPage.sideBarItems.IndexOf(MainPage.sideBarItems.Last(x => x.ItemType == NavigationControlItemType.Location
                    && !x.Path.Equals(App.AppSettings.RecycleBinPath))) + 1;
                var locationItem = new LocationItem
                {
                    Font = App.Current.Resources["FluentUIGlyphs"] as FontFamily,
                    Path = path,
                    Glyph = GetItemIcon(path),
                    IsDefaultLocation = false,
                    Text = folder.DisplayName,
                    DateAdded = DateTime.UtcNow
                };
                MainPage.sideBarItems.Insert(insertIndex, locationItem);
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine(ex.Message);
            }
            catch (Exception ex) when (
                ex is ArgumentException // Pinned item was invalid
                || ex is FileNotFoundException // Pinned item was deleted
                || ex is System.Runtime.InteropServices.COMException // Pinned item's drive was ejected
                || (uint)ex.HResult == 0x8007000F // The system cannot find the drive specified
                || (uint)ex.HResult == 0x800700A1) // The specified path is invalid (usually an mtp device was disconnected)
            {
                Debug.WriteLine("Pinned item was invalid and will be removed from the file lines list soon: " + ex.Message);
                RemoveItem(path);
            }
        }

        /// <summary>
        /// Adds all items to the navigation sidebar
        /// </summary>
        public async void AddAllItemsToSidebar()
        {
            for (int i = 0; i < Items.Count(); i++)
            {
                string path = Items[i];
                await AddItemToSidebar(path);
            }
        }

        /// <summary>
        /// Removes stale items in the navigation sidebar
        /// </summary>
        public void RemoveStaleSidebarItems()
        {
            // Remove unpinned items from sidebar
            for (int i = 0; i < MainPage.sideBarItems.Count(); i++)
            {
                if (MainPage.sideBarItems[i] is LocationItem)
                {
                    var item = MainPage.sideBarItems[i] as LocationItem;
                    if (!item.IsDefaultLocation && !Items.Contains(item.Path))
                    {
                        MainPage.sideBarItems.RemoveAt(i);
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