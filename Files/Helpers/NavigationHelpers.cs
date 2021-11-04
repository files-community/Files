using Files.Common;
using Files.Enums;
using Files.Filesystem;
using Files.Filesystem.StorageItems;
using Files.Services;
using Files.ViewModels;
using Files.Views;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.System;
using Windows.UI.Core;

namespace Files.Helpers
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
            if (associatedInstance.FilesystemViewModel.WorkingDirectory.StartsWith(CommonPaths.RecycleBinPath))
            {
                // Do not open files and folders inside the recycle bin
                return;
            }
            if (associatedInstance.SlimContentPage == null)
            {
                return;
            }
            foreach (ListedItem item in associatedInstance.SlimContentPage.SelectedItems.ToList())
            {
                var type = item.PrimaryItemAttribute == StorageItemTypes.Folder ?
                    FilesystemItemType.Directory : FilesystemItemType.File;

                await OpenPath(item.ItemPath, associatedInstance, type, false, openViaApplicationPicker);
            }
        }

        public static async void OpenItemsWithExecutable(IShellPage associatedInstance, List<IStorageItemWithPath> items, string executable)
        {
            if (associatedInstance.FilesystemViewModel.WorkingDirectory.StartsWith(CommonPaths.RecycleBinPath))
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
        public static async Task<bool> OpenPath(string path, IShellPage associatedInstance, FilesystemItemType? itemType = null, bool openSilent = false, bool openViaApplicationPicker = false, IEnumerable<string> selectItems = null, string args = default)
        {
            string previousDir = associatedInstance.FilesystemViewModel.WorkingDirectory;
            bool isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Hidden);
            bool isShortcutItem = path.EndsWith(".lnk") || path.EndsWith(".url"); // Determine
            FilesystemResult opened = (FilesystemResult)false;

            var shortcutInfo = new ShortcutItem();
            if (itemType == null || isShortcutItem || isHiddenItem)
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

                    if (status == AppServiceResponseStatus.Success)
                    {
                        shortcutInfo.TargetPath = response.Get("TargetPath", string.Empty);
                        shortcutInfo.Arguments = response.Get("Arguments", string.Empty);
                        shortcutInfo.WorkingDirectory = response.Get("WorkingDirectory", string.Empty);
                        shortcutInfo.RunAsAdmin = response.Get("RunAsAdmin", false);
                        shortcutInfo.PrimaryItemAttribute = response.Get("IsFolder", false) ? StorageItemTypes.Folder : StorageItemTypes.File;

                        itemType = response.Get("IsFolder", false) ? FilesystemItemType.Directory : FilesystemItemType.File;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (isHiddenItem)
                {
                    itemType = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Directory) ? FilesystemItemType.Directory : FilesystemItemType.File;
                }
                else
                {
                    itemType = await StorageItemHelpers.GetTypeFromPath(path);
                }
            }

            if (itemType == FilesystemItemType.Library)
            {
                opened = await OpenLibrary(path, associatedInstance, selectItems);
            }
            else if (itemType == FilesystemItemType.Directory)
            {
                opened = await OpenDirectory(path, associatedInstance, selectItems, shortcutInfo);
            }
            else if (itemType == FilesystemItemType.File)
            {
                opened = await OpenFile(path, associatedInstance, selectItems, shortcutInfo, openViaApplicationPicker, args);
            }

            if (opened.ErrorCode == FileSystemStatusCode.NotFound && !openSilent)
            {
                await DialogDisplayHelper.ShowDialogAsync("FileNotFoundDialog/Title".GetLocalized(), "FileNotFoundDialog/Text".GetLocalized());
                associatedInstance.NavToolbarViewModel.CanRefresh = false;
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    var ContentOwnedViewModelInstance = associatedInstance.FilesystemViewModel;
                    ContentOwnedViewModelInstance?.RefreshItems(previousDir);
                });
            }

            return opened;
        }

        private static async Task<FilesystemResult> OpenLibrary(string path, IShellPage associatedInstance, IEnumerable<string> selectItems)
        {
            IUserSettingsService userSettingsService = Ioc.Default.GetService<IUserSettingsService>();

            var opened = (FilesystemResult)false;
            bool isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Hidden);
            if (isHiddenItem)
            {
                if (userSettingsService.PreferencesSettingsService.OpenFoldersInNewTab)
                {
                    await OpenPathInNewTab(path);
                }
                else
                {
                    associatedInstance.NavToolbarViewModel.PathControlDisplayText = path;
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
                    if (userSettingsService.PreferencesSettingsService.OpenFoldersInNewTab)
                    {
                        await OpenPathInNewTab(library.Text);
                    }
                    else
                    {
                        associatedInstance.NavToolbarViewModel.PathControlDisplayText = library.Text;
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

        private static async Task<FilesystemResult> OpenDirectory(string path, IShellPage associatedInstance, IEnumerable<string> selectItems, ShortcutItem shortcutInfo)
        {
            IUserSettingsService userSettingsService = Ioc.Default.GetService<IUserSettingsService>();

            var opened = (FilesystemResult)false;
            bool isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Hidden);
            bool isShortcutItem = path.EndsWith(".lnk") || path.EndsWith(".url"); // Determine

            if (isShortcutItem)
            {
                if (string.IsNullOrEmpty(shortcutInfo.TargetPath))
                {
                    await Win32Helpers.InvokeWin32ComponentAsync(path, associatedInstance);
                    opened = (FilesystemResult)true;
                }
                else
                {
                    if (userSettingsService.PreferencesSettingsService.OpenFoldersInNewTab)
                    {
                        await OpenPathInNewTab(shortcutInfo.TargetPath);
                    }
                    else
                    {
                        associatedInstance.NavToolbarViewModel.PathControlDisplayText = shortcutInfo.TargetPath;
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
                if (userSettingsService.PreferencesSettingsService.OpenFoldersInNewTab)
                {
                    await OpenPathInNewTab(path);
                }
                else
                {
                    associatedInstance.NavToolbarViewModel.PathControlDisplayText = path;
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
                        // Add location to MRU List
                        if (childFolder.Folder is SystemStorageFolder)
                        {
                            var mostRecentlyUsed = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;
                            mostRecentlyUsed.Add(await childFolder.Folder.ToStorageFolderAsync(), childFolder.Path);
                        }
                    });
                if (!opened)
                {
                    opened = (FilesystemResult)FolderHelpers.CheckFolderAccessWithWin32(path);
                }
                if (opened)
                {
                    if (userSettingsService.PreferencesSettingsService.OpenFoldersInNewTab)
                    {
                        await OpenPathInNewTab(path);
                    }
                    else
                    {
                        associatedInstance.NavToolbarViewModel.PathControlDisplayText = path;
                        associatedInstance.NavigateWithArguments(associatedInstance.InstanceViewModel.FolderSettings.GetLayoutType(path), new NavigationArguments()
                        {
                            NavPathParam = path,
                            AssociatedTabInstance = associatedInstance,
                            SelectItems = selectItems
                        });
                    }
                }
            }
            return opened;
        }

        private static async Task<FilesystemResult> OpenFile(string path, IShellPage associatedInstance, IEnumerable<string> selectItems, ShortcutItem shortcutInfo, bool openViaApplicationPicker = false, string args = default)
        {
            var opened = (FilesystemResult)false;
            bool isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Hidden);
            bool isShortcutItem = path.EndsWith(".lnk") || path.EndsWith(".url"); // Determine
            if (isShortcutItem)
            {
                if (string.IsNullOrEmpty(shortcutInfo.TargetPath))
                {
                    await Win32Helpers.InvokeWin32ComponentAsync(path, associatedInstance, args);
                }
                else
                {
                    if (!path.EndsWith(".url"))
                    {
                        StorageFileWithPath childFile = await associatedInstance.FilesystemViewModel.GetFileWithPathFromPathAsync(shortcutInfo.TargetPath);
                        if (childFile != null)
                        {
                            // Add location to MRU List
                            if (childFile.File is SystemStorageFile)
                            {
                                var mostRecentlyUsed = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;
                                mostRecentlyUsed.Add(await childFile.File.ToStorageFileAsync(), childFile.Path);
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
                        // Add location to MRU List
                        if (childFile.File is SystemStorageFile)
                        {
                            var mostRecentlyUsed = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;
                            mostRecentlyUsed.Add(await childFile.File.ToStorageFileAsync(), childFile.Path);
                        }

                        if (openViaApplicationPicker)
                        {
                            LauncherOptions options = new LauncherOptions
                            {
                                DisplayApplicationPicker = true
                            };
                            if (!await Launcher.LaunchFileAsync(childFile.File, options))
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
                                    case Enums.SortOption.Name:
                                        sortEntry.PropertyName = "System.ItemNameDisplay";
                                        queryOptions.SortOrder.Clear();
                                        queryOptions.SortOrder.Add(sortEntry);
                                        break;

                                    case Enums.SortOption.DateModified:
                                        sortEntry.PropertyName = "System.DateModified";
                                        queryOptions.SortOrder.Clear();
                                        queryOptions.SortOrder.Add(sortEntry);
                                        break;

                                    case Enums.SortOption.DateCreated:
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
                                launchSuccess = await Launcher.LaunchFileAsync(await childFile.File.ToStorageFileAsync(), options);
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