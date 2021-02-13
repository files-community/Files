using Files.Common;
using Files.DataModels;
using Files.Dialogs;
using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using Files.ViewModels;
using Files.Views;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.Extensions;
using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.UserProfile;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using static Files.Views.Properties;

namespace Files.Interacts
{
    public class Interaction
    {
        public IFilesystemHelpers FilesystemHelpers => AssociatedInstance.FilesystemHelpers;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IShellPage AssociatedInstance;

        public SettingsViewModel AppSettings => App.AppSettings;

        public FolderSettingsViewModel FolderSettings => AssociatedInstance?.InstanceViewModel.FolderSettings;

        private AppServiceConnection Connection => AssociatedInstance?.ServiceConnection;

        public Interaction(IShellPage appInstance)
        {
            AssociatedInstance = appInstance;
        }

        public void List_ItemDoubleClick(object sender, DoubleTappedRoutedEventArgs e)
        {
            // Skip opening selected items if the double tap doesn't capture an item
            if ((e.OriginalSource as FrameworkElement)?.DataContext is ListedItem && !AppSettings.OpenItemsWithOneclick)
            {
                OpenSelectedItems(false);
            }
        }

        public void SetAsDesktopBackgroundItem_Click(object sender, RoutedEventArgs e)
        {
            SetAsBackground(WallpaperType.Desktop);
        }

        public void SetAsLockscreenBackgroundItem_Click(object sender, RoutedEventArgs e)
        {
            SetAsBackground(WallpaperType.LockScreen);
        }

        public async void SetAsBackground(WallpaperType type)
        {
            if (UserProfilePersonalizationSettings.IsSupported())
            {
                // Get the path of the selected file
                var sourceFile = (StorageFile)await AssociatedInstance.FilesystemViewModel.GetFileFromPathAsync(AssociatedInstance.ContentPage.SelectedItem.ItemPath);
                if (sourceFile == null)
                {
                    return;
                }

                // Get the app's local folder to use as the destination folder.
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;

                // the file to the destination folder.
                // Generate unique name if the file already exists.
                // If the file you are trying to set as the wallpaper has the same name as the current wallpaper,
                // the system will ignore the request and no-op the operation
                var file = (StorageFile)await FilesystemTasks.Wrap(() => sourceFile.CopyAsync(localFolder, sourceFile.Name, NameCollisionOption.GenerateUniqueName).AsTask());
                if (file == null)
                {
                    return;
                }

                UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
                if (type == WallpaperType.Desktop)
                {
                    // Set the desktop background
                    await profileSettings.TrySetWallpaperImageAsync(file);
                }
                else if (type == WallpaperType.LockScreen)
                {
                    // Set the lockscreen background
                    await profileSettings.TrySetLockScreenImageAsync(file);
                }
            }
        }

        public RelayCommand AddNewTabToMultitaskingControl => new RelayCommand(() => OpenNewTab());

        private async void OpenNewTab()
        {
            await MainPage.AddNewTabByPathAsync(typeof(PaneHolderPage), "NewTab".GetLocalized());
        }

        public async void OpenInNewWindowItem_Click()
        {
            var items = AssociatedInstance.ContentPage.SelectedItems;
            foreach (ListedItem listedItem in items)
            {
                var selectedItemPath = (listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath;
                var folderUri = new Uri($"files-uwp:?folder={@selectedItemPath}");
                await Launcher.LaunchUriAsync(folderUri);
            }
        }

        public void OpenDirectoryInNewPane_Click()
        {
            var listedItem = AssociatedInstance.ContentPage.SelectedItems.FirstOrDefault();
            if (listedItem != null)
            {
                AssociatedInstance.PaneHolder?.OpenPathInNewPane((listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath);
            }
        }

        public RelayCommand OpenNewPane => new RelayCommand(() => OpenNewPaneCommand());

        public void OpenNewPaneCommand()
        {
            AssociatedInstance.PaneHolder?.OpenPathInNewPane("NewTab".GetLocalized());
        }

        public async void OpenDirectoryInNewTab_Click()
        {
            foreach (ListedItem listedItem in AssociatedInstance.ContentPage.SelectedItems)
            {
                await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
                {
                    await MainPage.AddNewTabByPathAsync(typeof(PaneHolderPage), (listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath);
                });
            }
        }

        public void ItemPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed)
            {
                if ((e.OriginalSource as FrameworkElement)?.DataContext is ListedItem Item && Item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    if (Item.IsShortcutItem)
                    {
                        OpenPathInNewTab(((e.OriginalSource as FrameworkElement)?.DataContext as ShortcutItem)?.TargetPath ?? Item.ItemPath);
                    }
                    else
                    {
                        OpenPathInNewTab(Item.ItemPath);
                    }
                }
            }
        }

        public static async void OpenPathInNewTab(string path)
        {
            await MainPage.AddNewTabByPathAsync(typeof(PaneHolderPage), path);
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

        public RelayCommand OpenDirectoryInDefaultTerminal => new RelayCommand(() => OpenDirectoryInTerminal());

        private async void OpenDirectoryInTerminal()
        {
            var terminal = AppSettings.TerminalController.Model.GetDefaultTerminal();

            if (Connection != null)
            {
                var value = new ValueSet
                {
                    { "WorkingDirectory", AssociatedInstance.FilesystemViewModel.WorkingDirectory },
                    { "Application", terminal.Path },
                    { "Arguments", string.Format(terminal.Arguments,
                       Helpers.PathNormalization.NormalizePath(AssociatedInstance.FilesystemViewModel.WorkingDirectory)) }
                };
                await Connection.SendMessageAsync(value);
            }
        }

        public void PinItem_Click(object sender, RoutedEventArgs e)
        {
            if (AssociatedInstance.ContentPage != null)
            {
                foreach (ListedItem listedItem in AssociatedInstance.ContentPage.SelectedItems)
                {
                    App.SidebarPinnedController.Model.AddItem(listedItem.ItemPath);
                }
            }
        }

        public void UnpinItem_Click(object sender, RoutedEventArgs e)
        {
            if (AssociatedInstance.ContentPage != null)
            {
                foreach (ListedItem listedItem in AssociatedInstance.ContentPage.SelectedItems)
                {
                    App.SidebarPinnedController.Model.RemoveItem(listedItem.ItemPath);
                }
            }
        }

        public async Task InvokeWin32ComponentAsync(string applicationPath, string arguments = null, bool runAsAdmin = false, string workingDir = null)
        {
            await InvokeWin32ComponentsAsync(new List<string>() { applicationPath }, arguments, runAsAdmin, workingDir);
        }

        public async Task InvokeWin32ComponentsAsync(List<string> applicationPaths, string arguments = null, bool runAsAdmin = false, string workingDir = null)
        {
            Debug.WriteLine("Launching EXE in FullTrustProcess");
            if (Connection != null)
            {
                var value = new ValueSet
                {
                    { "WorkingDirectory", string.IsNullOrEmpty(workingDir) ? AssociatedInstance?.FilesystemViewModel?.WorkingDirectory : workingDir },
                    { "Application", applicationPaths.FirstOrDefault() },
                    { "ApplicationList", JsonConvert.SerializeObject(applicationPaths) },
                };

                if (runAsAdmin)
                {
                    value.Add("Arguments", "runas");
                }
                else
                {
                    value.Add("Arguments", arguments);
                }

                await Connection.SendMessageAsync(value);
            }
        }

        public async Task OpenShellCommandInExplorerAsync(string shellCommand)
        {
            Debug.WriteLine("Launching shell command in FullTrustProcess");
            if (Connection != null)
            {
                var value = new ValueSet();
                value.Add("ShellCommand", shellCommand);
                value.Add("Arguments", "ShellCommand");
                await Connection.SendMessageAsync(value);
            }
        }

        public async void GrantAccessPermissionHandler(IUICommand command)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess"));
        }

        public static bool IsAnyContentDialogOpen()
        {
            var openedPopups = VisualTreeHelper.GetOpenPopups(Window.Current);
            return openedPopups.Any(popup => popup.Child is ContentDialog);
        }

        public static T FindChild<T>(DependencyObject startNode) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(startNode);
            for (int i = 0; i < count; i++)
            {
                DependencyObject current = VisualTreeHelper.GetChild(startNode, i);
                if (current.GetType().Equals(typeof(T)) || current.GetType().GetTypeInfo().IsSubclassOf(typeof(T)))
                {
                    T asType = (T)current;
                    return asType;
                }
                var retVal = FindChild<T>(current);
                if (retVal != null)
                {
                    return retVal;
                }
            }
            return null;
        }

        public static void FindChildren<T>(IList<T> results, DependencyObject startNode) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(startNode);
            for (int i = 0; i < count; i++)
            {
                DependencyObject current = VisualTreeHelper.GetChild(startNode, i);
                if (current.GetType().Equals(typeof(T)) || (current.GetType().GetTypeInfo().IsSubclassOf(typeof(T))))
                {
                    T asType = (T)current;
                    results.Add(asType);
                }
                FindChildren<T>(results, current);
            }
        }

        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            T parent = null;
            DependencyObject CurrentParent = VisualTreeHelper.GetParent(child);
            while (CurrentParent != null)
            {
                if (CurrentParent is T)
                {
                    parent = (T)CurrentParent;
                    break;
                }
                CurrentParent = VisualTreeHelper.GetParent(CurrentParent);
            }
            return parent;
        }

        public static TEnum GetEnum<TEnum>(string text) where TEnum : struct
        {
            if (!typeof(TEnum).GetTypeInfo().IsEnum)
            {
                throw new InvalidOperationException("Generic parameter 'TEnum' must be an enum.");
            }
            return (TEnum)Enum.Parse(typeof(TEnum), text);
        }

        public async void RunAsAdmin_Click()
        {
            if (Connection != null)
            {
                await Connection.SendMessageAsync(new ValueSet()
                {
                    { "Arguments", "InvokeVerb" },
                    { "FilePath", AssociatedInstance.ContentPage.SelectedItem.ItemPath },
                    { "Verb", "runas" }
                });
            }
        }

        public async void RunAsAnotherUser_Click()
        {
            if (Connection != null)
            {
                await Connection.SendMessageAsync(new ValueSet()
                {
                    { "Arguments", "InvokeVerb" },
                    { "FilePath", AssociatedInstance.ContentPage.SelectedItem.ItemPath },
                    { "Verb", "runasuser" }
                });
            }
        }

        public void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectedItems(false);
        }

        public void OpenItemWithApplicationPicker_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectedItems(true);
        }

        public async void OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            var item = AssociatedInstance.ContentPage.SelectedItem as ShortcutItem;
            if (string.IsNullOrEmpty(item?.TargetPath))
            {
                return;
            }
            var folderPath = Path.GetDirectoryName(item.TargetPath);
            // Check if destination path exists
            var destFolder = await AssociatedInstance.FilesystemViewModel.GetFolderWithPathFromPathAsync(folderPath);
            if (destFolder)
            {
                AssociatedInstance.ContentFrame.Navigate(FolderSettings.GetLayoutType(folderPath), new NavigationArguments()
                {
                    NavPathParam = folderPath,
                    AssociatedTabInstance = AssociatedInstance
                });
            }
            else if (destFolder == FileSystemStatusCode.NotFound)
            {
                await DialogDisplayHelper.ShowDialogAsync("FileNotFoundDialog/Title".GetLocalized(), "FileNotFoundDialog/Text".GetLocalized());
            }
            else
            {
                await DialogDisplayHelper.ShowDialogAsync("InvalidItemDialogTitle".GetLocalized(),
                    string.Format("InvalidItemDialogContent".GetLocalized(), Environment.NewLine, destFolder.ErrorCode.ToString()));
            }
        }

        /// <summary>
        /// Navigates to a directory or opens file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="itemType"></param>
        /// <param name="openSilent">Determines whether history of opened item is saved (... to Recent Items/Windows Timeline/opening in background)</param>
        /// <param name="openViaApplicationPicker">Determines whether open file using application picker</param>
        public async Task<bool> OpenPath(string path, FilesystemItemType? itemType = null, bool openSilent = false, bool openViaApplicationPicker = false)
        // TODO: This function reliability has not been extensively tested
        {
            string previousDir = AssociatedInstance.FilesystemViewModel.WorkingDirectory;
            bool isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Hidden);
            bool isShortcutItem = path.EndsWith(".lnk") || path.EndsWith(".url"); // Determine
            FilesystemResult opened = (FilesystemResult)false;

            // Shortcut item variables
            string shortcutTargetPath = null;
            string shortcutArguments = null;
            string shortcutWorkingDirectory = null;
            bool shortcutRunAsAdmin = false;
            bool shortcutIsFolder = false;

            if (itemType == null || isShortcutItem || isHiddenItem)
            {
                if (isShortcutItem)
                {
                    AppServiceResponse response = await Connection.SendMessageAsync(new ValueSet()
                    {
                        { "Arguments", "FileOperation" },
                        { "fileop", "ParseLink" },
                        { "filepath", path }
                    });

                    if (response.Status == AppServiceResponseStatus.Success)
                    {
                        shortcutTargetPath = response.Message.Get("TargetPath", string.Empty);
                        shortcutArguments = response.Message.Get("Arguments", string.Empty);
                        shortcutWorkingDirectory = response.Message.Get("WorkingDirectory", string.Empty);
                        shortcutRunAsAdmin = response.Message.Get("RunAsAdmin", false);
                        shortcutIsFolder = response.Message.Get("IsFolder", false);

                        itemType = shortcutIsFolder ? FilesystemItemType.Directory : FilesystemItemType.File;
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
                    itemType = await StorageItemHelpers.GetTypeFromPath(path, AssociatedInstance);
                }
            }

            var mostRecentlyUsed = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;

            if (itemType == FilesystemItemType.Directory) // OpenDirectory
            {
                if (isShortcutItem)
                {
                    if (string.IsNullOrEmpty(shortcutTargetPath))
                    {
                        await InvokeWin32ComponentAsync(path);
                        return true;
                    }
                    else
                    {
                        AssociatedInstance.NavigationToolbar.PathControlDisplayText = shortcutTargetPath;
                        AssociatedInstance.ContentFrame.Navigate(AssociatedInstance.InstanceViewModel.FolderSettings.GetLayoutType(shortcutTargetPath), new NavigationArguments()
                        {
                            NavPathParam = shortcutTargetPath,
                            AssociatedTabInstance = AssociatedInstance
                        }, new SuppressNavigationTransitionInfo());

                        return true;
                    }
                }
                else if (isHiddenItem)
                {
                    AssociatedInstance.NavigationToolbar.PathControlDisplayText = path;
                    AssociatedInstance.ContentFrame.Navigate(AssociatedInstance.InstanceViewModel.FolderSettings.GetLayoutType(path), new NavigationArguments()
                    {
                        NavPathParam = path,
                        AssociatedTabInstance = AssociatedInstance
                    }, new SuppressNavigationTransitionInfo());

                    return true;
                }
                else
                {
                    opened = await AssociatedInstance.FilesystemViewModel.GetFolderWithPathFromPathAsync(path)
                        .OnSuccess(childFolder =>
                        {
                            // Add location to MRU List
                            mostRecentlyUsed.Add(childFolder.Folder, childFolder.Path);
                        });
                    if (!opened)
                    {
                        opened = (FilesystemResult)ItemViewModel.CheckFolderAccessWithWin32(path);
                    }
                    if (opened)
                    {
                        AssociatedInstance.NavigationToolbar.PathControlDisplayText = path;
                        AssociatedInstance.ContentFrame.Navigate(AssociatedInstance.InstanceViewModel.FolderSettings.GetLayoutType(path), new NavigationArguments()
                        {
                            NavPathParam = path,
                            AssociatedTabInstance = AssociatedInstance
                        }, new SuppressNavigationTransitionInfo());
                    }
                }
            }
            else if (itemType == FilesystemItemType.File) // OpenFile
            {
                if (isShortcutItem)
                {
                    if (string.IsNullOrEmpty(shortcutTargetPath))
                    {
                        await InvokeWin32ComponentAsync(path);
                    }
                    else
                    {
                        if (!path.EndsWith(".url"))
                        {
                            StorageFileWithPath childFile = await AssociatedInstance.FilesystemViewModel.GetFileWithPathFromPathAsync(shortcutTargetPath);
                            if (childFile != null)
                            {
                                // Add location to MRU List
                                mostRecentlyUsed.Add(childFile.File, childFile.Path);
                            }
                        }
                        await InvokeWin32ComponentAsync(shortcutTargetPath, shortcutArguments, shortcutRunAsAdmin, shortcutWorkingDirectory);
                    }
                    opened = (FilesystemResult)true;
                }
                else if (isHiddenItem)
                {
                    await InvokeWin32ComponentAsync(path);
                }
                else
                {
                    opened = await AssociatedInstance.FilesystemViewModel.GetFileWithPathFromPathAsync(path)
                        .OnSuccess(async childFile =>
                        {
                            // Add location to MRU List
                            mostRecentlyUsed.Add(childFile.File, childFile.Path);

                            if (openViaApplicationPicker)
                            {
                                LauncherOptions options = new LauncherOptions
                                {
                                    DisplayApplicationPicker = true
                                };
                                await Launcher.LaunchFileAsync(childFile.File, options);
                            }
                            else
                            {
                                //try using launcher first
                                bool launchSuccess = false;

                                StorageFileQueryResult fileQueryResult = null;

                                //Get folder to create a file query (to pass to apps like Photos, Movies & TV..., needed to scroll through the folder like what Windows Explorer does)
                                StorageFolder currFolder = await AssociatedInstance.FilesystemViewModel.GetFolderFromPathAsync(Path.GetDirectoryName(path));

                                if (currFolder != null)
                                {
                                    QueryOptions queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, null);

                                    //We can have many sort entries
                                    SortEntry sortEntry = new SortEntry()
                                    {
                                        AscendingOrder = FolderSettings.DirectorySortDirection == Microsoft.Toolkit.Uwp.UI.SortDirection.Ascending
                                    };

                                    //Basically we tell to the launched app to follow how we sorted the files in the directory.

                                    var sortOption = FolderSettings.DirectorySortOption;

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

                                    fileQueryResult = currFolder.CreateFileQueryWithOptions(queryOptions);

                                    var options = new LauncherOptions
                                    {
                                        NeighboringFilesQuery = fileQueryResult
                                    };

                                    // Now launch file with options.
                                    launchSuccess = await Launcher.LaunchFileAsync(childFile.File, options);
                                }

                                if (!launchSuccess)
                                {
                                    await InvokeWin32ComponentAsync(path);
                                }
                            }
                        });
                }
            }

            if (opened.ErrorCode == FileSystemStatusCode.NotFound && !openSilent)
            {
                await DialogDisplayHelper.ShowDialogAsync("FileNotFoundDialog/Title".GetLocalized(), "FileNotFoundDialog/Text".GetLocalized());
                AssociatedInstance.NavigationToolbar.CanRefresh = false;
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    var ContentOwnedViewModelInstance = AssociatedInstance.FilesystemViewModel;
                    ContentOwnedViewModelInstance?.RefreshItems(previousDir);
                });
            }

            return opened;
        }

        private async void OpenSelectedItems(bool openViaApplicationPicker = false)
        {
            if (AssociatedInstance.FilesystemViewModel.WorkingDirectory.StartsWith(AppSettings.RecycleBinPath))
            {
                // Do not open files and folders inside the recycle bin
                return;
            }

            foreach (ListedItem item in AssociatedInstance.ContentPage.SelectedItems)
            {
                var type = item.PrimaryItemAttribute == StorageItemTypes.Folder ?
                    FilesystemItemType.Directory : FilesystemItemType.File;
                await OpenPath(item.ItemPath, type, false, openViaApplicationPicker);
            }
        }

        public RelayCommand OpenNewWindow => new RelayCommand(() => LaunchNewWindow());

        public async void LaunchNewWindow()
        {
            var filesUWPUri = new Uri("files-uwp:");
            await Launcher.LaunchUriAsync(filesUWPUri);
        }

        public void ShareItem_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager manager = DataTransferManager.GetForCurrentView();
            manager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(Manager_DataRequested);
            DataTransferManager.ShowShareUI();
        }

        private async void ShowProperties()
        {
            if (AssociatedInstance.ContentPage.IsItemSelected)
            {
                if (AssociatedInstance.ContentPage.SelectedItems.Count > 1)
                {
                    await OpenPropertiesWindowAsync(AssociatedInstance.ContentPage.SelectedItems);
                }
                else
                {
                    await OpenPropertiesWindowAsync(AssociatedInstance.ContentPage.SelectedItem);
                }
            }
            else
            {
                if (!Path.GetPathRoot(AssociatedInstance.FilesystemViewModel.CurrentFolder.ItemPath)
                    .Equals(AssociatedInstance.FilesystemViewModel.CurrentFolder.ItemPath, StringComparison.OrdinalIgnoreCase))
                {
                    await OpenPropertiesWindowAsync(AssociatedInstance.FilesystemViewModel.CurrentFolder);
                }
                else
                {
                    await OpenPropertiesWindowAsync(App.DrivesManager.Drives
                        .SingleOrDefault(x => x.Path.Equals(AssociatedInstance.FilesystemViewModel.CurrentFolder.ItemPath)));
                }
            }
        }

        public async Task OpenPropertiesWindowAsync(object item)
        {
            if (item == null)
            {
                return;
            }
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                CoreApplicationView newWindow = CoreApplication.CreateNewView();
                ApplicationView newView = null;

                await newWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Frame frame = new Frame();
                    frame.Navigate(typeof(Properties), new PropertiesPageNavigationArguments()
                    {
                        Item = item,
                        AppInstanceArgument = AssociatedInstance
                    }, new SuppressNavigationTransitionInfo());
                    Window.Current.Content = frame;
                    Window.Current.Activate();

                    newView = ApplicationView.GetForCurrentView();
                    newWindow.TitleBar.ExtendViewIntoTitleBar = true;
                    newView.Title = "PropertiesTitle".GetLocalized();
                    newView.PersistedStateId = "Properties";
                    newView.SetPreferredMinSize(new Size(400, 550));
                    newView.Consolidated += delegate
                    {
                        Window.Current.Close();
                    };
                });
                bool viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newView.Id);
                // Set window size again here as sometimes it's not resized in the page Loaded event
                newView.TryResizeView(new Size(400, 550));
            }
            else
            {
                var propertiesDialog = new PropertiesDialog();
                propertiesDialog.propertiesFrame.Tag = propertiesDialog;
                propertiesDialog.propertiesFrame.Navigate(typeof(Properties), new PropertiesPageNavigationArguments()
                {
                    Item = item,
                    AppInstanceArgument = AssociatedInstance
                }, new SuppressNavigationTransitionInfo());
                await propertiesDialog.ShowAsync(ContentDialogPlacement.Popup);
            }
        }

        public void ShowPropertiesButton_Click(object sender, RoutedEventArgs e)
        {
            ShowProperties();
        }

        public void ShowFolderPropertiesButton_Click(object sender, RoutedEventArgs e)
        {
            ShowProperties();
        }

        public void PinDirectoryToSidebar(object sender, RoutedEventArgs e)
        {
            App.SidebarPinnedController.Model.AddItem(AssociatedInstance.FilesystemViewModel.WorkingDirectory);
        }

        public void UnpinDirectoryFromSidebar(object sender, RoutedEventArgs e)
        {
            App.SidebarPinnedController.Model.RemoveItem(AssociatedInstance.FilesystemViewModel.WorkingDirectory);
        }

        private async void Manager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequestDeferral dataRequestDeferral = args.Request.GetDeferral();
            List<IStorageItem> items = new List<IStorageItem>();
            DataRequest dataRequest = args.Request;

            /*dataRequest.Data.Properties.Title = "Data Shared From Files";
            dataRequest.Data.Properties.Description = "The items you selected will be shared";*/

            foreach (ListedItem item in AssociatedInstance.ContentPage.SelectedItems)
            {
                if (item.IsShortcutItem)
                {
                    if (item.IsLinkItem)
                    {
                        dataRequest.Data.Properties.Title = string.Format("ShareDialogTitle".GetLocalized(), items.First().Name);
                        dataRequest.Data.Properties.Description = "ShareDialogSingleItemDescription".GetLocalized();
                        dataRequest.Data.SetWebLink(new Uri(((ShortcutItem)item).TargetPath));
                        dataRequestDeferral.Complete();
                        return;
                    }
                }
                else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    await AssociatedInstance.FilesystemViewModel.GetFolderFromPathAsync(item.ItemPath)
                        .OnSuccess(folderAsItem => items.Add(folderAsItem));
                }
                else
                {
                    await AssociatedInstance.FilesystemViewModel.GetFileFromPathAsync(item.ItemPath)
                        .OnSuccess(fileAsItem => items.Add(fileAsItem));
                }
            }

            if (items.Count == 1)
            {
                dataRequest.Data.Properties.Title = string.Format("ShareDialogTitle".GetLocalized(), items.First().Name);
                dataRequest.Data.Properties.Description = "ShareDialogSingleItemDescription".GetLocalized();
            }
            else if (items.Count == 0)
            {
                dataRequest.FailWithDisplayText("ShareDialogFailMessage".GetLocalized());
                dataRequestDeferral.Complete();
                return;
            }
            else
            {
                dataRequest.Data.Properties.Title = string.Format("ShareDialogTitleMultipleItems".GetLocalized(), items.Count,
                    "ItemsCount.Text".GetLocalized());
                dataRequest.Data.Properties.Description = "ShareDialogMultipleItemsDescription".GetLocalized();
            }

            dataRequest.Data.SetStorageItems(items);
            dataRequestDeferral.Complete();
        }

        public async void CreateShortcutFromItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (ListedItem selectedItem in AssociatedInstance.ContentPage.SelectedItems)
            {
                if (Connection != null)
                {
                    var value = new ValueSet
                    {
                        { "Arguments", "FileOperation" },
                        { "fileop", "CreateLink" },
                        { "targetpath", selectedItem.ItemPath },
                        { "arguments", "" },
                        { "workingdir", "" },
                        { "runasadmin", false },
                        {
                            "filepath",
                            Path.Combine(AssociatedInstance.FilesystemViewModel.WorkingDirectory,
                                string.Format("ShortcutCreateNewSuffix".GetLocalized(), selectedItem.ItemName) + ".lnk")
                        }
                    };
                    await Connection.SendMessageAsync(value);
                }
            }
        }

        public async void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            await FilesystemHelpers.DeleteItemsAsync(
                AssociatedInstance.ContentPage.SelectedItems.Select((item) => StorageItemHelpers.FromPathAndType(
                    item.ItemPath,
                    item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory)).ToList(),
                true, false, true);
        }

        public void RenameItem_Click(object sender, RoutedEventArgs e)
        {
            if (AssociatedInstance.ContentPage.IsItemSelected)
            {
                AssociatedInstance.ContentPage.StartRenameItem();
            }
        }

        public async Task<bool> RenameFileItemAsync(ListedItem item, string oldName, string newName)
        {
            if (oldName == newName)
            {
                return true;
            }

            var renamed = ReturnResult.InProgress;
            if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
            {
                renamed = await FilesystemHelpers.RenameAsync(StorageItemHelpers.FromPathAndType(item.ItemPath, FilesystemItemType.Directory),
                    newName, NameCollisionOption.FailIfExists, true);
            }
            else
            {
                if (item.IsShortcutItem || !AppSettings.ShowFileExtensions)
                {
                    newName += item.FileExtension;
                }

                renamed = await FilesystemHelpers.RenameAsync(StorageItemHelpers.FromPathAndType(item.ItemPath, FilesystemItemType.File),
                    newName, NameCollisionOption.FailIfExists, true);
            }

            if (renamed == ReturnResult.Success)
            {
                AssociatedInstance.NavigationToolbar.CanGoForward = false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Set a single file or folder to hidden or unhidden an refresh the
        /// view after setting the flag
        /// </summary>
        /// <param name="item"></param>
        /// <param name="isHidden"></param>
        public void SetHiddenAttributeItem(ListedItem item, bool isHidden)
        {
            item.IsHiddenItem = isHidden;
            AssociatedInstance.ContentPage.ResetItemOpacity();
        }

        public async void RestoreItem_Click(object sender, RoutedEventArgs e)
        {
            if (AssociatedInstance.ContentPage.IsItemSelected)
            {
                foreach (ListedItem listedItem in AssociatedInstance.ContentPage.SelectedItems)
                {
                    if (listedItem is RecycleBinItem binItem)
                    {
                        FilesystemItemType itemType = binItem.PrimaryItemAttribute == StorageItemTypes.Folder ? FilesystemItemType.Directory : FilesystemItemType.File;
                        await FilesystemHelpers.RestoreFromTrashAsync(StorageItemHelpers.FromPathAndType(
                            (listedItem as RecycleBinItem).ItemPath,
                            itemType), (listedItem as RecycleBinItem).ItemOriginalPath, true);
                    }
                }
            }
        }

        public async void CutItem_Click(object sender, RoutedEventArgs e)
        {
            DataPackage dataPackage = new DataPackage
            {
                RequestedOperation = DataPackageOperation.Move
            };
            List<IStorageItem> items = new List<IStorageItem>();
            var cut = (FilesystemResult)false;
            if (AssociatedInstance.ContentPage.IsItemSelected)
            {
                // First, reset DataGrid Rows that may be in "cut" command mode
                AssociatedInstance.ContentPage.ResetItemOpacity();

                foreach (ListedItem listedItem in AssociatedInstance.ContentPage.SelectedItems)
                {
                    // Dim opacities accordingly
                    AssociatedInstance.ContentPage.SetItemOpacity(listedItem);

                    if (listedItem.PrimaryItemAttribute == StorageItemTypes.File)
                    {
                        cut = await AssociatedInstance.FilesystemViewModel.GetFileFromPathAsync(listedItem.ItemPath)
                            .OnSuccess(t => items.Add(t));
                        if (!cut)
                        {
                            break;
                        }
                    }
                    else
                    {
                        cut = await AssociatedInstance.FilesystemViewModel.GetFolderFromPathAsync(listedItem.ItemPath)
                            .OnSuccess(t => items.Add(t));
                        if (!cut)
                        {
                            break;
                        }
                    }
                }
                if (cut.ErrorCode == FileSystemStatusCode.NotFound)
                {
                    AssociatedInstance.ContentPage.ResetItemOpacity();
                    return;
                }
                else if (cut.ErrorCode == FileSystemStatusCode.Unauthorized)
                {
                    // Try again with fulltrust process
                    if (Connection != null)
                    {
                        var filePaths = string.Join('|', AssociatedInstance.ContentPage.SelectedItems.Select(x => x.ItemPath));
                        var result = await Connection.SendMessageAsync(new ValueSet()
                        {
                            { "Arguments", "FileOperation" },
                            { "fileop", "Clipboard" },
                            { "filepath", filePaths },
                            { "operation", (int)DataPackageOperation.Move }
                        });
                        if (result.Status == AppServiceResponseStatus.Success)
                        {
                            return;
                        }
                    }
                    AssociatedInstance.ContentPage.ResetItemOpacity();
                    return;
                }
            }
            if (!items.Any())
            {
                return;
            }
            dataPackage.SetStorageItems(items);
            try
            {
                Clipboard.SetContent(dataPackage);
                Clipboard.Flush();
            }
            catch
            {
                dataPackage = null;
            }
        }

        public string CopySourcePath;

        public async void CopyItem_ClickAsync(object sender, RoutedEventArgs e)
        {
            DataPackage dataPackage = new DataPackage
            {
                RequestedOperation = DataPackageOperation.Copy
            };
            List<IStorageItem> items = new List<IStorageItem>();

            CopySourcePath = AssociatedInstance.FilesystemViewModel.WorkingDirectory;
            var copied = (FilesystemResult)false;

            if (AssociatedInstance.ContentPage.IsItemSelected)
            {
                foreach (ListedItem listedItem in AssociatedInstance.ContentPage.SelectedItems)
                {
                    if (listedItem.PrimaryItemAttribute == StorageItemTypes.File)
                    {
                        copied = await AssociatedInstance.FilesystemViewModel.GetFileFromPathAsync(listedItem.ItemPath)
                            .OnSuccess(t => items.Add(t));
                        if (!copied)
                        {
                            break;
                        }
                    }
                    else
                    {
                        copied = await AssociatedInstance.FilesystemViewModel.GetFolderFromPathAsync(listedItem.ItemPath)
                            .OnSuccess(t => items.Add(t));
                        if (!copied)
                        {
                            break;
                        }
                    }
                }
                if (copied.ErrorCode == FileSystemStatusCode.Unauthorized)
                {
                    // Try again with fulltrust process
                    if (Connection != null)
                    {
                        var filePaths = string.Join('|', AssociatedInstance.ContentPage.SelectedItems.Select(x => x.ItemPath));
                        var result = await Connection.SendMessageAsync(new ValueSet()
                        {
                            { "Arguments", "FileOperation" },
                            { "fileop", "Clipboard" },
                            { "filepath", filePaths },
                            { "operation", (int)DataPackageOperation.Copy }
                        });
                    }
                    return;
                }
            }

            if (items?.Count > 0)
            {
                dataPackage.SetStorageItems(items);
                try
                {
                    Clipboard.SetContent(dataPackage);
                    Clipboard.Flush();
                }
                catch
                {
                    dataPackage = null;
                }
            }
        }

        public RelayCommand CopyPathOfSelectedItem => new RelayCommand(() => CopyLocation());

        private void CopyLocation()
        {
            try
            {
                if (AssociatedInstance.ContentPage != null)
                {
                    Clipboard.Clear();
                    DataPackage data = new DataPackage();
                    data.SetText(AssociatedInstance.ContentPage.SelectedItem.ItemPath);
                    Clipboard.SetContent(data);
                    Clipboard.Flush();
                }
            }
            catch
            {
            }
        }

        public RelayCommand CopyPathOfWorkingDirectory => new RelayCommand(() => CopyWorkingLocation());

        private void CopyWorkingLocation()
        {
            try
            {
                if (AssociatedInstance.ContentPage != null)
                {
                    Clipboard.Clear();
                    DataPackage data = new DataPackage();
                    data.SetText(AssociatedInstance.FilesystemViewModel.WorkingDirectory);
                    Clipboard.SetContent(data);
                    Clipboard.Flush();
                }
            }
            catch
            {
            }
        }

        private enum ImpossibleActionResponseTypes
        {
            Skip,
            Abort
        }

        public RelayCommand EmptyRecycleBin => new RelayCommand(() => EmptyRecycleBin_ClickAsync());

        public async void EmptyRecycleBin_ClickAsync()
        {
            var ConfirmEmptyBinDialog = new ContentDialog()
            {
                Title = "ConfirmEmptyBinDialogTitle".GetLocalized(),
                Content = "ConfirmEmptyBinDialogContent".GetLocalized(),
                PrimaryButtonText = "ConfirmEmptyBinDialog/PrimaryButtonText".GetLocalized(),
                SecondaryButtonText = "ConfirmEmptyBinDialog/SecondaryButtonText".GetLocalized()
            };

            ContentDialogResult result = await ConfirmEmptyBinDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                if (Connection != null)
                {
                    var value = new ValueSet();
                    value.Add("Arguments", "RecycleBin");
                    value.Add("action", "Empty");
                    // Send request to fulltrust process to empty recyclebin
                    await Connection.SendMessageAsync(value);
                }
            }
        }

        public RelayCommand PasteItemsFromClipboard => new RelayCommand(async () => await PasteItemAsync());

        public async Task PasteItemAsync()
        {
            DataPackageView packageView = await FilesystemTasks.Wrap(() => Task.FromResult(Clipboard.GetContent()));
            if (packageView != null)
            {
                string destinationPath = AssociatedInstance.FilesystemViewModel.WorkingDirectory;
                await FilesystemHelpers.PerformOperationTypeAsync(packageView.RequestedOperation, packageView, destinationPath, true);
                AssociatedInstance.ContentPage.ResetItemOpacity();
            }
        }

        public async void CreateFileFromDialogResultType(AddItemType itemType, ShellNewEntry itemInfo)
        {
            string currentPath = null;
            if (AssociatedInstance.ContentPage != null)
            {
                currentPath = AssociatedInstance.FilesystemViewModel.WorkingDirectory;
            }

            // Show rename dialog
            DynamicDialog dialog = DynamicDialogFactory.GetFor_RenameDialog();
            await dialog.ShowAsync();

            if (dialog.DynamicResult != DynamicDialogResult.Primary)
            {
                return;
            }

            // Create file based on dialog result
            string userInput = dialog.ViewModel.AdditionalData as string;
            var folderRes = await AssociatedInstance.FilesystemViewModel.GetFolderWithPathFromPathAsync(currentPath);
            FilesystemResult created = folderRes;
            if (folderRes)
            {
                switch (itemType)
                {
                    case AddItemType.Folder:
                        userInput = !string.IsNullOrWhiteSpace(userInput) ? userInput : "NewFolder".GetLocalized();
                        created = await FilesystemTasks.Wrap(async () =>
                        {
                            return await FilesystemHelpers.CreateAsync(
                                StorageItemHelpers.FromPathAndType(Path.Combine(folderRes.Result.Path, userInput), FilesystemItemType.Directory),
                                true);
                        });
                        break;

                    case AddItemType.File:
                        userInput = !string.IsNullOrWhiteSpace(userInput) ? userInput : itemInfo?.Name ?? "NewFile".GetLocalized();
                        created = await FilesystemTasks.Wrap(async () =>
                        {
                            return await FilesystemHelpers.CreateAsync(
                                StorageItemHelpers.FromPathAndType(Path.Combine(folderRes.Result.Path, userInput + itemInfo?.Extension), FilesystemItemType.File),
                                true);
                        });
                        break;
                }
            }
            if (created == FileSystemStatusCode.Unauthorized)
            {
                await DialogDisplayHelper.ShowDialogAsync("AccessDeniedCreateDialog/Title".GetLocalized(), "AccessDeniedCreateDialog/Text".GetLocalized());
            }
        }

        public RelayCommand CreateNewFolder => new RelayCommand(() => NewFolder());
        public RelayCommand<ShellNewEntry> CreateNewFile => new RelayCommand<ShellNewEntry>((itemType) => NewFile(itemType));

        private void NewFolder()
        {
            CreateFileFromDialogResultType(AddItemType.Folder, null);
        }

        private void NewFile(ShellNewEntry itemType)
        {
            CreateFileFromDialogResultType(AddItemType.File, itemType);
        }

        public RelayCommand SelectAllContentPageItems => new RelayCommand(() => SelectAllItems());

        public void SelectAllItems() => AssociatedInstance.ContentPage.SelectAllItems();

        public RelayCommand InvertContentPageSelction => new RelayCommand(() => InvertAllItems());

        public void InvertAllItems() => AssociatedInstance.ContentPage.InvertSelection();

        public RelayCommand ClearContentPageSelection => new RelayCommand(() => ClearAllItems());

        public void ClearAllItems() => AssociatedInstance.ContentPage.ClearSelection();

        public async void ToggleQuickLook()
        {
            try
            {
                if (AssociatedInstance.ContentPage.IsItemSelected && !AssociatedInstance.ContentPage.IsRenamingItem)
                {
                    var clickedOnItem = AssociatedInstance.ContentPage.SelectedItem;

                    Logger.Info("Toggle QuickLook");
                    Debug.WriteLine("Toggle QuickLook");
                    if (Connection != null)
                    {
                        var value = new ValueSet();
                        value.Add("path", clickedOnItem.ItemPath);
                        value.Add("Arguments", "ToggleQuickLook");
                        await Connection.SendMessageAsync(value);
                    }
                }
            }
            catch (FileNotFoundException)
            {
                await DialogDisplayHelper.ShowDialogAsync("FileNotFoundDialog/Title".GetLocalized(), "FileNotFoundPreviewDialog/Text".GetLocalized());
                AssociatedInstance.NavigationToolbar.CanRefresh = false;
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    var ContentOwnedViewModelInstance = AssociatedInstance.FilesystemViewModel;
                    ContentOwnedViewModelInstance?.RefreshItems(null);
                });
            }
        }

        public void PushJumpChar(char letter)
        {
            AssociatedInstance.FilesystemViewModel.JumpString += letter.ToString().ToLower();
        }

        public async Task<string> GetHashForFileAsync(ListedItem fileItem, string nameOfAlg, CancellationToken token, Microsoft.UI.Xaml.Controls.ProgressBar progress)
        {
            HashAlgorithmProvider algorithmProvider = HashAlgorithmProvider.OpenAlgorithm(nameOfAlg);
            StorageFile itemFromPath = await AssociatedInstance.FilesystemViewModel.GetFileFromPathAsync((fileItem as ShortcutItem)?.TargetPath ?? fileItem.ItemPath);
            if (itemFromPath == null)
            {
                return "";
            }

            Stream stream = await FilesystemTasks.Wrap(() => itemFromPath.OpenStreamForReadAsync());
            if (stream == null)
            {
                return "";
            }

            var inputStream = stream.AsInputStream();
            var str = inputStream.AsStreamForRead();
            var cap = (long)(0.5 * str.Length) / 100;
            uint capacity;
            if (cap >= uint.MaxValue)
            {
                capacity = uint.MaxValue;
            }
            else
            {
                capacity = Convert.ToUInt32(cap);
            }

            Windows.Storage.Streams.Buffer buffer = new Windows.Storage.Streams.Buffer(capacity);
            var hash = algorithmProvider.CreateHash();
            while (!token.IsCancellationRequested)
            {
                await inputStream.ReadAsync(buffer, capacity, InputStreamOptions.None);
                if (buffer.Length > 0)
                {
                    hash.Append(buffer);
                }
                else
                {
                    break;
                }
                if (progress != null)
                {
                    progress.Value = (double)str.Position / str.Length * 100;
                }
            }
            inputStream.Dispose();
            stream.Dispose();
            if (token.IsCancellationRequested)
            {
                return "";
            }
            return CryptographicBuffer.EncodeToHexString(hash.GetValueAndReset()).ToLower();
        }

        public static async Task EjectDeviceAsync(string path)
        {
            var removableDevice = new RemovableDevice(path);
            bool result = await removableDevice.EjectAsync();
            if (result)
            {
                Debug.WriteLine("Device successfully ejected");

                var toastContent = new ToastContent()
                {
                    Visual = new ToastVisual()
                    {
                        BindingGeneric = new ToastBindingGeneric()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = "EjectNotificationHeader".GetLocalized()
                                },
                                new AdaptiveText()
                                {
                                    Text = "EjectNotificationBody".GetLocalized()
                                }
                            },
                            Attribution = new ToastGenericAttributionText()
                            {
                                Text = "SettingsAboutAppName".GetLocalized()
                            }
                        }
                    },
                    ActivationType = ToastActivationType.Protocol
                };

                // Create the toast notification
                var toastNotif = new ToastNotification(toastContent.GetXml());

                // And send the notification
                ToastNotificationManager.CreateToastNotifier().Show(toastNotif);
            }
            else
            {
                Debug.WriteLine("Can't eject device");

                await DialogDisplayHelper.ShowDialogAsync(
                    "EjectNotificationErrorDialogHeader".GetLocalized(),
                    "EjectNotificationErrorDialogBody".GetLocalized());
            }
        }
    }
}