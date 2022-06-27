using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Helpers;
using Files.Backend.Services.Settings;
using Files.Shared;
using Files.Shared.Enums;
using Files.Uwp.Filesystem;
using Files.Uwp.Filesystem.StorageItems;
using Files.Uwp.ViewModels;
using Files.Uwp.Views;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.System;

namespace Files.Uwp.Helpers
{
    public static class NavigationHelpers
    {
        public static async Task OpenPathInNewTab(string path)
        {
            await MainPageViewModel.AddNewTabByPathAsync(typeof(PaneHolderPage), path);
        }

        public static async Task<bool> OpenPathInNewWindowAsync(string path)
        {
            var folderUri = new Uri($"files-uwp:?folder={Uri.EscapeDataString(path)}");
            return await Launcher.LaunchUriAsync(folderUri);
        }

        public static async Task<bool> OpenTabInNewWindowAsync(string tabArgs)
        {
            var folderUri = new Uri($"files-uwp:?tab={Uri.EscapeDataString(tabArgs)}");
            return await Launcher.LaunchUriAsync(folderUri);
        }

        public static async void LaunchNewWindow()
        {
            var filesUWPUri = new Uri("files-uwp:");
            await Launcher.LaunchUriAsync(filesUWPUri);
        }

        public static async Task OpenDirectoryInTerminal(string workingDir)
        {
            var terminal = App.TerminalController.Model.GetDefaultTerminal();
            if (terminal == null)
            {
                return;
            }

            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var value = new ValueSet()
                {
                    { "Arguments", "LaunchApp" },
                    { "WorkingDirectory", workingDir },
                    { "Application", terminal.Path },
                    { "Parameters", string.Format(terminal.Arguments,
                       Helpers.PathNormalization.NormalizePath(workingDir)) }
                };
                await connection.SendMessageAsync(value);
            }
        }

        public static async void OpenSelectedItems(IShellPage associatedInstance, bool openViaApplicationPicker = false)
        {
            if (associatedInstance.FilesystemViewModel.WorkingDirectory.StartsWith(CommonPaths.RecycleBinPath, StringComparison.Ordinal))
            {
                // Do not open files and folders inside the recycle bin
                return;
            }
            if (associatedInstance.SlimContentPage == null)
            {
                return;
            }

            bool forceOpenInNewTab = false;
            var selectedItems = associatedInstance.SlimContentPage.SelectedItems.ToList();
            var opened = false;

            if (!openViaApplicationPicker &&
                selectedItems.Count > 1 &&
                selectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.File && !x.IsExecutable && !x.IsShortcutItem))
            {
                // Multiple files are selected, open them together
                opened = await Win32Helpers.InvokeWin32ComponentAsync(string.Join('|', selectedItems.Select(x => x.ItemPath)), associatedInstance);
            }
            if (!opened)
            {
                foreach (ListedItem item in selectedItems)
                {
                    var type = item.PrimaryItemAttribute == StorageItemTypes.Folder ?
                        FilesystemItemType.Directory : FilesystemItemType.File;

                    await OpenPath(item.ItemPath, associatedInstance, type, false, openViaApplicationPicker, forceOpenInNewTab: forceOpenInNewTab);

                    if (type == FilesystemItemType.Directory)
                    {
                        forceOpenInNewTab = true;
                    }
                }
            }
        }

        public static async void OpenItemsWithExecutable(IShellPage associatedInstance, IEnumerable<IStorageItemWithPath> items, string executable)
        {
            if (associatedInstance.FilesystemViewModel.WorkingDirectory.StartsWith(CommonPaths.RecycleBinPath, StringComparison.Ordinal))
            {
                // Do not open files and folders inside the recycle bin
                return;
            }
            if (associatedInstance.SlimContentPage == null)
            {
                return;
            }
            foreach (var item in items)
            {
                try
                {
                    await OpenPath(executable, associatedInstance, FilesystemItemType.File, false, false, args: $"\"{item.Path}\"");
                }
                catch (Exception e)
                {
                    // This is to try and figure out the root cause of AppCenter error #985932119u
                    App.Logger.Warn(e, e.Message);
                }
            }
        }

        /// <summary>
        /// Navigates to a directory or opens file
        /// </summary>
        /// <param name="path">The path to navigate to or open</param>
        /// <param name="associatedInstance">The instance associated with view</param>
        /// <param name="itemType"></param>
        /// <param name="openSilent">Determines whether history of opened item is saved (... to Recent Items/Windows Timeline/opening in background)</param>
        /// <param name="openViaApplicationPicker">Determines whether open file using application picker</param>
        /// <param name="selectItems">List of filenames that are selected upon navigation</param>
        /// <param name="forceOpenInNewTab">Open folders in a new tab regardless of the "OpenFoldersInNewTab" option</param>
        public static async Task<bool> OpenPath(string path, IShellPage associatedInstance, FilesystemItemType? itemType = null, bool openSilent = false, bool openViaApplicationPicker = false, IEnumerable<string> selectItems = null, string args = default, bool forceOpenInNewTab = false)
        {
            string previousDir = associatedInstance.FilesystemViewModel.WorkingDirectory;
            bool isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Hidden);
            bool isDirectory = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Directory);
            bool isReparsePoint = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.ReparsePoint);
            bool isShortcutItem = path.EndsWith(".lnk", StringComparison.Ordinal) || path.EndsWith(".url", StringComparison.Ordinal);
            FilesystemResult opened = (FilesystemResult)false;

            var shortcutInfo = new ShellLinkItem();
            if (itemType == null || isShortcutItem || isHiddenItem || isReparsePoint)
            {
                if (isShortcutItem)
                {
                    var connection = await AppServiceConnectionHelper.Instance;
                    if (connection == null)
                    {
                        return false;
                    }
                    var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
                    {
                        { "Arguments", "FileOperation" },
                        { "fileop", "ParseLink" },
                        { "filepath", path }
                    });

                    if (status == AppServiceResponseStatus.Success && response.ContainsKey("ShortcutInfo"))
                    {
                        var shInfo = JsonConvert.DeserializeObject<ShellLinkItem>((string)response["ShortcutInfo"]);
                        if (shInfo != null)
                        {
                            shortcutInfo = shInfo;
                        }
                        itemType = shInfo != null && shInfo.IsFolder ? FilesystemItemType.Directory : FilesystemItemType.File;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (isReparsePoint)
                {
                    if (!isDirectory)
                    {
                        if (NativeFindStorageItemHelper.GetWin32FindDataForPath(path, out var findData))
                        {
                            if (findData.dwReserved0 == NativeFileOperationsHelper.IO_REPARSE_TAG_SYMLINK)
                            {
                                shortcutInfo.TargetPath = NativeFileOperationsHelper.ParseSymLink(path);
                            }
                        }
                    }
                    itemType ??= isDirectory ? FilesystemItemType.Directory : FilesystemItemType.File;
                }
                else if (isHiddenItem)
                {
                    itemType = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Directory) ? FilesystemItemType.Directory : FilesystemItemType.File;
                }
                else
                {
                    itemType = await StorageHelpers.GetTypeFromPath(path);
                }
            }

            if (itemType == FilesystemItemType.Library)
            {
                opened = await OpenLibrary(path, associatedInstance, selectItems, forceOpenInNewTab);
            }
            else if (itemType == FilesystemItemType.Directory)
            {
                opened = await OpenDirectory(path, associatedInstance, selectItems, shortcutInfo, forceOpenInNewTab);
            }
            else if (itemType == FilesystemItemType.File)
            {
                opened = await OpenFile(path, associatedInstance, selectItems, shortcutInfo, openViaApplicationPicker, args);
            }

            if (opened.ErrorCode == FileSystemStatusCode.NotFound && !openSilent)
            {
                await DialogDisplayHelper.ShowDialogAsync("FileNotFoundDialog/Title".GetLocalized(), "FileNotFoundDialog/Text".GetLocalized());
                associatedInstance.ToolbarViewModel.CanRefresh = false;
                associatedInstance.FilesystemViewModel?.RefreshItems(previousDir);
            }

            return opened;
        }

        private static async Task<FilesystemResult> OpenLibrary(string path, IShellPage associatedInstance, IEnumerable<string> selectItems, bool forceOpenInNewTab)
        {
            IUserSettingsService userSettingsService = Ioc.Default.GetService<IUserSettingsService>();

            var opened = (FilesystemResult)false;
            bool isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Hidden);
            if (isHiddenItem)
            {
                if (forceOpenInNewTab || userSettingsService.PreferencesSettingsService.OpenFoldersInNewTab)
                {
                    await OpenPathInNewTab(path);
                }
                else
                {
                    associatedInstance.ToolbarViewModel.PathControlDisplayText = path;
                    associatedInstance.NavigateWithArguments(associatedInstance.InstanceViewModel.FolderSettings.GetLayoutType(path), new NavigationArguments()
                    {
                        NavPathParam = path,
                        AssociatedTabInstance = associatedInstance
                    });
                }
                opened = (FilesystemResult)true;
            }
            else if (App.LibraryManager.TryGetLibrary(path, out LibraryLocationItem library))
            {
                opened = (FilesystemResult)await library.CheckDefaultSaveFolderAccess();
                if (opened)
                {
                    if (forceOpenInNewTab || userSettingsService.PreferencesSettingsService.OpenFoldersInNewTab)
                    {
                        await OpenPathInNewTab(library.Text);
                    }
                    else
                    {
                        associatedInstance.ToolbarViewModel.PathControlDisplayText = library.Text;
                        associatedInstance.NavigateWithArguments(associatedInstance.InstanceViewModel.FolderSettings.GetLayoutType(path), new NavigationArguments()
                        {
                            NavPathParam = path,
                            AssociatedTabInstance = associatedInstance,
                            SelectItems = selectItems,
                        });
                    }
                }
            }
            return opened;
        }

        private static async Task<FilesystemResult> OpenDirectory(string path, IShellPage associatedInstance, IEnumerable<string> selectItems, ShellLinkItem shortcutInfo, bool forceOpenInNewTab)
        {
            IUserSettingsService userSettingsService = Ioc.Default.GetService<IUserSettingsService>();

            var opened = (FilesystemResult)false;
            bool isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Hidden);
            bool isShortcutItem = path.EndsWith(".lnk", StringComparison.Ordinal) || path.EndsWith(".url", StringComparison.Ordinal); // Determine

            if (isShortcutItem)
            {
                if (string.IsNullOrEmpty(shortcutInfo.TargetPath))
                {
                    await Win32Helpers.InvokeWin32ComponentAsync(path, associatedInstance);
                    opened = (FilesystemResult)true;
                }
                else
                {
                    if (forceOpenInNewTab || userSettingsService.PreferencesSettingsService.OpenFoldersInNewTab)
                    {
                        await OpenPathInNewTab(shortcutInfo.TargetPath);
                    }
                    else
                    {
                        associatedInstance.ToolbarViewModel.PathControlDisplayText = shortcutInfo.TargetPath;
                        associatedInstance.NavigateWithArguments(associatedInstance.InstanceViewModel.FolderSettings.GetLayoutType(shortcutInfo.TargetPath), new NavigationArguments()
                        {
                            NavPathParam = shortcutInfo.TargetPath,
                            AssociatedTabInstance = associatedInstance,
                            SelectItems = selectItems
                        });
                    }

                    opened = (FilesystemResult)true;
                }
            }
            else if (isHiddenItem)
            {
                if (forceOpenInNewTab || userSettingsService.PreferencesSettingsService.OpenFoldersInNewTab)
                {
                    await OpenPathInNewTab(path);
                }
                else
                {
                    associatedInstance.ToolbarViewModel.PathControlDisplayText = path;
                    associatedInstance.NavigateWithArguments(associatedInstance.InstanceViewModel.FolderSettings.GetLayoutType(path), new NavigationArguments()
                    {
                        NavPathParam = path,
                        AssociatedTabInstance = associatedInstance
                    });
                }

                opened = (FilesystemResult)true;
            }
            else
            {
                opened = await associatedInstance.FilesystemViewModel.GetFolderWithPathFromPathAsync(path)
                    .OnSuccess(async (childFolder) =>
                    {
                        // Add location to Recent Items List
                        if (childFolder.Item is SystemStorageFolder)
                        {
                            await App.RecentItemsManager.AddToRecentItems(childFolder.Path);
                        }
                    });
                if (!opened)
                {
                    opened = (FilesystemResult)FolderHelpers.CheckFolderAccessWithWin32(path);
                }
                if (opened)
                {
                    if (forceOpenInNewTab || userSettingsService.PreferencesSettingsService.OpenFoldersInNewTab)
                    {
                        await OpenPathInNewTab(path);
                    }
                    else
                    {
                        associatedInstance.ToolbarViewModel.PathControlDisplayText = path;
                        associatedInstance.NavigateWithArguments(associatedInstance.InstanceViewModel.FolderSettings.GetLayoutType(path), new NavigationArguments()
                        {
                            NavPathParam = path,
                            AssociatedTabInstance = associatedInstance,
                            SelectItems = selectItems
                        });
                    }
                }
                else
                {
                    await Win32Helpers.InvokeWin32ComponentAsync(path, associatedInstance);
                }
            }
            return opened;
        }

        private static async Task<FilesystemResult> OpenFile(string path, IShellPage associatedInstance, IEnumerable<string> selectItems, ShellLinkItem shortcutInfo, bool openViaApplicationPicker = false, string args = default)
        {
            var opened = (FilesystemResult)false;
            bool isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Hidden);
            bool isShortcutItem = path.EndsWith(".lnk", StringComparison.Ordinal) || path.EndsWith(".url", StringComparison.Ordinal) || !string.IsNullOrEmpty(shortcutInfo.TargetPath);
            if (isShortcutItem)
            {
                if (string.IsNullOrEmpty(shortcutInfo.TargetPath))
                {
                    await Win32Helpers.InvokeWin32ComponentAsync(path, associatedInstance, args);
                }
                else
                {
                    if (!path.EndsWith(".url", StringComparison.Ordinal))
                    {
                        StorageFileWithPath childFile = await associatedInstance.FilesystemViewModel.GetFileWithPathFromPathAsync(shortcutInfo.TargetPath);
                        if (childFile != null)
                        {
                            // Add location to Recent Items List
                            if (childFile.Item is SystemStorageFile)
                            {
                                await App.RecentItemsManager.AddToRecentItems(childFile.Path);
                            }
                        }
                    }
                    await Win32Helpers.InvokeWin32ComponentAsync(shortcutInfo.TargetPath, associatedInstance, $"{args} {shortcutInfo.Arguments}", shortcutInfo.RunAsAdmin, shortcutInfo.WorkingDirectory);
                }
                opened = (FilesystemResult)true;
            }
            else if (isHiddenItem)
            {
                await Win32Helpers.InvokeWin32ComponentAsync(path, associatedInstance, args);
            }
            else
            {
                opened = await associatedInstance.FilesystemViewModel.GetFileWithPathFromPathAsync(path)
                    .OnSuccess(async childFile =>
                    {
                        // Add location to Recent Items List
                        if (childFile.Item is SystemStorageFile)
                        {
                            await App.RecentItemsManager.AddToRecentItems(childFile.Path);
                        }

                        if (openViaApplicationPicker)
                        {
                            LauncherOptions options = new LauncherOptions
                            {
                                DisplayApplicationPicker = true
                            };
                            if (!await Launcher.LaunchFileAsync(childFile.Item, options))
                            {
                                var connection = await AppServiceConnectionHelper.Instance;
                                if (connection != null)
                                {
                                    await connection.SendMessageAsync(new ValueSet()
                                    {
                                        { "Arguments", "InvokeVerb" },
                                        { "FilePath", path },
                                        { "Verb", "openas" }
                                    });
                                }
                            }
                        }
                        else
                        {
                            //try using launcher first
                            bool launchSuccess = false;

                            BaseStorageFileQueryResult fileQueryResult = null;

                            //Get folder to create a file query (to pass to apps like Photos, Movies & TV..., needed to scroll through the folder like what Windows Explorer does)
                            BaseStorageFolder currentFolder = await associatedInstance.FilesystemViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(path));

                            if (currentFolder != null)
                            {
                                QueryOptions queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, null);

                                //We can have many sort entries
                                SortEntry sortEntry = new SortEntry()
                                {
                                    AscendingOrder = associatedInstance.InstanceViewModel.FolderSettings.DirectorySortDirection == SortDirection.Ascending
                                };

                                //Basically we tell to the launched app to follow how we sorted the files in the directory.
                                var sortOption = associatedInstance.InstanceViewModel.FolderSettings.DirectorySortOption;

                                switch (sortOption)
                                {
                                    case SortOption.Name:
                                        sortEntry.PropertyName = "System.ItemNameDisplay";
                                        queryOptions.SortOrder.Clear();
                                        queryOptions.SortOrder.Add(sortEntry);
                                        break;

                                    case SortOption.DateModified:
                                        sortEntry.PropertyName = "System.DateModified";
                                        queryOptions.SortOrder.Clear();
                                        queryOptions.SortOrder.Add(sortEntry);
                                        break;

                                    case SortOption.DateCreated:
                                        sortEntry.PropertyName = "System.DateCreated";
                                        queryOptions.SortOrder.Clear();
                                        queryOptions.SortOrder.Add(sortEntry);
                                        break;

                                    //Unfortunately this is unsupported | Remarks: https://docs.microsoft.com/en-us/uwp/api/windows.storage.search.queryoptions.sortorder?view=winrt-19041
                                    //case Enums.SortOption.Size:

                                    //sortEntry.PropertyName = "System.TotalFileSize";
                                    //queryOptions.SortOrder.Clear();
                                    //queryOptions.SortOrder.Add(sortEntry);
                                    //break;

                                    //Unfortunately this is unsupported | Remarks: https://docs.microsoft.com/en-us/uwp/api/windows.storage.search.queryoptions.sortorder?view=winrt-19041
                                    //case Enums.SortOption.FileType:

                                    //sortEntry.PropertyName = "System.FileExtension";
                                    //queryOptions.SortOrder.Clear();
                                    //queryOptions.SortOrder.Add(sortEntry);
                                    //break;

                                    //Handle unsupported
                                    default:
                                        //keep the default one in SortOrder IList
                                        break;
                                }

                                var options = new LauncherOptions();
                                if (currentFolder.AreQueryOptionsSupported(queryOptions))
                                {
                                    fileQueryResult = currentFolder.CreateFileQueryWithOptions(queryOptions);
                                    options.NeighboringFilesQuery = fileQueryResult.ToStorageFileQueryResult();
                                }

                                // Now launch file with options.
                                var storageItem = (StorageFile)await FilesystemTasks.Wrap(() => childFile.Item.ToStorageFileAsync().AsTask());
                                if (storageItem != null)
                                {
                                    launchSuccess = await Launcher.LaunchFileAsync(storageItem, options);
                                }
                            }

                            if (!launchSuccess)
                            {
                                await Win32Helpers.InvokeWin32ComponentAsync(path, associatedInstance, args);
                            }
                        }
                    });
            }
            return opened;
        }
    }
}