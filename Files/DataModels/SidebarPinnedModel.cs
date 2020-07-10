using Files.Filesystem;
using Files.View_Models;
using Files.Views;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.DataModels
{
    public class SidebarPinnedModel
    {
        [JsonIgnore]
        public SettingsViewModel AppSettings => App.AppSettings;

        [JsonProperty("items")]
        public List<string> Items { get; set; } = new List<string>();

        public void AddDefaultItems()
        {
            Items.Add(AppSettings.DesktopPath);
            Items.Add(AppSettings.DownloadsPath);
            Items.Add(AppSettings.DocumentsPath);
            Items.Add(AppSettings.PicturesPath);
            Items.Add(AppSettings.MusicPath);
            Items.Add(AppSettings.VideosPath);
        }

        public List<string> GetItems()
        {
            return Items;
        }

        public async void AddItem(string item)
        {
            if (!Items.Contains(item))
            {
                Items.Add(item);
                await AddItemToSidebar(item);
                Save();
            }
        }

        public void RemoveItem(string item)
        {
            if (Items.Contains(item))
            {
                Items.Remove(item);
                RemoveStaleSidebarItems();
                Save();
            }
        }

        public void Save() => App.SidebarPinnedController.SaveModel();

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
                    Path = path,
                    Glyph = GetItemIcon(path),
                    IsDefaultLocation = false,
                    Text = folder.DisplayName
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
                || (uint)ex.HResult == 0x800700A1) // The specified path is invalid (usually an mtp device was disconnected)
            {
                Debug.WriteLine("Pinned item was invalid and will be removed from the file lines list soon: " + ex.Message);
                RemoveItem(path);
            }
        }

        public async void AddAllItemsToSidebar()
        {
            for (int i = 0; i < Items.Count(); i++)
            {
                string path = Items[i];
                await AddItemToSidebar(path);
            }
        }

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

        public string GetItemIcon(string path)
        {
            string iconCode;

            if (path.Equals(AppSettings.DesktopPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uE8FC";
            }
            else if (path.Equals(AppSettings.DownloadsPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uE896";
            }
            else if (path.Equals(AppSettings.DocumentsPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uE8A5";
            }
            else if (path.Equals(AppSettings.PicturesPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uEB9F";
            }
            else if (path.Equals(AppSettings.MusicPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uEC4F";
            }
            else if (path.Equals(AppSettings.VideosPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uE8B2";
            }
            else if (Path.GetPathRoot(path).Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uEDA2";
            }
            else
            {
                iconCode = "\uE8B7";
            }

            return iconCode;
        }
    }
}