using Files.DataModels;
using Files.Dialogs;
using Files.Filesystem;
using Files.Helpers;
using Files.Views.Pages;
using GalaSoft.MvvmLight.Command;
using NLog;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.WindowManagement;
using Windows.UI.WindowManagement.Preview;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using static Files.Dialogs.ConfirmDeleteDialog;
using Files.UserControls;
using Windows.UI.ViewManagement;

namespace Files.Interacts
{
    public class Interaction
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IShellPage CurrentInstance;
        private readonly InstanceTabsView instanceTabsView;

        public Interaction()
        {
            CurrentInstance = App.CurrentInstance;
            instanceTabsView = (Window.Current.Content as Frame).Content as InstanceTabsView;
        }

        public void List_ItemClick(object sender, DoubleTappedRoutedEventArgs e)
        {
            OpenSelectedItems(false);
        }

        public async void SetAsDesktopBackgroundItem_Click(object sender, RoutedEventArgs e)
        {
            // Get the path of the selected file
            StorageFile sourceFile = await StorageFile.GetFileFromPathAsync(CurrentInstance.ContentPage.SelectedItem.ItemPath);

            // Get the app's local folder to use as the destination folder.
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            // Copy the file to the destination folder.
            // Replace the existing file if the file already exists.
            StorageFile file = await sourceFile.CopyAsync(localFolder, "Background.png", NameCollisionOption.ReplaceExisting);

            // Set the desktop background
            UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
            await profileSettings.TrySetWallpaperImageAsync(file);
        }

        public async void SetAsLockscreenBackgroundItem_Click(object sender, RoutedEventArgs e)
        {
            // Get the path of the selected file
            StorageFile sourceFile = await StorageFile.GetFileFromPathAsync(CurrentInstance.ContentPage.SelectedItem.ItemPath);

            // Get the app's local folder to use as the destination folder.
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            // Copy the file to the destination folder.
            // Replace the existing file if the file already exists.
            StorageFile file = await sourceFile.CopyAsync(localFolder, "Background.png", NameCollisionOption.ReplaceExisting);

            // Set the lockscreen background
            await LockScreen.SetImageFileAsync(file);
        }

        public void OpenNewTab()
        {
            instanceTabsView.AddNewTab(typeof(ModernShellPage), "New tab");
        }

        public async void OpenInNewWindowItem_Click(object sender, RoutedEventArgs e)
        {
            var items = CurrentInstance.ContentPage.SelectedItems;
            foreach (ListedItem listedItem in items)
            {
                var selectedItemPath = listedItem.ItemPath;
                var folderUri = new Uri("files-uwp:" + "?folder=" + @selectedItemPath);
                await Launcher.LaunchUriAsync(folderUri);
            }
        }

        public async void OpenDirectoryInNewTab_Click(object sender, RoutedEventArgs e)
        {
            foreach (ListedItem listedItem in CurrentInstance.ContentPage.SelectedItems)
            {
                await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    instanceTabsView.AddNewTab(typeof(ModernShellPage), listedItem.ItemPath);
                });
            }
        }

        public void OpenPathInNewTab(string path)
        {
            instanceTabsView.AddNewTab(typeof(ModernShellPage), path);
        }

        public async void OpenPathInNewWindow(string path)
        {
            var folderUri = new Uri("files-uwp:" + "?folder=" + path);
            await Launcher.LaunchUriAsync(folderUri);
        }

        public async void OpenDirectoryInTerminal(object sender, RoutedEventArgs e)
        {
            var terminal = App.AppSettings.TerminalsModel.GetDefaultTerminal();

            if (App.Connection != null)
            {
                var value = new ValueSet
                {
                    { "WorkingDirectory", CurrentInstance.FilesystemViewModel.WorkingDirectory },
                    { "Application", terminal.Path },
                    { "Arguments", string.Format(terminal.Arguments,
                        InstanceTabsView.NormalizePath(CurrentInstance.FilesystemViewModel.WorkingDirectory)) }
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
                    App.SidebarPinned.AddItem(listedItem.ItemPath);
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

        public static async Task InvokeWin32Component(string applicationPath, string arguments = null)
        {
            await InvokeWin32Components(new List<string>() { applicationPath }, arguments);
        }

        public static async Task InvokeWin32Components(List<string> applicationPaths, string arguments = null)
        {
            Debug.WriteLine("Launching EXE in FullTrustProcess");
            if (App.Connection != null)
            {
                var value = new ValueSet();
                value.Add("ApplicationList", JsonConvert.SerializeObject(applicationPaths));
                value.Add("Arguments", arguments);
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
                if ((current.GetType()).Equals(typeof(T)) || (current.GetType().GetTypeInfo().IsSubclassOf(typeof(T))))
                {
                    T asType = (T)current;
                    return asType;
                }
                FindChild<T>(current);
            }
            return null;
        }

        public static void FindChildren<T>(List<T> results, DependencyObject startNode) where T : DependencyObject
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

        public void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectedItems(false);
        }

        public void OpenItemWithApplicationPicker_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectedItems(true);
        }

        private async void OpenSelectedItems(bool displayApplicationPicker)
        {
            if (CurrentInstance.FilesystemViewModel.WorkingDirectory.StartsWith(App.AppSettings.RecycleBinPath))
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
                        // Add location to MRU List
                        mostRecentlyUsed.Add(await StorageFolder.GetFolderFromPathAsync(clickedOnItemPath));

                        CurrentInstance.FilesystemViewModel.WorkingDirectory = clickedOnItemPath;
                        CurrentInstance.NavigationToolbar.PathControlDisplayText = clickedOnItemPath;

                        CurrentInstance.ContentPage.AssociatedViewModel.IsFolderEmptyTextDisplayed = false;
                        CurrentInstance.ContentFrame.Navigate(sourcePageType, clickedOnItemPath, new SuppressNavigationTransitionInfo());
                    }
                    else
                    {
                        // Add location to MRU List
                        mostRecentlyUsed.Add(await StorageFile.GetFileFromPathAsync(clickedOnItem.ItemPath));
                        if (displayApplicationPicker)
                        {
                            StorageFile file = await StorageFile.GetFileFromPathAsync(clickedOnItem.ItemPath);
                            var options = new LauncherOptions
                            {
                                DisplayApplicationPicker = true
                            };
                            await Launcher.LaunchFileAsync(file, options);
                        }
                        else
                        {
                            //try using launcher first


                            

                            bool launchSuccess = false;

                            try
                            {
                                StorageFileQueryResult fileQueryResult = null;

                                StorageFile file = await StorageFile.GetFileFromPathAsync(clickedOnItem.ItemPath);

                                //Get folder to create a file query (to pass to apps like Photos, Movies & TV..., needed to scroll through the folder like what Windows Explorer does)
                                var currFolder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(clickedOnItem.ItemPath));

                                QueryOptions queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, null);



                                //We can have many sort entries
                                SortEntry sortEntry = new SortEntry()
                                {
                                    AscendingOrder = CurrentInstance.FilesystemViewModel.DirectorySortDirection == Microsoft.Toolkit.Uwp.UI.SortDirection.Ascending,
                                };

                                var sortOption = CurrentInstance.FilesystemViewModel.DirectorySortOption;

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
                                launchSuccess = await Launcher.LaunchFileAsync(file, options);
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
                        instanceTabsView.AddNewTab(typeof(ModernShellPage), clickedOnItem.ItemPath);
                    }
                    foreach (ListedItem clickedOnItem in CurrentInstance.ContentPage.SelectedItems.Where(x => x.PrimaryItemAttribute == StorageItemTypes.File))
                    {
                        // Add location to MRU List
                        mostRecentlyUsed.Add(await StorageFile.GetFileFromPathAsync(clickedOnItem.ItemPath));
                    }
                    if (displayApplicationPicker)
                    {
                        foreach (ListedItem clickedOnItem in CurrentInstance.ContentPage.SelectedItems.Where(x => x.PrimaryItemAttribute == StorageItemTypes.File))
                        {
                            StorageFile file = await StorageFile.GetFileFromPathAsync(clickedOnItem.ItemPath);
                            var options = new LauncherOptions
                            {
                                DisplayApplicationPicker = true
                            };
                            await Launcher.LaunchFileAsync(file, options);
                        }
                    }
                    else
                    {
                        var applicationPath = string.Join(";", CurrentInstance.ContentPage.SelectedItems.Where(x => x.PrimaryItemAttribute == StorageItemTypes.File).Select(x => x.ItemPath));
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

        public async void CloseTab()
        {
            if (((Window.Current.Content as Frame).Content as InstanceTabsView).TabStrip.TabItems.Count == 1)
            {
                await InstanceTabsView.StartTerminateAsync();
            }
            else if (((Window.Current.Content as Frame).Content as InstanceTabsView).TabStrip.TabItems.Count > 1)
            {
                ((Window.Current.Content as Frame).Content as InstanceTabsView).TabStrip.TabItems.RemoveAt(((Window.Current.Content as Frame).Content as InstanceTabsView).TabStrip.SelectedIndex);
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

        public static Dictionary<UIContext, AppWindow> AppWindows { get; set; }
            = new Dictionary<UIContext, AppWindow>();

        private async void ShowProperties()
        {
            if (App.CurrentInstance.ContentPage.IsItemSelected)
            {
                if (App.AppSettings.OpenPropertiesInMultipleWindows)
                {
                    foreach (var item in App.CurrentInstance.ContentPage.SelectedItems)
                    {
                        await OpenPropertiesWindow(item);
                    }
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
                    //TODO: Implement drive properties
                }
            }
        }
        private async Task OpenPropertiesWindow(ListedItem item)
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                AppWindow appWindow = await AppWindow.TryCreateAsync();
                Frame frame = new Frame();
                appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                frame.Navigate(typeof(Properties), item, new SuppressNavigationTransitionInfo());
                WindowManagementPreview.SetPreferredMinSize(appWindow, new Size(400, 500));

                appWindow.RequestSize(new Size(400, 475));
                appWindow.Title = ResourceController.GetTranslation("PropertiesTitle");

                ElementCompositionPreview.SetAppWindowContent(appWindow, frame);
                AppWindows.Add(frame.UIContext, appWindow);

                appWindow.Closed += delegate
                {
                    AppWindows.Remove(frame.UIContext);
                    frame.Content = null;
                    appWindow = null;
                };

                await appWindow.TryShowAsync();
            }
            else
            {
                App.PropertiesDialogDisplay.propertiesFrame.Tag = App.PropertiesDialogDisplay;
                App.PropertiesDialogDisplay.propertiesFrame.Navigate(typeof(Properties), item, new SuppressNavigationTransitionInfo());
                await App.PropertiesDialogDisplay.ShowAsync(ContentDialogPlacement.Popup);
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
            App.SidebarPinned.AddItem(CurrentInstance.FilesystemViewModel.WorkingDirectory);
        }

        private async void Manager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequestDeferral dataRequestDeferral = args.Request.GetDeferral();
            List<IStorageItem> items = new List<IStorageItem>();

            foreach (ListedItem item in App.CurrentInstance.ContentPage.SelectedItems)
            {
                if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    var folderAsItem = await StorageFolder.GetFolderFromPathAsync(item.ItemPath);
                    items.Add(folderAsItem);
                }
                else
                {
                    var fileAsItem = await StorageFile.GetFileFromPathAsync(item.ItemPath);
                    items.Add(fileAsItem);
                }
            }

            DataRequest dataRequest = args.Request;
            dataRequest.Data.SetStorageItems(items);
            dataRequest.Data.Properties.Title = "Data Shared From Files";
            dataRequest.Data.Properties.Description = "The items you selected will be shared";
            dataRequestDeferral.Complete();
        }

        public async void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            var deleteFromRecycleBin = CurrentInstance.FilesystemViewModel.WorkingDirectory.StartsWith(App.AppSettings.RecycleBinPath);
            if (deleteFromRecycleBin)
            {
                // Permanently delete if deleting from recycle bin
                App.InteractionViewModel.PermanentlyDelete = StorageDeleteOption.PermanentDelete;
            }

            if (App.AppSettings.ShowConfirmDeleteDialog == true) //check if the setting to show a confirmation dialog is on
            {
                var dialog = new ConfirmDeleteDialog(deleteFromRecycleBin);
                await dialog.ShowAsync();

                if (dialog.Result != MyResult.Delete) //delete selected  item(s) if the result is yes
                {
                    App.InteractionViewModel.PermanentlyDelete = StorageDeleteOption.Default; //reset PermanentlyDelete flag
                    return; //return if the result isn't delete
                }
            }
            StatusBanner banner = null;
            try
            {
                var CurrentInstance = App.CurrentInstance;
                List<ListedItem> selectedItems = new List<ListedItem>();
                foreach (ListedItem selectedItem in CurrentInstance.ContentPage.SelectedItems)
                {
                    selectedItems.Add(selectedItem);
                }
                int itemsDeleted = 0;
                if (selectedItems.Count > 3)
                {
                    banner = App.CurrentInstance.StatusBarControl.OngoingTasksControl.PostBanner(null, 
                        CurrentInstance.FilesystemViewModel.WorkingDirectory, 
                        0, 
                        UserControls.StatusBanner.StatusBannerSeverity.Ongoing, 
                        UserControls.StatusBanner.StatusBannerOperation.Delete);
                }
                await Task.Run(async () => 
                {
                    foreach (ListedItem storItem in selectedItems)
                    {
                        uint progressValue = (uint)(itemsDeleted * 100.0 / selectedItems.Count);
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                            new DispatchedHandler(() => 
                            {
                                if (selectedItems.Count > 3) { banner = banner.UpdateProgress(progressValue); }
                            }));
                       
                        IStorageItem item;
                        try
                        {
                            if (storItem.PrimaryItemAttribute == StorageItemTypes.File)
                            {
                                item = await StorageFile.GetFileFromPathAsync(storItem.ItemPath);
                            }
                            else
                            {
                                item = await StorageFolder.GetFolderFromPathAsync(storItem.ItemPath);
                            }

                            await item.DeleteAsync(App.InteractionViewModel.PermanentlyDelete);
                        }
                        catch (FileLoadException)
                        {
                            // try again
                            if (storItem.PrimaryItemAttribute == StorageItemTypes.File)
                            {
                                item = await StorageFile.GetFileFromPathAsync(storItem.ItemPath);
                            }
                            else
                            {
                                item = await StorageFolder.GetFolderFromPathAsync(storItem.ItemPath);
                            }

                            await item.DeleteAsync(App.InteractionViewModel.PermanentlyDelete);
                        }

                        if (deleteFromRecycleBin)
                        {
                            // Recycle bin also stores a file starting with $I for each item
                            var iFilePath = Path.Combine(Path.GetDirectoryName(storItem.ItemPath), Path.GetFileName(storItem.ItemPath).Replace("$R", "$I"));
                            await (await StorageFile.GetFileFromPathAsync(iFilePath)).DeleteAsync(StorageDeleteOption.PermanentDelete);
                        }

                        CurrentInstance.FilesystemViewModel.RemoveFileOrFolder(storItem);
                        itemsDeleted++;
                    }
                });

                App.CurrentInstance.NavigationToolbar.CanGoForward = false;
            }
            catch (UnauthorizedAccessException)
            {
                await DialogDisplayHelper.ShowDialog(ResourceController.GetTranslation("AccessDeniedDeleteDialog/Title"), ResourceController.GetTranslation("AccessDeniedDeleteDialog/Text"));
            }
            catch (FileNotFoundException)
            {
                await DialogDisplayHelper.ShowDialog(ResourceController.GetTranslation("FileNotFoundDialog/Title"), ResourceController.GetTranslation("FileNotFoundDialog/Text"));
            }
            catch (IOException)
            {
                if (await DialogDisplayHelper.ShowDialog(ResourceController.GetTranslation("FileInUseDeleteDialog.Title"), ResourceController.GetTranslation("FileInUseDeleteDialog.Text"), ResourceController.GetTranslation("FileInUseDeleteDialog.PrimaryButtonText"), ResourceController.GetTranslation("FileInUseDeleteDialog.SecondaryButtonText")))
                {
                    DeleteItem_Click(null, null);
                }
            }
            App.CurrentInstance.StatusBarControl.OngoingTasksControl.RemoveBanner(banner);
            App.InteractionViewModel.PermanentlyDelete = StorageDeleteOption.Default; //reset PermanentlyDelete flag
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
                        var folder = await StorageFolder.GetFolderFromPathAsync(item.ItemPath);
                        await folder.RenameAsync(newName, NameCollisionOption.FailIfExists);
                    }
                    else
                    {
                        var file = await StorageFile.GetFileFromPathAsync(item.ItemPath);
                        await file.RenameAsync(newName, NameCollisionOption.FailIfExists);
                    }
                }
                catch (Exception)
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
                            var folder = await StorageFolder.GetFolderFromPathAsync(item.ItemPath);

                            await folder.RenameAsync(newName, NameCollisionOption.GenerateUniqueName);
                        }
                        else
                        {
                            var file = await StorageFile.GetFileFromPathAsync(item.ItemPath);

                            await file.RenameAsync(newName, NameCollisionOption.GenerateUniqueName);
                        }
                    }
                    else if (result == ContentDialogResult.Secondary)
                    {
                        if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                        {
                            var folder = await StorageFolder.GetFolderFromPathAsync(item.ItemPath);

                            await folder.RenameAsync(newName, NameCollisionOption.ReplaceExisting);
                        }
                        else
                        {
                            var file = await StorageFile.GetFileFromPathAsync(item.ItemPath);

                            await file.RenameAsync(newName, NameCollisionOption.ReplaceExisting);
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
                        if (listedItem.PrimaryItemAttribute == StorageItemTypes.Folder)
                        {
                            StorageFolder sourceFolder = await StorageFolder.GetFolderFromPathAsync(listedItem.ItemPath);
                            await MoveDirectoryAsync(sourceFolder, Path.GetDirectoryName(listedItem.ItemOriginalPath), listedItem.ItemName);
                            await sourceFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
                        }
                        else
                        {
                            var file = await StorageFile.GetFileFromPathAsync(listedItem.ItemPath);
                            var destinationFolder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(listedItem.ItemOriginalPath));
                            await file.MoveAsync(destinationFolder, Path.GetFileName(listedItem.ItemOriginalPath), NameCollisionOption.GenerateUniqueName);
                        }
                        // Recycle bin also stores a file starting with $I for each item
                        var iFilePath = Path.Combine(Path.GetDirectoryName(listedItem.ItemPath), Path.GetFileName(listedItem.ItemPath).Replace("$R", "$I"));
                        await (await StorageFile.GetFileFromPathAsync(iFilePath)).DeleteAsync(StorageDeleteOption.PermanentDelete);
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

        public async Task<StorageFolder> MoveDirectoryAsync(StorageFolder SourceFolder, string DestinationPath, string sourceRootName)
        {
            StorageFolder DestinationFolder = await StorageFolder.GetFolderFromPathAsync(DestinationPath);
            var createdRoot = await DestinationFolder.CreateFolderAsync(sourceRootName, CreationCollisionOption.FailIfExists);
            DestinationFolder = await StorageFolder.GetFolderFromPathAsync(createdRoot.Path);

            foreach (StorageFile fileInSourceDir in await SourceFolder.GetFilesAsync())
            {
                await fileInSourceDir.MoveAsync(DestinationFolder, fileInSourceDir.Name, NameCollisionOption.FailIfExists);
            }
            foreach (StorageFolder folderinSourceDir in await SourceFolder.GetFoldersAsync())
            {
                await MoveDirectoryAsync(folderinSourceDir, DestinationFolder.Path, folderinSourceDir.Name);
            }
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

                foreach (ListedItem listedItem in CurrentInstance.ContentPage.SelectedItems)
                {
                    // Dim opacities accordingly
                    CurrentInstance.ContentPage.SetItemOpacity(listedItem);

                    try
                    {
                        if (listedItem.PrimaryItemAttribute == StorageItemTypes.File)
                        {
                            var item = await StorageFile.GetFileFromPathAsync(listedItem.ItemPath);
                            items.Add(item);
                        }
                        else
                        {
                            var item = await StorageFolder.GetFolderFromPathAsync(listedItem.ItemPath);
                            items.Add(item);
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        CurrentInstance.ContentPage.ResetItemOpacity();
                        return;
                    }
                }
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
        public IReadOnlyList<IStorageItem> itemsToPaste;
        public int itemsPasted;

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
                foreach (ListedItem listedItem in App.CurrentInstance.ContentPage.SelectedItems)
                {
                    if (listedItem.PrimaryItemAttribute == StorageItemTypes.File)
                    {
                        var item = await StorageFile.GetFileFromPathAsync(listedItem.ItemPath);
                        items.Add(item);
                    }
                    else
                    {
                        var item = await StorageFolder.GetFolderFromPathAsync(listedItem.ItemPath);
                        items.Add(item);
                    }
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

        private enum ImpossibleActionResponseTypes
        {
            Skip,
            Abort
        }

        public async void EmptyRecycleBin_ClickAsync(object sender, RoutedEventArgs e)
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

        public async void PasteItem_ClickAsync(object sender, RoutedEventArgs e)
        {
            DataPackageView packageView = Clipboard.GetContent();
            string destinationPath = CurrentInstance.FilesystemViewModel.WorkingDirectory;

            await PasteItems(packageView, destinationPath, packageView.RequestedOperation);
        }

        public async Task PasteItems(DataPackageView packageView, string destinationPath, DataPackageOperation acceptedOperation)
        {
            if (!packageView.Contains(StandardDataFormats.StorageItems))
            {
                // Happens if you copy some text and then you Ctrl+V in FilesUWP
                // Should this be done in ModernShellPage?
                return;
            }
            if (CurrentInstance.FilesystemViewModel.WorkingDirectory.StartsWith(App.AppSettings.RecycleBinPath))
            {
                // Do not paste files and folders inside the recycle bin
                await DialogDisplayHelper.ShowDialog(ResourceController.GetTranslation("ErrorDialogThisActionCannotBeDone"), ResourceController.GetTranslation("ErrorDialogUnsupportedOperation"));
                return;
            }

            itemsToPaste = await packageView.GetStorageItemsAsync();
            HashSet<IStorageItem> pastedItems = new HashSet<IStorageItem>();
            itemsPasted = 0;
            StatusBanner banner = null;
            if (itemsToPaste.Count > 3)
            {
                banner = App.CurrentInstance.StatusBarControl.OngoingTasksControl.PostBanner(null,
                        CurrentInstance.FilesystemViewModel.WorkingDirectory,
                        0,
                        StatusBanner.StatusBannerSeverity.Ongoing,
                        StatusBanner.StatusBannerOperation.Paste);
            }

            await Task.Run(async () => 
            {
                foreach (IStorageItem item in itemsToPaste)
                {
                    if (item.IsOfType(StorageItemTypes.Folder))
                    {
                        if (destinationPath.IsSubPathOf(item.Path))
                        {
                            ImpossibleActionResponseTypes responseType = ImpossibleActionResponseTypes.Abort;
                            Binding themeBind = new Binding();
                            themeBind.Source = ThemeHelper.RootTheme;

                            ContentDialog dialog = new ContentDialog()
                            {
                                Title = ResourceController.GetTranslation("ErrorDialogThisActionCannotBeDone"),
                                Content = ResourceController.GetTranslation("ErrorDialogTheDestinationFolder") + " (" + destinationPath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last() + ") " + ResourceController.GetTranslation("ErrorDialogIsASubfolder") + " (" + item.Name + ")",
                                PrimaryButtonText = ResourceController.GetTranslation("ErrorDialogSkip"),
                                CloseButtonText = ResourceController.GetTranslation("ErrorDialogCancel"),
                                PrimaryButtonCommand = new RelayCommand(() => { responseType = ImpossibleActionResponseTypes.Skip; }),
                                CloseButtonCommand = new RelayCommand(() => { responseType = ImpossibleActionResponseTypes.Abort; })
                            };
                            BindingOperations.SetBinding(dialog, FrameworkElement.RequestedThemeProperty, themeBind);

                            await dialog.ShowAsync();
                            if (responseType == ImpossibleActionResponseTypes.Skip)
                            {
                                continue;
                            }
                            else if (responseType == ImpossibleActionResponseTypes.Abort)
                            {
                                return;
                            }
                        }
                        else
                        {
                            try
                            {
                                ClonedDirectoryOutput pastedOutput = await CloneDirectoryAsync(item.Path, destinationPath, item.Name, false, banner);
                                pastedItems.Add(pastedOutput.FolderOutput);
                                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                    new DispatchedHandler(() =>
                                    {
                                        banner = pastedOutput.StatusBannerOutput;
                                    }));
                            }
                            catch (FileNotFoundException)
                            {
                                // Folder was moved/deleted in the meantime
                                continue;
                            }
                        }
                    }
                    else if (item.IsOfType(StorageItemTypes.File))
                    {
                        uint progressValue = (uint)(itemsPasted * 100.0 / itemsToPaste.Count);
                        if (itemsToPaste.Count > 3)
                        {
                            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                new DispatchedHandler(() =>
                                {
                                    banner = banner.UpdateProgress(progressValue);
                                }));
                        }
                        try
                        {
                            StorageFile clipboardFile = await StorageFile.GetFileFromPathAsync(item.Path);
                            StorageFile pastedFile = await clipboardFile.CopyAsync(await StorageFolder.GetFolderFromPathAsync(destinationPath), item.Name, NameCollisionOption.GenerateUniqueName);
                            pastedItems.Add(pastedFile);
                        }
                        catch (FileNotFoundException)
                        {
                            // File was moved/deleted in the meantime
                            continue;
                        }
                    }
                }

                if (acceptedOperation == DataPackageOperation.Move)
                {
                    foreach (IStorageItem item in itemsToPaste)
                    {
                        try
                        {
                            if (item.IsOfType(StorageItemTypes.File))
                            {
                                StorageFile file = await StorageFile.GetFileFromPathAsync(item.Path);
                                await file.DeleteAsync();
                            }
                            else if (item.IsOfType(StorageItemTypes.Folder))
                            {
                                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(item.Path);
                                await folder.DeleteAsync();
                            }
                        }
                        catch (FileNotFoundException)
                        {
                            // File or Folder was moved/deleted in the meantime
                            continue;
                        }
                        ListedItem listedItem = CurrentInstance.FilesystemViewModel.FilesAndFolders.FirstOrDefault(listedItem => listedItem.ItemPath.Equals(item.Path, StringComparison.OrdinalIgnoreCase));
                    }
                }
            });

            if (destinationPath == CurrentInstance.FilesystemViewModel.WorkingDirectory)
            {
                List<string> pastedItemPaths = pastedItems.Select(item => item.Path).ToList();
                List<ListedItem> copiedItems = CurrentInstance.FilesystemViewModel.FilesAndFolders.Where(listedItem => pastedItemPaths.Contains(listedItem.ItemPath)).ToList();
                if (copiedItems.Any())
                {
                    CurrentInstance.ContentPage.SetSelectedItemsOnUi(copiedItems);
                    CurrentInstance.ContentPage.FocusSelectedItems();
                }
            }
            packageView.ReportOperationCompleted(acceptedOperation);
            App.CurrentInstance.StatusBarControl.OngoingTasksControl.RemoveBanner(banner);
        }

        public class ClonedDirectoryOutput
        {
            public StorageFolder FolderOutput { get; set; }
            public StatusBanner StatusBannerOutput { get; set; } = null;
        }

        public async Task<ClonedDirectoryOutput> CloneDirectoryAsync(string SourcePath, string DestinationPath, string sourceRootName, bool suppressProgressFlyout, StatusBanner banner = null)
        {
            StorageFolder SourceFolder = await StorageFolder.GetFolderFromPathAsync(SourcePath);
            StorageFolder DestinationFolder = await StorageFolder.GetFolderFromPathAsync(DestinationPath);
            var createdRoot = await DestinationFolder.CreateFolderAsync(sourceRootName, CreationCollisionOption.GenerateUniqueName);
            DestinationFolder = await StorageFolder.GetFolderFromPathAsync(createdRoot.Path);

            foreach (StorageFile fileInSourceDir in await SourceFolder.GetFilesAsync())
            {
                if (itemsToPaste != null)
                {
                    if (itemsToPaste.Count > 3 && !suppressProgressFlyout)
                    {
                        uint progressValue = (uint)(itemsPasted * 100.0 / (itemsToPaste.Count + (await SourceFolder.GetFilesAsync()).Count));
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                            new DispatchedHandler(() =>
                            {
                                if (banner == null)
                                {
                                    throw new ArgumentNullException();
                                }
                                else
                                {
                                    banner = banner.UpdateProgress(progressValue);
                                }
                            }));
                    }
                }

                await fileInSourceDir.CopyAsync(DestinationFolder, fileInSourceDir.Name, NameCollisionOption.GenerateUniqueName);
            }
            foreach (StorageFolder folderinSourceDir in await SourceFolder.GetFoldersAsync())
            {
                if (itemsToPaste != null)
                {
                    if (itemsToPaste.Count > 3 && !suppressProgressFlyout)
                    {
                        uint progressValue = (uint)(itemsPasted * 100.0 / (itemsToPaste.Count + (await SourceFolder.GetFoldersAsync()).Count));
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                            new DispatchedHandler(() =>
                            {
                                if (banner == null)
                                {
                                    throw new ArgumentNullException();
                                }
                                else
                                {
                                    banner = banner.UpdateProgress(progressValue);
                                }
                            }));
                    }
                }

                var output = await CloneDirectoryAsync(folderinSourceDir.Path, DestinationFolder.Path, folderinSourceDir.Name, false, banner);
                banner = output.StatusBannerOutput;
            }
            if (!suppressProgressFlyout)
            {
                return new ClonedDirectoryOutput()
                {
                    FolderOutput = createdRoot,
                    StatusBannerOutput = banner
                };
            }
            else
            {
                return new ClonedDirectoryOutput()
                {
                    FolderOutput = createdRoot,
                    StatusBannerOutput = null
                };
            }
            
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

        public async void ExtractItems_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = CurrentInstance.ContentPage.GetSelectedIndex();
            StorageFile selectedItem = await StorageFile.GetFileFromPathAsync(CurrentInstance.FilesystemViewModel.FilesAndFolders[selectedIndex].ItemPath);

            StatusBanner banner = null;
            ExtractFilesDialog extractFilesDialog = new ExtractFilesDialog(CurrentInstance.FilesystemViewModel.WorkingDirectory);
            await extractFilesDialog.ShowAsync();
            if (((bool)ApplicationData.Current.LocalSettings.Values["Extract_Destination_Cancelled"]) == false)
            {
                var bufferItem = await selectedItem.CopyAsync(ApplicationData.Current.TemporaryFolder, selectedItem.DisplayName, NameCollisionOption.ReplaceExisting);
                string destinationPath = ApplicationData.Current.LocalSettings.Values["Extract_Destination_Path"].ToString();
                //ZipFile.ExtractToDirectory(selectedItem.Path, destinationPath, );
                var destFolder_InBuffer = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(selectedItem.DisplayName + "_Extracted", CreationCollisionOption.ReplaceExisting);
                using FileStream fs = new FileStream(bufferItem.Path, FileMode.Open);
                ZipArchive zipArchive = new ZipArchive(fs);
                int totalCount = zipArchive.Entries.Count;
                int index = 0;

                App.InteractionViewModel.IsContentLoadingIndicatorVisible = false;
                banner = App.CurrentInstance.StatusBarControl.OngoingTasksControl.PostBanner(
                    null, 
                    App.CurrentInstance.FilesystemViewModel.WorkingDirectory, 
                    0, 
                    StatusBanner.StatusBannerSeverity.Ongoing, 
                    StatusBanner.StatusBannerOperation.Extract);

                await Task.Run(async () => {
                    foreach (ZipArchiveEntry archiveEntry in zipArchive.Entries)
                    {
                        uint progressValue = (uint)(index * 100.0 / zipArchive.Entries.Count);
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                            new DispatchedHandler(() =>
                            {
                                if (banner == null)
                                {
                                    throw new ArgumentNullException();
                                }
                                else
                                {
                                    banner.UpdateProgress(progressValue);
                                }
                            }));
                        if (archiveEntry.FullName.Contains('/'))
                        {
                            var nestedDirectories = archiveEntry.FullName.Split('/').ToList();
                            nestedDirectories.Remove(nestedDirectories.Last());
                            var relativeOutputPathToEntry = Path.Combine(nestedDirectories.ToArray());
                            System.IO.Directory.CreateDirectory(Path.Combine(destFolder_InBuffer.Path, relativeOutputPathToEntry));
                        }

                        if (!string.IsNullOrWhiteSpace(archiveEntry.Name))
                            archiveEntry.ExtractToFile(Path.Combine(destFolder_InBuffer.Path, archiveEntry.FullName));

                        index++;
                        if (index == totalCount)
                        {
                            App.InteractionViewModel.IsContentLoadingIndicatorVisible = false;
                        }
                    }
                });
                
                await CloneDirectoryAsync(destFolder_InBuffer.Path, destinationPath, destFolder_InBuffer.Name, true)
                    .ContinueWith(async (x) =>
                {
                    await destFolder_InBuffer.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        Frame rootFrame = Window.Current.Content as Frame;
                        var instanceTabsView = rootFrame.Content as InstanceTabsView;
                        instanceTabsView.AddNewTab(typeof(ModernShellPage), destinationPath + "\\" + selectedItem.DisplayName + "_Extracted");
                    });
                });
                banner.UpdateProgress(100);
                App.CurrentInstance.StatusBarControl.OngoingTasksControl.RemoveBanner(banner);
            }
            else if (((bool)ApplicationData.Current.LocalSettings.Values["Extract_Destination_Cancelled"]) == true)
            {
                return;
            }
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
            var itemFromPath = await StorageFile.GetFileFromPathAsync(fileItem.ItemPath);
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

                progress.Value = (double)str.Position / str.Length * 100;
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