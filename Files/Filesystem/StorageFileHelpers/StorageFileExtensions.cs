using Files.Common;
using Files.Filesystem;
using Files.Helpers;
using Files.View_Models;
using Files.Views;
using Files.Views.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;

namespace Files.Filesystem
{
    public static class StorageFileExtensions
    {
        private static SettingsViewModel _AppSettings => App.AppSettings;

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
                    if (MainPage.sideBarItems.FirstOrDefault(x => x.ItemType == NavigationControlItemType.Drive && x.Path.Contains(s, StringComparison.OrdinalIgnoreCase)) != null)
                    {
                        componentLabel = MainPage.sideBarItems.FirstOrDefault(x => x.ItemType == NavigationControlItemType.Drive && x.Path.Contains(s, StringComparison.OrdinalIgnoreCase)).Text;
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

        public async static Task<StorageFolderWithPath> GetFolderWithPathFromPathAsync(string value, StorageFolderWithPath rootFolder = null, StorageFolderWithPath parentFolder = null)
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

            if (parentFolder != null && !Path.IsPathRooted(value))
            {
                // Relative path
                var fullPath = Path.GetFullPath(Path.Combine(parentFolder.Path, value));
                return new StorageFolderWithPath(await StorageFolder.GetFolderFromPathAsync(fullPath));
            }
            else
            {
                return new StorageFolderWithPath(await StorageFolder.GetFolderFromPathAsync(value));
            }
        }

        public async static Task<StorageFolder> GetFolderFromPathAsync(string value, StorageFolderWithPath rootFolder = null, StorageFolderWithPath parentFolder = null)
        {
            return (await GetFolderWithPathFromPathAsync(value, rootFolder, parentFolder)).Folder;
        }

        public async static Task<StorageFileWithPath> GetFileWithPathFromPathAsync(string value, StorageFolderWithPath rootFolder = null, StorageFolderWithPath parentFolder = null)
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

            if (parentFolder != null && !Path.IsPathRooted(value))
            {
                // Relative path
                var fullPath = Path.GetFullPath(Path.Combine(parentFolder.Path, value));
                return new StorageFileWithPath(await StorageFile.GetFileFromPathAsync(fullPath));
            }
            else
            {
                return new StorageFileWithPath(await StorageFile.GetFileFromPathAsync(value));
            }
        }

        public async static Task<StorageFile> GetFileFromPathAsync(string value, StorageFolderWithPath rootFolder = null, StorageFolderWithPath parentFolder = null)
        {
            return (await GetFileWithPathFromPathAsync(value, rootFolder, parentFolder)).File;
        }

        public async static Task<IList<StorageFolderWithPath>> GetFoldersWithPathAsync(this StorageFolderWithPath parentFolder, uint maxNumberOfItems = uint.MaxValue)
        {
            return (await parentFolder.Folder.GetFoldersAsync(CommonFolderQuery.DefaultQuery, 0, maxNumberOfItems)).Select(x => new StorageFolderWithPath(x, Path.Combine(parentFolder.Path, x.Name))).ToList();
        }

        public async static Task<IList<StorageFileWithPath>> GetFilesWithPathAsync(this StorageFolderWithPath parentFolder, uint maxNumberOfItems = uint.MaxValue)
        {
            return (await parentFolder.Folder.GetFilesAsync(CommonFileQuery.DefaultQuery, 0, maxNumberOfItems)).Select(x => new StorageFileWithPath(x, Path.Combine(parentFolder.Path, x.Name))).ToList();
        }

        public async static Task<IList<StorageFolderWithPath>> GetFoldersWithPathAsync(this StorageFolderWithPath parentFolder, string nameFilter, uint maxNumberOfItems = uint.MaxValue)
        {
            var queryOptions = new QueryOptions();
            queryOptions.ApplicationSearchFilter = $"System.FileName:{nameFilter}*";
            StorageFolderQueryResult queryResult = parentFolder.Folder.CreateFolderQueryWithOptions(queryOptions);

            return (await queryResult.GetFoldersAsync(0, maxNumberOfItems)).Select(x => new StorageFolderWithPath(x, Path.Combine(parentFolder.Path, x.Name))).ToList();
        }

        public static string GetPathWithoutEnvironmentVariable(string path)
        {
            if (path.Contains("%temp%", StringComparison.OrdinalIgnoreCase)) 
                path = path.Replace("%temp%", _AppSettings.TempPath, StringComparison.OrdinalIgnoreCase);

            if (path.Contains("%tmp%", StringComparison.OrdinalIgnoreCase)) 
                path = path.Replace("%tmp%", _AppSettings.TempPath, StringComparison.OrdinalIgnoreCase);

            if (path.Contains("%localappdata%", StringComparison.OrdinalIgnoreCase)) 
                path = path.Replace("%localappdata%", _AppSettings.LocalAppDataPath, StringComparison.OrdinalIgnoreCase);

            if (path.Contains("%homepath%", StringComparison.OrdinalIgnoreCase)) 
                path = path.Replace("%homepath%", _AppSettings.HomePath, StringComparison.OrdinalIgnoreCase);

            return Environment.ExpandEnvironmentVariables(path);
        }
    }
}