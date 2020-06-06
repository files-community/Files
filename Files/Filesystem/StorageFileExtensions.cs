using Files.Common;
using Files.Filesystem;
using Files.Helpers;
using Files.Views.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files
{
    public interface IStorageItemWithPath
    {
        public string Path { get; set; }
        public IStorageItem Item { get; set; }
    }

    public class StorageFolderWithPath : IStorageItemWithPath
    {
        public StorageFolder Folder
        {
            get
            {
                return (StorageFolder)Item;
            }
            set
            {
                Item = value;
            }
        }
        public string Path { get; set; }
        public IStorageItem Item { get; set; }

        public StorageFolderWithPath(StorageFolder folder)
        {
            Folder = folder;
            Path = folder.Path;
        }

        public StorageFolderWithPath(StorageFolder folder, string path)
        {
            Folder = folder;
            Path = path;
        }
    }

    public class StorageFileWithPath : IStorageItemWithPath
    {
        public StorageFile File
        {
            get
            {
                return (StorageFile)Item;
            }
            set
            {
                Item = value;
            }
        }
        public string Path { get; set; }
        public IStorageItem Item { get; set; }

        public StorageFileWithPath(StorageFile file)
        {
            File = file;
            Path = File.Path;
        }

        public StorageFileWithPath(StorageFile file, string path)
        {
            File = file;
            Path = path;
        }
    }

    public static class StorageFileExtensions
    {
        public static INavigationControlItem GetMatchingSidebarItem(string value)
        {
            INavigationControlItem item = null;
            List<INavigationControlItem> sidebarItems = App.sideBarItems.Where(x => !string.IsNullOrWhiteSpace(x.Path)).ToList();

            item = sidebarItems.FirstOrDefault(x => x.Path.Equals(value, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                item = sidebarItems.FirstOrDefault(x => x.Path.Equals(value + "\\", StringComparison.OrdinalIgnoreCase));
            }
            if (item == null)
            {
                item = sidebarItems.FirstOrDefault(x => value.StartsWith(x.Path, StringComparison.OrdinalIgnoreCase));
            }
            if (item == null)
            {
                item = sidebarItems.FirstOrDefault(x => x.Path.Equals(Path.GetPathRoot(value), StringComparison.OrdinalIgnoreCase));
            }
            return item;
        }

        public static List<PathBoxItem> GetDirectoryPathComponents(string value)
        {
            List<string> pathComponents = new List<string>();
            List<PathBoxItem> pathBoxItems = new List<PathBoxItem>();

            // If path is a library, simplify it
            // If path is found to not be a library
            if (value.StartsWith("\\\\?\\"))
            {
                pathComponents = value.Replace("\\\\?\\", "").Split("\\", StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            else
            {
                pathComponents = value.Split("\\", StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            int index = 0;
            foreach (string s in pathComponents)
            {
                string componentLabel = null;
                string tag = "";
                if (s.StartsWith(App.AppSettings.RecycleBinPath))
                {
                    // Handle the recycle bin: use the localized folder name
                    PathBoxItem item = new PathBoxItem()
                    {
                        Title = ApplicationData.Current.LocalSettings.Values.Get("RecycleBin_Title", "Recycle Bin"),
                        Path = tag,
                    };
                    App.CurrentInstance.NavigationToolbar.PathComponents.Add(item);
                }
                else if (s.Contains(":"))
                {
                    if (App.sideBarItems.FirstOrDefault(x => x.ItemType == NavigationControlItemType.Drive && x.Path.Contains(s, StringComparison.OrdinalIgnoreCase)) != null)
                    {
                        componentLabel = App.sideBarItems.FirstOrDefault(x => x.ItemType == NavigationControlItemType.Drive && x.Path.Contains(s, StringComparison.OrdinalIgnoreCase)).Text;
                    }
                    else
                    {
                        componentLabel = @"Drive (" + s + @"\)";
                    }
                    tag = s + @"\";

                    PathBoxItem item = new PathBoxItem()
                    {
                        Title = componentLabel,
                        Path = tag,
                    };
                    pathBoxItems.Add(item);
                }
                else
                {
                    componentLabel = s;
                    foreach (string part in pathComponents.GetRange(0, index + 1))
                    {
                        tag = tag + part + @"\";
                    }
                    if (value.StartsWith("\\\\?\\"))
                    {
                        tag = "\\\\?\\" + tag;
                    }
                    else if (index == 0)
                    {
                        tag = "\\\\" + tag;
                    }

                    PathBoxItem item = new PathBoxItem()
                    {
                        Title = componentLabel,
                        Path = tag,
                    };
                    pathBoxItems.Add(item);
                }
                index++;
            }
            return pathBoxItems;
        }

        public async static Task<StorageFolderWithPath> GetFolderFromRelativePathAsync(string value, StorageFolderWithPath rootFolder = null, StorageFolderWithPath parentFolder = null)
        {
            if (rootFolder != null)
            {
                var currComponents = GetDirectoryPathComponents(value);

                if (rootFolder.Path == value)
                {
                    return rootFolder;
                }
                else if (parentFolder != null && value.IsSubPathOf(parentFolder.Path))
                {
                    var folder = parentFolder.Folder;
                    var prevComponents = GetDirectoryPathComponents(parentFolder.Path);
                    var path = parentFolder.Path;
                    foreach (var component in currComponents.ExceptBy(prevComponents, c => c.Path))
                    {
                        folder = await folder.GetFolderAsync(component.Title);
                        path = Path.Combine(path, folder.Name);
                    }
                    return new StorageFolderWithPath(folder, path);
                }
                else if (value.IsSubPathOf(rootFolder.Path))
                {
                    var folder = rootFolder.Folder;
                    var path = rootFolder.Path;
                    foreach (var component in currComponents.Skip(1))
                    {
                        folder = await folder.GetFolderAsync(component.Title);
                        path = Path.Combine(path, folder.Name);
                    }
                    return new StorageFolderWithPath(folder, path);
                }
            }
            return new StorageFolderWithPath(await StorageFolder.GetFolderFromPathAsync(value));
        }

        public async static Task<StorageFileWithPath> GetFileFromRelativePathAsync(string value, StorageFolderWithPath rootFolder = null, StorageFolderWithPath parentFolder = null)
        {
            if (rootFolder != null)
            {
                var currComponents = GetDirectoryPathComponents(value);

                if (parentFolder != null && value.IsSubPathOf(parentFolder.Path))
                {
                    var folder = parentFolder.Folder;
                    var prevComponents = GetDirectoryPathComponents(parentFolder.Path);
                    var path = parentFolder.Path;
                    foreach (var component in currComponents.ExceptBy(prevComponents, c => c.Path).SkipLast(1))
                    {
                        folder = await folder.GetFolderAsync(component.Title);
                        path = Path.Combine(path, folder.Name);
                    }
                    var file = await folder.GetFileAsync(currComponents.Last().Title);
                    path = Path.Combine(path, file.Name);
                    return new StorageFileWithPath(file, path);
                }
                else if (value.IsSubPathOf(rootFolder.Path))
                {
                    var folder = rootFolder.Folder;
                    var path = rootFolder.Path;
                    foreach (var component in currComponents.Skip(1).SkipLast(1))
                    {
                        folder = await folder.GetFolderAsync(component.Title);
                        path = Path.Combine(path, folder.Name);
                    }
                    var file = await folder.GetFileAsync(currComponents.Last().Title);
                    path = Path.Combine(path, file.Name);
                    return new StorageFileWithPath(file, path);
                }
            }
            return new StorageFileWithPath(await StorageFile.GetFileFromPathAsync(value));
        }
    }
}
