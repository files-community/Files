using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Files.Filesystem;
using Newtonsoft.Json;
using Windows.Storage;

namespace Files.DataModels
{
    public class SidebarPinnedModel
    {
        public static readonly string JsonFileName = "PinnedItems.json";

        [JsonProperty("items")]
        public List<string> Items { get; set; } = new List<string>();

        public void AddDefaultItems()
        {
            Items.Add(App.AppSettings.DesktopPath);
            Items.Add(App.AppSettings.DownloadsPath);
            Items.Add(App.AppSettings.DocumentsPath);
            Items.Add(App.AppSettings.PicturesPath);
            Items.Add(App.AppSettings.MusicPath);
            Items.Add(App.AppSettings.VideosPath);
        }

        public List<string> GetItems()
        {
            return Items;
        }

        public void AddItem(string item)
        {
            if (!Items.Contains(item))
            {
                Items.Add(item);
                AddItemToSidebar(item);
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

        public void Save()
        {
            using (var file = File.CreateText(ApplicationData.Current.LocalCacheFolder.Path + Path.DirectorySeparatorChar + JsonFileName))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, this);
            }
        }

        public async void AddItemToSidebar(string path)
        {
            try
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);
                int insertIndex = App.sideBarItems.IndexOf(App.sideBarItems.Last(x => x.ItemType == NavigationControlItemType.Location)) + 1;
                var locationItem = new LocationItem
                {
                    Path = path,
                    Glyph = GetItemIcon(path),
                    IsDefaultLocation = false,
                    Text = folder.DisplayName
                };
                App.sideBarItems.Insert(insertIndex, locationItem);
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine(ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                Debug.WriteLine("Pinned item was deleted and will be removed from the file lines list soon: " + ex.Message);
                RemoveItem(path);
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                Debug.WriteLine("Pinned item's drive was ejected and will be removed from the file lines list soon: " + ex.Message);
                RemoveItem(path);
            }
        }

        public void AddAllItemsToSidebar()
        {
            for (int i = 0; i < Items.Count(); i++)
            {
                string path = Items[i];
                AddItemToSidebar(path);
            }
        }

        public void RemoveStaleSidebarItems()
        {
            // Remove unpinned items from sidebar
            for (int i = 0; i < App.sideBarItems.Count(); i++)
            {
                if (App.sideBarItems[i] is LocationItem)
                {
                    var item = App.sideBarItems[i] as LocationItem;
                    if (!item.IsDefaultLocation && !Items.Contains(item.Path))
                    {
                        App.sideBarItems.RemoveAt(i);
                    }
                }
            }
        }

        public string GetItemIcon(string path)
        {
            string iconCode;

            if (path.Equals(App.AppSettings.DesktopPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uE8FC";
            }
            else if (path.Equals(App.AppSettings.DownloadsPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uE896";
            }
            else if (path.Equals(App.AppSettings.DocumentsPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uE8A5";
            }
            else if (path.Equals(App.AppSettings.PicturesPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uEB9F";
            }
            else if (path.Equals(App.AppSettings.MusicPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uEC4F";
            }
            else if (path.Equals(App.AppSettings.VideosPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uE8B2";
            }
            else if(Path.GetPathRoot(path).Equals(path, StringComparison.OrdinalIgnoreCase))
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
