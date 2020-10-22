using Files.Commands;
using Files.Dialogs;
using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using Files.View_Models;
using Files.Views;
using Files.Views.Pages;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Files.Interacts
{
    public class Interaction
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IShellPage CurrentInstance;
        public SettingsViewModel AppSettings => App.AppSettings;

        public Interaction()
        {
            CurrentInstance = App.CurrentInstance;
        }

        public void List_ItemClick(object sender, DoubleTappedRoutedEventArgs e)
        {
            OpenSelectedItems(false);
        }

        public async void List_ItemPress(object sender, PointerRoutedEventArgs e)
        {
            // Skip code if the user right clicks an item
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                var properties = e.GetCurrentPoint((UIElement)sender).Properties;
                if (properties.IsRightButtonPressed)
                {
                    return;
                }
            }

            // Check if the setting to open items with a single click is turned on
            if (AppSettings.OpenItemsWithOneclick)
            {
                await Task.Delay(200); // The delay gives time for the item to be selected
                OpenSelectedItems(false);
            }
        }

        public async void ListItemSingleClick(object sender, ItemClickEventArgs e)
        {
            if (AppSettings.OpenItemsWithOneclick)
            {
                await Task.Delay(200); // The delay gives time for the item to be selected
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
                StorageFile sourceFile = await ItemViewModel.GetFileFromPathAsync(CurrentInstance.ContentPage.SelectedItem.ItemPath);

                // Get the app's local folder to use as the destination folder.
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;

                // the file to the destination folder.
                // Generate unique name if the file already exists.
                // If the file you are trying to set as the wallpaper has the same name as the current wallpaper,
                // the system will ignore the request and no-op the operation
                StorageFile file = await sourceFile.CopyAsync(localFolder, sourceFile.Name, NameCollisionOption.GenerateUniqueName);

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

        public async void OpenNewTab()
        {
            await MainPage.AddNewTab(typeof(ModernShellPage), ResourceController.GetTranslation("NewTab"));
        }

        public async void OpenInNewWindowItem_Click(object sender, RoutedEventArgs e)
        {
            var items = CurrentInstance.ContentPage.SelectedItems;
            foreach (ListedItem listedItem in items)
            {
                var selectedItemPath = (listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath;
                var folderUri = new Uri("files-uwp:" + "?folder=" + @selectedItemPath);
                await Launcher.LaunchUriAsync(folderUri);
            }
        }

        public async void OpenDirectoryInNewTab_Click(object sender, RoutedEventArgs e)
        {
            foreach (ListedItem listedItem in CurrentInstance.ContentPage.SelectedItems)
            {
                await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
                {
                    await MainPage.AddNewTab(typeof(ModernShellPage), (listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath);
                });
            }
        }

        public async void OpenPathInNewTab(string path)
        {
            await MainPage.AddNewTab(typeof(ModernShellPage), path);
        }

        public static async Task<bool> OpenPathInNewWindow(string path)
        {
            var folderUri = new Uri("files-uwp:" + "?folder=" + path);
            return await Launcher.LaunchUriAsync(folderUri);
        }

        public async void OpenDirectoryInTerminal(object sender, RoutedEventArgs e)
        {
            var terminal = AppSettings.TerminalController.Model.GetDefaultTerminal();

            if (App.Connection != null)
            {
                var value = new ValueSet
                {
                    { "WorkingDirectory", CurrentInstance.FilesystemViewModel.WorkingDirectory },
                    { "Application", terminal.Path },
                    { "Arguments", string.Format(terminal.Arguments,
                       Helpers.PathNormalization.NormalizePath(CurrentInstance.FilesystemViewModel.WorkingDirectory)) }
                };
                await App.Connection.SendMessageAsync(value);
            }
        }

        public void PinItem_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentInstance.ContentPage != null)
            {
                foreach (ListedItem listedItem in CurrentInstance.ContentPage.SelectedItems)
                {
                    App.SidebarPinnedController.Model.AddItem(listedItem.ItemPath);
                }
            }
        }

        public void GetPath_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentInstance.ContentPage != null)
            {
                Clipboard.Clear();
                DataPackage data = new DataPackage();
                data.SetText(CurrentInstance.FilesystemViewModel.WorkingDirectory);
                Clipboard.SetContent(data);
                Clipboard.Flush();
            }
        }

        public static async Task InvokeWin32Component(string applicationPath, string arguments = null, bool runAsAdmin = false, string workingDir = null)
        {
            await InvokeWin32Components(new List<string>() { applicationPath }, arguments, runAsAdmin, workingDir);
        }

        public static async Task InvokeWin32Components(List<string> applicationPaths, string arguments = null, bool runAsAdmin = false, string workingDir = null)
        {
            Debug.WriteLine("Launching EXE in FullTrustProcess");
            if (App.Connection != null)
            {
                var value = new ValueSet
                {
                    { "WorkingDirectory", string.IsNullOrEmpty(workingDir) ? App.CurrentInstance?.FilesystemViewModel?.WorkingDirectory : workingDir },
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

                await App.Connection.SendMessageAsync(value);
            }
        }

        public static async Task OpenShellCommandInExplorer(string shellCommand)
        {
            Debug.WriteLine("Launching shell command in FullTrustProcess");
            if (App.Connection != null)
            {
                var value = new ValueSet();
                value.Add("ShellCommand", shellCommand);
                value.Add("Arguments", "ShellCommand");
                await App.Connection.SendMessageAsync(value);
            }
        }

        public async void GrantAccessPermissionHandler(IUICommand command)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess"));
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
                if ((current.GetType()).Equals(typeof(T)) || (current.GetType().GetTypeInfo().IsSubclassOf(typeof(T))))
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
            if (App.Connection != null)
            {
                await App.Connection.SendMessageAsync(new ValueSet() {
                    { "Arguments", "InvokeVerb" },
                    { "FilePath", CurrentInstance.ContentPage.SelectedItem.ItemPath },
                    { "Verb", "runas" } });
            }
        }

        public async void RunAsAnotherUser_Click()
        {
            if (App.Connection != null)
            {
                await App.Connection.SendMessageAsync(new ValueSet() {
                    { "Arguments", "InvokeVerb" },
                    { "FilePath", CurrentInstance.ContentPage.SelectedItem.ItemPath },
                    { "Verb", "runasuser" } });
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
            var item = CurrentInstance.ContentPage.SelectedItem as ShortcutItem;
            try
            {
                var folderPath = Path.GetDirectoryName(item.TargetPath);
                // Check if destination path exists
                var destFolder = await ItemViewModel.GetFolderWithPathFromPathAsync(folderPath);
                App.CurrentInstance.ContentFrame.Navigate(AppSettings.GetLayoutType(), folderPath);
            }
            catch (FileNotFoundException)
            {
                await DialogDisplayHelper.ShowDialog(ResourceController.GetTranslation("FileNotFoundDialog/Title"), ResourceController.GetTranslation("FileNotFoundDialog/Text"));
            }
            catch (Exception ex)
            {
                await DialogDisplayHelper.ShowDialog(ResourceController.GetTranslation("InvalidItemDialogTitle"),
                    string.Format(ResourceController.GetTranslation("InvalidItemDialogContent"), Environment.NewLine, ex.Message));
            }
        }

        private async void OpenSelectedItems(bool displayApplicationPicker)
        {
            if (CurrentInstance.FilesystemViewModel.WorkingDirectory.StartsWith(AppSettings.RecycleBinPath))
            {
                // Do not open files and folders inside the recycle bin
                return;
            }

            try
            {
                int selectedItemCount;
                Type sourcePageType = App.CurrentInstance.CurrentPageType;
                selectedItemCount = CurrentInstance.ContentPage.SelectedItems.Count;

                // Access MRU List
                var mostRecentlyUsed = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;

                if (selectedItemCount == 1)
                {
                    var clickedOnItem = CurrentInstance.ContentPage.SelectedItem;
                    var clickedOnItemPath = clickedOnItem.ItemPath;
                    if (clickedOnItem.PrimaryItemAttribute == StorageItemTypes.Folder)
                    {
                        var childFolder = await ItemViewModel.GetFolderWithPathFromPathAsync(
                            (clickedOnItem as ShortcutItem)?.TargetPath ?? clickedOnItem.ItemPath);

                        // Add location to MRU List
                        mostRecentlyUsed.Add(childFolder.Folder, childFolder.Path);

                        await App.CurrentInstance.FilesystemViewModel.SetWorkingDirectory(childFolder.Path);
                        CurrentInstance.NavigationToolbar.PathControlDisplayText = childFolder.Path;

                        CurrentInstance.ContentPage.AssociatedViewModel.IsFolderEmptyTextDisplayed = false;
                        CurrentInstance.ContentFrame.Navigate(sourcePageType, childFolder.Path, new SuppressNavigationTransitionInfo());
                    }
                    else if (clickedOnItem.IsShortcutItem)
                    {
                        var shortcutItem = (ShortcutItem)clickedOnItem;
                        if (string.IsNullOrEmpty(shortcutItem.TargetPath))
                        {
                            await InvokeWin32Component(shortcutItem.ItemPath);
                        }
                        else
                        {
                            if (!shortcutItem.IsUrl)
                            {
                                var childFile = await ItemViewModel.GetFileWithPathFromPathAsync(shortcutItem.TargetPath);
                                // Add location to MRU List
                                mostRecentlyUsed.Add(childFile.File, childFile.Path);
                            }
                            await InvokeWin32Component(shortcutItem.TargetPath, shortcutItem.Arguments, shortcutItem.RunAsAdmin, shortcutItem.WorkingDirectory);
                        }
                    }
                    else
                    {
                        var childFile = await ItemViewModel.GetFileWithPathFromPathAsync(clickedOnItem.ItemPath);
                        // Add location to MRU List
                        mostRecentlyUsed.Add(childFile.File, childFile.Path);

                        if (displayApplicationPicker)
                        {
                            var options = new LauncherOptions
                            {
                                DisplayApplicationPicker = true
                            };
                            await Launcher.LaunchFileAsync(childFile.File, options);
                        }
                        else
                        {
                            //try using launcher first
                            bool launchSuccess = false;

                            try
                            {
                                StorageFileQueryResult fileQueryResult = null;

                                //Get folder to create a file query (to pass to apps like Photos, Movies & TV..., needed to scroll through the folder like what Windows Explorer does)
                                StorageFolder currFolder = await ItemViewModel.GetFolderFromPathAsync(Path.GetDirectoryName(clickedOnItem.ItemPath));

                                QueryOptions queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, null);

                                //We can have many sort entries
                                SortEntry sortEntry = new SortEntry()
                                {
                                    AscendingOrder = AppSettings.DirectorySortDirection == Microsoft.Toolkit.Uwp.UI.SortDirection.Ascending,
                                };

                                var sortOption = AppSettings.DirectorySortOption;

                                switch (sortOption)
                                {
                                    case Enums.SortOption.Name:
                                        sortEntry.PropertyName = "System.ItemNameDisplay";
                                        queryOptions.SortOrder.Clear();
                                        break;

                                    case Enums.SortOption.DateModified:
                                        sortEntry.PropertyName = "System.DateModified";
                                        queryOptions.SortOrder.Clear();
                                        break;

                                    case Enums.SortOption.Size:
                                        //Unfortunately this is unsupported | Remarks: https://docs.microsoft.com/en-us/uwp/api/windows.storage.search.queryoptions.sortorder?view=winrt-19041

                                        //sortEntry.PropertyName = "System.TotalFileSize";
                                        //queryOptions.SortOrder.Clear();
                                        break;

                                    case Enums.SortOption.FileType:
                                        //Unfortunately this is unsupported | Remarks: https://docs.microsoft.com/en-us/uwp/api/windows.storage.search.queryoptions.sortorder?view=winrt-19041

                                        //sortEntry.PropertyName = "System.FileExtension";
                                        //queryOptions.SortOrder.Clear();
                                        break;

                                    default:
                                        //keep the default one in SortOrder IList
                                        break;
                                }

                                //Basically we tell to the launched app to follow how we sorted the files in the directory.
                                queryOptions.SortOrder.Add(sortEntry);

                                fileQueryResult = currFolder.CreateFileQueryWithOptions(queryOptions);

                                var options = new LauncherOptions
                                {
                                    NeighboringFilesQuery = fileQueryResult
                                };

                                //Now launch file with options.
                                launchSuccess = await Launcher.LaunchFileAsync(childFile.File, options);
                            }
                            catch (Exception ex)
                            {
                                //well...
                                Debug.WriteLine("Error in Interaction\\OpenSelectedItems() || " + ex.Message);
                            }

                            if (!launchSuccess)
                                await InvokeWin32Component(clickedOnItem.ItemPath);
                        }
                    }
                }
                else if (selectedItemCount > 1)
                {
                    foreach (ListedItem clickedOnItem in CurrentInstance.ContentPage.SelectedItems.Where(x => x.PrimaryItemAttribute == StorageItemTypes.Folder))
                    {
                        await MainPage.AddNewTab(typeof(ModernShellPage), (clickedOnItem as ShortcutItem)?.TargetPath ?? clickedOnItem.ItemPath);
                    }
                    foreach (ListedItem clickedOnItem in CurrentInstance.ContentPage.SelectedItems.Where(x => x.PrimaryItemAttribute == StorageItemTypes.File
                        && !x.IsShortcutItem))
                    {
                        var childFile = await ItemViewModel.GetFileWithPathFromPathAsync(clickedOnItem.ItemPath);
                        // Add location to MRU List
                        mostRecentlyUsed.Add(childFile.File, childFile.Path);

                        if (displayApplicationPicker)
                        {
                            var options = new LauncherOptions
                            {
                                DisplayApplicationPicker = true
                            };
                            await Launcher.LaunchFileAsync(childFile.File, options);
                        }
                    }
                    if (!displayApplicationPicker)
                    {
                        var applicationPath = string.Join('|', CurrentInstance.ContentPage.SelectedItems.Where(x => x.PrimaryItemAttribute == StorageItemTypes.File).Select(x => x.ItemPath));
                        await InvokeWin32Component(applicationPath);
                    }
                }
            }
            catch (FileNotFoundException)
            {
                await DialogDisplayHelper.ShowDialog(ResourceController.GetTranslation("FileNotFoundDialog/Title"), ResourceController.GetTranslation("FileNotFoundDialog/Text"));
                NavigationActions.Refresh_Click(null, null);
            }
        }

        public void CloseTab()
        {
            if (App.MultitaskingControl.Items.Count == 1)
            {
                App.CloseApp();
            }
            else if (App.MultitaskingControl.Items.Count > 1)
            {
                App.MultitaskingControl.Items.RemoveAt(App.InteractionViewModel.TabStripSelectedIndex);
            }
        }

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
            if (App.CurrentInstance.ContentPage.IsItemSelected)
            {
                if (App.CurrentInstance.ContentPage.SelectedItems.Count > 1)
                {
                    await OpenPropertiesWindow(App.CurrentInstance.ContentPage.SelectedItems);
                }
                else
                {
                    await OpenPropertiesWindow(CurrentInstance.ContentPage.SelectedItem);
                }
            }
            else
            {
                if (!Path.GetPathRoot(App.CurrentInstance.FilesystemViewModel.CurrentFolder.ItemPath)
                    .Equals(App.CurrentInstance.FilesystemViewModel.CurrentFolder.ItemPath, StringComparison.OrdinalIgnoreCase))
                {
                    await OpenPropertiesWindow(App.CurrentInstance.FilesystemViewModel.CurrentFolder);
                }
                else
                {
                    await OpenPropertiesWindow(App.AppSettings.DrivesManager.Drives
                        .Single(x => x.Path.Equals(App.CurrentInstance.FilesystemViewModel.CurrentFolder.ItemPath)));
                }
            }
        }

        public async Task OpenPropertiesWindow(object item)
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                CoreApplicationView newWindow = CoreApplication.CreateNewView();
                ApplicationView newView = null;

                await newWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Frame frame = new Frame();
                    frame.Navigate(typeof(Properties), item, new SuppressNavigationTransitionInfo());
                    Window.Current.Content = frame;
                    Window.Current.Activate();

                    newView = ApplicationView.GetForCurrentView();
                    newWindow.TitleBar.ExtendViewIntoTitleBar = true;
                    newView.Title = ResourceController.GetTranslation("PropertiesTitle");
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
                propertiesDialog.propertiesFrame.Navigate(typeof(Properties), item, new SuppressNavigationTransitionInfo());
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
            App.SidebarPinnedController.Model.AddItem(CurrentInstance.FilesystemViewModel.WorkingDirectory);
        }

        private async void Manager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequestDeferral dataRequestDeferral = args.Request.GetDeferral();
            List<IStorageItem> items = new List<IStorageItem>();
            DataRequest dataRequest = args.Request;

            /*dataRequest.Data.Properties.Title = "Data Shared From Files";
            dataRequest.Data.Properties.Description = "The items you selected will be shared";*/

            foreach (ListedItem item in CurrentInstance.ContentPage.SelectedItems)
            {
                if (item.IsShortcutItem)
                {
                    if (item.IsLinkItem)
                    {
                        dataRequest.Data.Properties.Title = string.Format(ResourceController.GetTranslation("ShareDialogTitle"), items.First().Name);
                        dataRequest.Data.Properties.Description = ResourceController.GetTranslation("ShareDialogSingleItemDescription");
                        dataRequest.Data.SetWebLink(new Uri(((ShortcutItem)item).TargetPath));
                        dataRequestDeferral.Complete();
                        return;
                    }
                }
                else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    var folderAsItem = await ItemViewModel.GetFolderFromPathAsync(item.ItemPath);
                    items.Add(folderAsItem);
                }
                else
                {
                    var fileAsItem = await ItemViewModel.GetFileFromPathAsync(item.ItemPath);
                    items.Add(fileAsItem);
                }
            }

            if (items.Count == 1)
            {
                dataRequest.Data.Properties.Title = string.Format(ResourceController.GetTranslation("ShareDialogTitle"), items.First().Name);
                dataRequest.Data.Properties.Description = ResourceController.GetTranslation("ShareDialogSingleItemDescription");
            }
            else if (items.Count == 0)
            {
                dataRequest.FailWithDisplayText(ResourceController.GetTranslation("ShareDialogFailMessage"));
                dataRequestDeferral.Complete();
                return;
            }
            else
            {
                dataRequest.Data.Properties.Title = string.Format(ResourceController.GetTranslation("ShareDialogTitleMultipleItems"), items.Count,
                    ResourceController.GetTranslation("ItemsCount.Text"));
                dataRequest.Data.Properties.Description = ResourceController.GetTranslation("ShareDialogMultipleItemsDescription");
            }

            dataRequest.Data.SetStorageItems(items);
            dataRequestDeferral.Complete();
        }

        public async void CreateShortcutFromItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (ListedItem selectedItem in CurrentInstance.ContentPage.SelectedItems)
            {
                if (App.Connection != null)
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
                            Path.Combine(CurrentInstance.FilesystemViewModel.WorkingDirectory,
                                string.Format(ResourceController.GetTranslation("ShortcutCreateNewSuffix"), selectedItem.ItemName) + ".lnk")
                        }
                    };
                    await App.Connection.SendMessageAsync(value);
                }
            }
        }

        public void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            ItemOperations.DeleteItemWithStatus(StorageDeleteOption.Default);
        }

        public void RenameItem_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentInstance.ContentPage.IsItemSelected)
            {
                App.CurrentInstance.ContentPage.StartRenameItem();
            }
        }

        public bool ContainsRestrictedCharacters(string input)
        {
            Regex regex = new Regex("\\\\|\\/|\\:|\\*|\\?|\\\"|\\<|\\>|\\|"); //restricted symbols for file names
            MatchCollection matches = regex.Matches(input);
            if (matches.Count > 0)
            {
                return true;
            }
            return false;
        }

        private static readonly List<string> RestrictedFileNames = new List<string>()
        {
                "CON", "PRN", "AUX",
                "NUL", "COM1", "COM2",
                "COM3", "COM4", "COM5",
                "COM6", "COM7", "COM8",
                "COM9", "LPT1", "LPT2",
                "LPT3", "LPT4", "LPT5",
                "LPT6", "LPT7", "LPT8", "LPT9"
        };

        public bool ContainsRestrictedFileName(string input)
        {
            foreach (var name in RestrictedFileNames)
            {
                Regex regex = new Regex($"^{name}($|\\.)(.+)?");
                MatchCollection matches = regex.Matches(input.ToUpper());
                if (matches.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<bool> RenameFileItem(ListedItem item, string oldName, string newName)
        {
            if (oldName == newName)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(newName)
                && !ContainsRestrictedCharacters(newName)
                && !ContainsRestrictedFileName(newName))
            {
                try
                {
                    if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                    {
                        var folder = await ItemViewModel.GetFolderFromPathAsync(item.ItemPath);
                        await folder.RenameAsync(newName, NameCollisionOption.FailIfExists);
                    }
                    else
                    {
                        var file = await ItemViewModel.GetFileFromPathAsync(item.ItemPath);
                        await file.RenameAsync(newName, NameCollisionOption.FailIfExists);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Try again with MoveFileFromApp
                    if (!NativeDirectoryChangesHelper.MoveFileFromApp(item.ItemPath, Path.Combine(Path.GetDirectoryName(item.ItemPath), newName)))
                    {
                        Debug.WriteLine(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is ArgumentException)
                    {
                        await DialogDisplayHelper.ShowDialog(ResourceController.GetTranslation("RenameError.NameInvalid.Title"), ResourceController.GetTranslation("RenameError.NameInvalid.Text"));
                    }
                    else if (ex is PathTooLongException)
                    {
                        await DialogDisplayHelper.ShowDialog(ResourceController.GetTranslation("RenameError.TooLong.Title"), ResourceController.GetTranslation("RenameError.TooLong.Text"));
                    }
                    else if (ex is FileNotFoundException)
                    {
                        await DialogDisplayHelper.ShowDialog(ResourceController.GetTranslation("RenameError.ItemDeleted.Title"), ResourceController.GetTranslation("RenameError.ItemDeleted.Text"));
                    }
                    else
                    {
                        var ItemAlreadyExistsDialog = new ContentDialog()
                        {
                            Title = ResourceController.GetTranslation("ItemAlreadyExistsDialogTitle"),
                            Content = ResourceController.GetTranslation("ItemAlreadyExistsDialogContent"),
                            PrimaryButtonText = ResourceController.GetTranslation("ItemAlreadyExistsDialogPrimaryButtonText"),
                            SecondaryButtonText = ResourceController.GetTranslation("ItemAlreadyExistsDialogSecondaryButtonText")
                        };

                        ContentDialogResult result = await ItemAlreadyExistsDialog.ShowAsync();

                        if (result == ContentDialogResult.Primary)
                        {
                            if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                            {
                                var folder = await ItemViewModel.GetFolderFromPathAsync(item.ItemPath);

                                await folder.RenameAsync(newName, NameCollisionOption.GenerateUniqueName);

                                App.JumpList.RemoveFolder(folder.Path);
                            }
                            else
                            {
                                var file = await ItemViewModel.GetFileFromPathAsync(item.ItemPath);

                                await file.RenameAsync(newName, NameCollisionOption.GenerateUniqueName);
                            }
                        }
                        else if (result == ContentDialogResult.Secondary)
                        {
                            if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                            {
                                var folder = await ItemViewModel.GetFolderFromPathAsync(item.ItemPath);

                                await folder.RenameAsync(newName, NameCollisionOption.ReplaceExisting);

                                App.JumpList.RemoveFolder(folder.Path);
                            }
                            else
                            {
                                var file = await ItemViewModel.GetFileFromPathAsync(item.ItemPath);

                                await file.RenameAsync(newName, NameCollisionOption.ReplaceExisting);
                            }
                        }
                    }
                }
            }
            else
            {
                return false;
            }

            CurrentInstance.NavigationToolbar.CanGoForward = false;
            return true;
        }

        public async void RestoreItem_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentInstance.ContentPage.IsItemSelected)
            {
                foreach (ListedItem listedItem in App.CurrentInstance.ContentPage.SelectedItems)
                {
                    try
                    {
                        var recycleBinItem = listedItem as RecycleBinItem;
                        if (listedItem.PrimaryItemAttribute == StorageItemTypes.Folder)
                        {
                            StorageFolder sourceFolder = await ItemViewModel.GetFolderFromPathAsync(recycleBinItem.ItemPath);
                            StorageFolder destFolder = await ItemViewModel.GetFolderFromPathAsync(Path.GetDirectoryName(recycleBinItem.ItemOriginalPath));
                            await MoveDirectoryAsync(sourceFolder, destFolder, recycleBinItem.ItemName);
                            await sourceFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
                        }
                        else
                        {
                            var file = await ItemViewModel.GetFileFromPathAsync(recycleBinItem.ItemPath);
                            var destinationFolder = await ItemViewModel.GetFolderFromPathAsync(Path.GetDirectoryName(recycleBinItem.ItemOriginalPath));
                            await file.MoveAsync(destinationFolder, Path.GetFileName(recycleBinItem.ItemOriginalPath), NameCollisionOption.GenerateUniqueName);
                        }
                        // Recycle bin also stores a file starting with $I for each item
                        var iFilePath = Path.Combine(Path.GetDirectoryName(recycleBinItem.ItemPath), Path.GetFileName(recycleBinItem.ItemPath).Replace("$R", "$I"));
                        await (await ItemViewModel.GetFileFromPathAsync(iFilePath)).DeleteAsync(StorageDeleteOption.PermanentDelete);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        await DialogDisplayHelper.ShowDialog(ResourceController.GetTranslation("AccessDeniedDeleteDialog/Title"), ResourceController.GetTranslation("AccessDeniedDeleteDialog/Text"));
                    }
                    catch (FileNotFoundException)
                    {
                        await DialogDisplayHelper.ShowDialog(ResourceController.GetTranslation("FileNotFoundDialog/Title"), ResourceController.GetTranslation("FileNotFoundDialog/Text"));
                    }
                    catch (Exception)
                    {
                        await DialogDisplayHelper.ShowDialog(ResourceController.GetTranslation("ItemAlreadyExistsDialogTitle"), ResourceController.GetTranslation("ItemAlreadyExistsDialogContent"));
                    }
                }
            }
        }

        public async Task<StorageFolder> MoveDirectoryAsync(StorageFolder SourceFolder, StorageFolder DestinationFolder, string sourceRootName)
        {
            var createdRoot = await DestinationFolder.CreateFolderAsync(sourceRootName, CreationCollisionOption.FailIfExists);
            DestinationFolder = createdRoot;

            foreach (StorageFile fileInSourceDir in await SourceFolder.GetFilesAsync())
            {
                await fileInSourceDir.MoveAsync(DestinationFolder, fileInSourceDir.Name, NameCollisionOption.FailIfExists);
            }
            foreach (StorageFolder folderinSourceDir in await SourceFolder.GetFoldersAsync())
            {
                await MoveDirectoryAsync(folderinSourceDir, DestinationFolder, folderinSourceDir.Name);
            }

            App.JumpList.RemoveFolder(SourceFolder.Path);

            return createdRoot;
        }

        public async void CutItem_Click(object sender, RoutedEventArgs e)
        {
            DataPackage dataPackage = new DataPackage
            {
                RequestedOperation = DataPackageOperation.Move
            };
            List<IStorageItem> items = new List<IStorageItem>();
            var CurrentInstance = App.CurrentInstance;
            if (CurrentInstance.ContentPage.IsItemSelected)
            {
                // First, reset DataGrid Rows that may be in "cut" command mode
                CurrentInstance.ContentPage.ResetItemOpacity();

                try
                {
                    foreach (ListedItem listedItem in CurrentInstance.ContentPage.SelectedItems)
                    {
                        // Dim opacities accordingly
                        CurrentInstance.ContentPage.SetItemOpacity(listedItem);

                        if (listedItem.PrimaryItemAttribute == StorageItemTypes.File)
                        {
                            var item = await ItemViewModel.GetFileFromPathAsync(listedItem.ItemPath);
                            items.Add(item);
                        }
                        else
                        {
                            var item = await ItemViewModel.GetFolderFromPathAsync(listedItem.ItemPath);
                            items.Add(item);
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    CurrentInstance.ContentPage.ResetItemOpacity();
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    // Try again with fulltrust process
                    if (App.Connection != null)
                    {
                        var filePaths = string.Join('|', CurrentInstance.ContentPage.SelectedItems.Select(x => x.ItemPath));
                        var result = await App.Connection.SendMessageAsync(new ValueSet() {
                            { "Arguments", "FileOperation" },
                            { "fileop", "Clipboard" },
                            { "filepath", filePaths },
                            { "operation", (int)DataPackageOperation.Move } });
                        if (result.Status == AppServiceResponseStatus.Success)
                        {
                            return;
                        }
                    }
                    CurrentInstance.ContentPage.ResetItemOpacity();
                    return;
                }
            }
            if (!items.Any())
            {
                return;
            }
            dataPackage.SetStorageItems(items);
            Clipboard.SetContent(dataPackage);
            try
            {
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

            CopySourcePath = App.CurrentInstance.FilesystemViewModel.WorkingDirectory;

            if (App.CurrentInstance.ContentPage.IsItemSelected)
            {
                try
                {
                    foreach (ListedItem listedItem in App.CurrentInstance.ContentPage.SelectedItems)
                    {
                        if (listedItem.PrimaryItemAttribute == StorageItemTypes.File)
                        {
                            var item = await ItemViewModel.GetFileFromPathAsync(listedItem.ItemPath);
                            items.Add(item);
                        }
                        else
                        {
                            var item = await ItemViewModel.GetFolderFromPathAsync(listedItem.ItemPath);
                            items.Add(item);
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Try again with fulltrust process
                    if (App.Connection != null)
                    {
                        var filePaths = string.Join('|', CurrentInstance.ContentPage.SelectedItems.Select(x => x.ItemPath));
                        var result = await App.Connection.SendMessageAsync(new ValueSet() {
                            { "Arguments", "FileOperation" },
                            { "fileop", "Clipboard" },
                            { "filepath", filePaths },
                            { "operation", (int)DataPackageOperation.Copy } });
                    }
                    return;
                }
            }

            if (items?.Count > 0)
            {
                dataPackage.SetStorageItems(items);
                Clipboard.SetContent(dataPackage);
                try
                {
                    Clipboard.Flush();
                }
                catch
                {
                    dataPackage = null;
                }
            }
        }

        public void CopyLocation_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (App.CurrentInstance.ContentPage != null)
            {
                Clipboard.Clear();
                DataPackage data = new DataPackage();
                data.SetText(CurrentInstance.ContentPage.SelectedItem.ItemPath);
                Clipboard.SetContent(data);
                Clipboard.Flush();
            }
        }

        private enum ImpossibleActionResponseTypes
        {
            Skip,
            Abort
        }

        public async void EmptyRecycleBin_ClickAsync(object sender, RoutedEventArgs e)
        {
            var ConfirmEmptyBinDialog = new ContentDialog()
            {
                Title = ResourceController.GetTranslation("ConfirmEmptyBinDialogTitle"),
                Content = ResourceController.GetTranslation("ConfirmEmptyBinDialogContent"),
                PrimaryButtonText = ResourceController.GetTranslation("ConfirmEmptyBinDialog/PrimaryButtonText"),
                SecondaryButtonText = ResourceController.GetTranslation("ConfirmEmptyBinDialog/SecondaryButtonText")
            };

            ContentDialogResult result = await ConfirmEmptyBinDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                if (App.Connection != null)
                {
                    var value = new ValueSet();
                    value.Add("Arguments", "RecycleBin");
                    value.Add("action", "Empty");
                    // Send request to fulltrust process to empty recyclebin
                    await App.Connection.SendMessageAsync(value);
                }
            }
        }

        public void PasteItem_ClickAsync(object sender, RoutedEventArgs e)
        {
            DataPackageView packageView = Clipboard.GetContent();
            string destinationPath = CurrentInstance.FilesystemViewModel.WorkingDirectory;

            ItemOperations.PasteItemWithStatus(packageView, destinationPath, packageView.RequestedOperation);
        }

        public void NewFolder_Click(object sender, RoutedEventArgs e)
        {
            AddItemDialog.CreateFile(AddItemType.Folder);
        }

        public void NewTextDocument_Click(object sender, RoutedEventArgs e)
        {
            AddItemDialog.CreateFile(AddItemType.TextDocument);
        }

        public void NewBitmapImage_Click(object sender, RoutedEventArgs e)
        {
            AddItemDialog.CreateFile(AddItemType.BitmapImage);
        }

        public void SelectAllItems() => CurrentInstance.ContentPage.SelectAllItems();

        public void InvertAllItems() => CurrentInstance.ContentPage.InvertSelection();

        public void ClearAllItems() => CurrentInstance.ContentPage.ClearSelection();

        public async void ToggleQuickLook()
        {
            try
            {
                if (CurrentInstance.ContentPage.IsItemSelected && !App.CurrentInstance.ContentPage.isRenamingItem)
                {
                    var clickedOnItem = CurrentInstance.ContentPage.SelectedItem;

                    Logger.Info("Toggle QuickLook");
                    Debug.WriteLine("Toggle QuickLook");
                    if (App.Connection != null)
                    {
                        var value = new ValueSet();
                        value.Add("path", clickedOnItem.ItemPath);
                        value.Add("Arguments", "ToggleQuickLook");
                        await App.Connection.SendMessageAsync(value);
                    }
                }
            }
            catch (FileNotFoundException)
            {
                await DialogDisplayHelper.ShowDialog(ResourceController.GetTranslation("FileNotFoundDialog/Title"), ResourceController.GetTranslation("FileNotFoundPreviewDialog/Text"));
                NavigationActions.Refresh_Click(null, null);
            }
        }

        public void PushJumpChar(char letter)
        {
            App.CurrentInstance.FilesystemViewModel.JumpString += letter.ToString().ToLower();
        }

        public async Task<string> GetHashForFile(ListedItem fileItem, string nameOfAlg, CancellationToken token, Microsoft.UI.Xaml.Controls.ProgressBar progress)
        {
            HashAlgorithmProvider algorithmProvider = HashAlgorithmProvider.OpenAlgorithm(nameOfAlg);
            var itemFromPath = await ItemViewModel.GetFileFromPathAsync((fileItem as ShortcutItem)?.TargetPath ?? fileItem.ItemPath);
            var stream = await itemFromPath.OpenStreamForReadAsync();
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
    }
}