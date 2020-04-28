using Files.Dialogs;
using Files.Filesystem;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.System.UserProfile;
using static Files.Dialogs.ConfirmDeleteDialog;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Files.Views.Pages;
using Windows.Foundation.Metadata;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml.Hosting;
using Windows.UI.WindowManagement.Preview;
using Windows.UI;
using Windows.Security.Cryptography.Core;
using Microsoft.Toolkit.Uwp.Helpers;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using GalaSoft.MvvmLight.Command;
using Files.Helpers;
using Windows.UI.Xaml.Data;
using System.Security.Cryptography;

namespace Files.Interacts
{
    public class Interaction
    {
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
            StorageFile sourceFile = await StorageFile.GetFileFromPathAsync((CurrentInstance.ContentPage as BaseLayout).SelectedItem.ItemPath);

            // Get the app's local folder to use as the destination folder.
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            // Copy the file to the destination folder.
            // Replace the existing file if the file already exists.
            StorageFile file = await sourceFile.CopyAsync(localFolder, "Background.png", NameCollisionOption.ReplaceExisting);

            // Set the desktop background
            UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
            await profileSettings.TrySetWallpaperImageAsync(file);
        }

        public void OpenNewTab()
        {
            instanceTabsView.AddNewTab(typeof(ModernShellPage), "New tab");
        }

        public async void OpenInNewWindowItem_Click(object sender, RoutedEventArgs e)
        {
            var CurrentSourceType = App.CurrentInstance.CurrentPageType;
            if (CurrentSourceType == typeof(GenericFileBrowser))
            {
                var items = (CurrentInstance.ContentPage as BaseLayout).SelectedItems;
                foreach (ListedItem listedItem in items)
                {
                    var selectedItemPath = listedItem.ItemPath;
                    var folderUri = new Uri("files-uwp:" + "?folder=" + @selectedItemPath);
                    await Launcher.LaunchUriAsync(folderUri);
                }
            }
            else if (CurrentSourceType == typeof(PhotoAlbum))
            {
                var items = (CurrentInstance.ContentPage as BaseLayout).SelectedItems;
                foreach (ListedItem listedItem in items)
                {
                    var selectedItemPath = listedItem.ItemPath;
                    var folderUri = new Uri("files-uwp:" + "?folder=" + @selectedItemPath);
                    await Launcher.LaunchUriAsync(folderUri);
                }
            }
        }

        public async void OpenDirectoryInNewTab_Click(object sender, RoutedEventArgs e)
        {
            var CurrentSourceType = App.CurrentInstance.CurrentPageType;
            var items = (CurrentInstance.ContentPage as BaseLayout).SelectedItems;
            foreach (ListedItem listedItem in items)
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
            var localSettings = ApplicationData.Current.LocalSettings;

            var terminalId = 1;

            if (localSettings.Values["terminal_id"] != null) terminalId = (int)localSettings.Values["terminal_id"];

            var terminal = App.AppSettings.Terminals.Single(p => p.Id == terminalId);

            localSettings.Values["Application"] = terminal.Path;
            localSettings.Values["Arguments"] = String.Format(terminal.arguments, CurrentInstance.ViewModel.WorkingDirectory);

            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }

        public async void PinItem_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentInstance.ContentPage != null)
            {
                StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
                List<string> items = new List<string>();

                try
                {
                    foreach (ListedItem listedItem in (CurrentInstance.ContentPage as BaseLayout).SelectedItems)
                    {
                        items.Add(listedItem.ItemPath);
                    }
                    var ListFile = await cacheFolder.GetFileAsync("PinnedItems.txt");
                    await FileIO.AppendLinesAsync(ListFile, items);
                }
                catch (FileNotFoundException)
                {
                    foreach (ListedItem listedItem in (CurrentInstance.ContentPage as BaseLayout).SelectedItems)
                    {
                        items.Add(listedItem.ItemPath);
                    }
                    var createdListFile = await cacheFolder.CreateFileAsync("PinnedItems.txt");
                    await FileIO.WriteLinesAsync(createdListFile, items);
                }
                finally
                {
                    foreach (string itemPath in items)
                    {
                        try
                        {
                            StorageFolder fol = await StorageFolder.GetFolderFromPathAsync(itemPath);
                            var name = fol.DisplayName;
                            var content = name;
                            var icon = "\uE8B7";

                            bool isDuplicate = false;
                            foreach (INavigationControlItem sbi in App.sideBarItems)
                            {
                                if (sbi is LocationItem)
                                {
                                    if (!string.IsNullOrWhiteSpace(sbi.Path) && !(sbi as LocationItem).IsDefaultLocation)
                                    {
                                        if (sbi.Path.ToString() == itemPath)
                                        {
                                            isDuplicate = true;
                                        }
                                    }
                                }
                            }

                            if (!isDuplicate)
                            {
                                int insertIndex = App.sideBarItems.IndexOf(App.sideBarItems.Last(x => x.ItemType == NavigationControlItemType.Location)) + 1;
                                App.sideBarItems.Insert(insertIndex, new LocationItem { Path = itemPath, Glyph = icon, IsDefaultLocation = false, Text = content });
                            }
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                        catch (FileNotFoundException ex)
                        {
                            Debug.WriteLine("Pinned item was deleted and will be removed from the file lines list soon: " + ex.Message);
                            App.AppSettings.LinesToRemoveFromFile.Add(itemPath);
                        }
                        catch (System.Runtime.InteropServices.COMException ex)
                        {
                            Debug.WriteLine("Pinned item's drive was ejected and will be removed from the file lines list soon: " + ex.Message);
                            App.AppSettings.LinesToRemoveFromFile.Add(itemPath);
                        }
                    }
                }
            }
            App.AppSettings.RemoveStaleSidebarItems();
        }

        public void GetPath_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentInstance.ContentPage != null)
            {
                Clipboard.Clear();
                DataPackage data = new DataPackage();
                data.SetText(CurrentInstance.ViewModel.WorkingDirectory);
                Clipboard.SetContent(data);
                Clipboard.Flush();
            }
        }

        public static async Task InvokeWin32Component(string ApplicationPath)
        {
            Debug.WriteLine("Launching EXE in FullTrustProcess");
            ApplicationData.Current.LocalSettings.Values["Application"] = ApplicationPath;
            ApplicationData.Current.LocalSettings.Values["Arguments"] = null;
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }

        public static async Task OpenShellCommandInExplorer(string shellCommand)
        {
            Debug.WriteLine("Launching shell command in FullTrustProcess");
            ApplicationData.Current.LocalSettings.Values["ShellCommand"] = shellCommand;
            ApplicationData.Current.LocalSettings.Values["Arguments"] = "ShellCommand";
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }

        public async void GrantAccessPermissionHandler(IUICommand command)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess"));
        }

        public DataGrid dataGrid;

        public void AllView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            dataGrid = (DataGrid)sender;
            var RowPressed = FindParent<DataGridRow>(e.OriginalSource as DependencyObject);
            if (RowPressed != null)
            {
                var ObjectPressed = ((ReadOnlyObservableCollection<ListedItem>)dataGrid.ItemsSource)[RowPressed.GetIndex()];

                // Check if RightTapped row is currently selected
                if (!App.CurrentInstance.ContentPage.SelectedItems.Equals(null))
                {
                    if (App.CurrentInstance.ContentPage.SelectedItems.Contains(ObjectPressed))
                        return;
                }

                // The following code is only reachable when a user RightTapped an unselected row
                dataGrid.SelectedItems.Clear();
                dataGrid.SelectedItems.Add(ObjectPressed);
            }
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

        private async void OpenSelectedItems(bool displayApplicationPicker)
        {
            try
            {
                string selectedItemPath = null;
                int selectedItemCount;
                Type sourcePageType = App.CurrentInstance.CurrentPageType;
                selectedItemCount = (CurrentInstance.ContentPage as BaseLayout).SelectedItems.Count;
                if (selectedItemCount == 1)
                {
                    selectedItemPath = (CurrentInstance.ContentPage as BaseLayout).SelectedItems[0].ItemPath;
                }

                // Access MRU List
                var mostRecentlyUsed = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;

                if (selectedItemCount == 1)
                {
                    var clickedOnItem = (CurrentInstance.ContentPage as BaseLayout).SelectedItems[0];
                    if (clickedOnItem.PrimaryItemAttribute == StorageItemTypes.Folder)
                    {
                        // Add location to MRU List
                        mostRecentlyUsed.Add(await StorageFolder.GetFolderFromPathAsync(selectedItemPath));

                        CurrentInstance.ViewModel.WorkingDirectory = selectedItemPath;
                        CurrentInstance.NavigationToolbar.PathControlDisplayText = selectedItemPath;

                        (CurrentInstance.ContentPage as BaseLayout).AssociatedViewModel.EmptyTextState.IsVisible = Visibility.Collapsed;
                        CurrentInstance.ContentFrame.Navigate(sourcePageType, selectedItemPath, new SuppressNavigationTransitionInfo());
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
                            await InvokeWin32Component(clickedOnItem.ItemPath);
                        }
                    }
                }
                else if (selectedItemCount > 1)
                {
                    foreach (ListedItem clickedOnItem in (CurrentInstance.ContentPage as BaseLayout).SelectedItems)
                    {
                        if (clickedOnItem.PrimaryItemAttribute == StorageItemTypes.Folder)
                        {
                            instanceTabsView.AddNewTab(typeof(ModernShellPage), clickedOnItem.ItemPath);
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
                                await InvokeWin32Component(clickedOnItem.ItemPath);
                            }
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                MessageDialog dialog = new MessageDialog("The file you are attempting to access may have been moved or deleted.", "File Not Found");
                await dialog.ShowAsync();
                NavigationActions.Refresh_Click(null, null);
            }
        }

        public void CloseTab()
        {
            if (((Window.Current.Content as Frame).Content as InstanceTabsView).TabStrip.TabItems.Count == 1)
            {
                Application.Current.Exit();
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

        public async void ShowPropertiesButton_Click(object sender, RoutedEventArgs e)
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                AppWindow appWindow = await AppWindow.TryCreateAsync();
                Frame frame = new Frame();
                appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                frame.Navigate(typeof(Properties), null, new SuppressNavigationTransitionInfo());
                WindowManagementPreview.SetPreferredMinSize(appWindow, new Size(400, 475));

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
                App.PropertiesDialogDisplay.propertiesFrame.Navigate(typeof(Properties), (App.CurrentInstance.ContentPage as BaseLayout).SelectedItem, new SuppressNavigationTransitionInfo());
                await App.PropertiesDialogDisplay.ShowAsync(ContentDialogPlacement.Popup);
            }
        }

        public async void ShowFolderPropertiesButton_Click(object sender, RoutedEventArgs e)
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                AppWindow appWindow = await AppWindow.TryCreateAsync();
                Frame frame = new Frame();
                frame.Navigate(typeof(Properties), null, new SuppressNavigationTransitionInfo());
                WindowManagementPreview.SetPreferredMinSize(appWindow, new Size(400, 475));
                appWindow.RequestSize(new Size(400, 475));
                appWindow.Title = ResourceController.GetTranslation("PropertiesTitle");

                ElementCompositionPreview.SetAppWindowContent(appWindow, frame);
                AppWindows.Add(frame.UIContext, appWindow);

                appWindow.Closed += delegate
                {
                    Interaction.AppWindows.Remove(frame.UIContext);
                    frame.Content = null;
                    appWindow = null;
                };

                await appWindow.TryShowAsync();
            }
            else
            {
                App.PropertiesDialogDisplay.propertiesFrame.Tag = App.PropertiesDialogDisplay;
                App.PropertiesDialogDisplay.propertiesFrame.Navigate(typeof(Properties), App.CurrentInstance.ViewModel.CurrentFolder, new SuppressNavigationTransitionInfo());
                await App.PropertiesDialogDisplay.ShowAsync(ContentDialogPlacement.Popup);
            }
        }

        private async void Manager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequestDeferral dataRequestDeferral = args.Request.GetDeferral();
            List<IStorageItem> items = new List<IStorageItem>();
            if (App.CurrentInstance.CurrentPageType == typeof(GenericFileBrowser))
            {
                var CurrentInstance = App.CurrentInstance;

                foreach (ListedItem li in (CurrentInstance.ContentPage as BaseLayout).SelectedItems)
                {
                    if (li.PrimaryItemAttribute == StorageItemTypes.Folder)
                    {
                        var folderAsItem = await StorageFolder.GetFolderFromPathAsync(li.ItemPath);
                        items.Add(folderAsItem);
                    }
                    else
                    {
                        var fileAsItem = await StorageFile.GetFileFromPathAsync(li.ItemPath);
                        items.Add(fileAsItem);
                    }
                }
            }
            else if (App.CurrentInstance.CurrentPageType == typeof(PhotoAlbum))
            {
                foreach (ListedItem li in (CurrentInstance.ContentPage as BaseLayout).SelectedItems)
                {
                    if (li.PrimaryItemAttribute == StorageItemTypes.Folder)
                    {
                        var folderAsItem = await StorageFolder.GetFolderFromPathAsync(li.ItemPath);
                        items.Add(folderAsItem);
                    }
                    else
                    {
                        var fileAsItem = await StorageFile.GetFileFromPathAsync(li.ItemPath);
                        items.Add(fileAsItem);
                    }
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
            if (App.AppSettings.ShowConfirmDeleteDialog == true) //check if the setting to show a confirmation dialog is on
            {
                var dialog = new ConfirmDeleteDialog();
                await dialog.ShowAsync();

                if (dialog.Result != MyResult.Delete) //delete selected  item(s) if the result is yes
                {
                    return; //return if the result isn't delete
                }
            }

            try
            {
                var CurrentInstance = App.CurrentInstance;
                List<ListedItem> selectedItems = new List<ListedItem>();
                foreach (ListedItem selectedItem in (CurrentInstance.ContentPage as BaseLayout).SelectedItems)
                {
                    selectedItems.Add(selectedItem);
                }
                int itemsDeleted = 0;
                if (selectedItems.Count > 3)
                {
                    (App.CurrentInstance as ModernShellPage).UpdateProgressFlyout(InteractionOperationType.DeleteItems, itemsDeleted, selectedItems.Count);
                }

                foreach (ListedItem storItem in selectedItems)
                {
                    if (selectedItems.Count > 3) { (App.CurrentInstance as ModernShellPage).UpdateProgressFlyout(InteractionOperationType.DeleteItems, ++itemsDeleted, selectedItems.Count); }

                    try
                    {
                        if (storItem.PrimaryItemAttribute == StorageItemTypes.File)
                        {
                            var item = await StorageFile.GetFileFromPathAsync(storItem.ItemPath);

                            if (App.InteractionViewModel.PermanentlyDelete)
                            {
                                await item.DeleteAsync(StorageDeleteOption.PermanentDelete);
                            }
                            else
                            {
                                await item.DeleteAsync(StorageDeleteOption.Default);
                            }
                        }
                        else
                        {
                            var item = await StorageFolder.GetFolderFromPathAsync(storItem.ItemPath);

                            if (App.InteractionViewModel.PermanentlyDelete)
                            {
                                await item.DeleteAsync(StorageDeleteOption.PermanentDelete);
                            }
                            else
                            {
                                await item.DeleteAsync(StorageDeleteOption.Default);
                            }
                        }
                    }
                    catch (FileLoadException)
                    {
                        // try again
                        if (storItem.PrimaryItemAttribute == StorageItemTypes.File)
                        {
                            var item = await StorageFile.GetFileFromPathAsync(storItem.ItemPath);

                            if (App.InteractionViewModel.PermanentlyDelete)
                            {
                                await item.DeleteAsync(StorageDeleteOption.PermanentDelete);
                            }
                            else
                            {
                                await item.DeleteAsync(StorageDeleteOption.Default);
                            }
                        }
                        else
                        {
                            var item = await StorageFolder.GetFolderFromPathAsync(storItem.ItemPath);

                            if (App.InteractionViewModel.PermanentlyDelete)
                            {
                                await item.DeleteAsync(StorageDeleteOption.PermanentDelete);
                            }
                            else
                            {
                                await item.DeleteAsync(StorageDeleteOption.Default);
                            }
                        }
                    }

                    CurrentInstance.ViewModel.RemoveFileOrFolder(storItem);
                }
                App.CurrentInstance.NavigationToolbar.CanGoForward = false;
            }
            catch (UnauthorizedAccessException)
            {
                MessageDialog AccessDeniedDialog = new MessageDialog("Access Denied", "Unable to delete this item");
                await AccessDeniedDialog.ShowAsync();
            }
            catch (FileNotFoundException)
            {
                Debug.WriteLine("Attention: Tried to delete an item that could be found");
            }

            App.InteractionViewModel.PermanentlyDelete = false; //reset PermanentlyDelete flag
        }

        public void RenameItem_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentInstance.CurrentPageType == typeof(GenericFileBrowser))
            {
                var fileBrowser = App.CurrentInstance.ContentPage as GenericFileBrowser;
                if (fileBrowser.AllView.SelectedItem != null)
                    fileBrowser.AllView.CurrentColumn = fileBrowser.AllView.Columns[1];
                fileBrowser.AllView.BeginEdit();
            }
            else if (App.CurrentInstance.CurrentPageType == typeof(PhotoAlbum))
            {
                var photoAlbum = App.CurrentInstance.ContentPage as PhotoAlbum;
                photoAlbum.StartRename();
            }
        }

        public async Task<bool> RenameFileItem(ListedItem item, string oldName, string newName)
        {
            if (oldName == newName)
                return true;

            if (!string.IsNullOrWhiteSpace(newName))
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

            CurrentInstance.NavigationToolbar.CanGoForward = false;
            return true;
        }

        public async void CutItem_Click(object sender, RoutedEventArgs e)
        {
            DataPackage dataPackage = new DataPackage
            {
                RequestedOperation = DataPackageOperation.Move
            };
            List<IStorageItem> items = new List<IStorageItem>();
            var CurrentInstance = App.CurrentInstance;
            if ((CurrentInstance.ContentPage as BaseLayout).SelectedItems.Count != 0)
            {
                // First, reset DataGrid Rows that may be in "cut" command mode
                foreach (ListedItem listedItem in CurrentInstance.ViewModel.FilesAndFolders)
                {
                    if (App.CurrentInstance.CurrentPageType == typeof(GenericFileBrowser))
                    {
                        FrameworkElement element = (CurrentInstance.ContentPage as GenericFileBrowser).AllView.Columns[0].GetCellContent(listedItem);
                        if (element != null)
                            element.Opacity = 1;
                    }
                    else if (App.CurrentInstance.CurrentPageType == typeof(PhotoAlbum))
                    {
                        List<Grid> itemContentGrids = new List<Grid>();
                        GridViewItem gridViewItem = (CurrentInstance.ContentPage as PhotoAlbum).FileList.ContainerFromItem(listedItem) as GridViewItem;
                        if (gridViewItem == null)
                            continue;
                        FindChildren<Grid>(itemContentGrids, gridViewItem);
                        var imageOfItem = itemContentGrids.Find(x => x.Tag?.ToString() == "ItemImage");
                        imageOfItem.Opacity = 1;
                    }
                }

                foreach (ListedItem listedItem in CurrentInstance.ContentPage.SelectedItems)
                {
                    // Dim opacities accordingly
                    if (App.CurrentInstance.CurrentPageType == typeof(GenericFileBrowser))
                    {
                        (CurrentInstance.ContentPage as GenericFileBrowser).AllView.Columns[0].GetCellContent(listedItem).Opacity = 0.4;
                    }
                    else if (App.CurrentInstance.CurrentPageType == typeof(PhotoAlbum))
                    {
                        GridViewItem itemToDimForCut = (GridViewItem)(CurrentInstance.ContentPage as PhotoAlbum).FileList.ContainerFromItem(listedItem);
                        List<Grid> itemContentGrids = new List<Grid>();
                        FindChildren<Grid>(itemContentGrids, itemToDimForCut);
                        var imageOfItem = itemContentGrids.Find(x => x.Tag?.ToString() == "ItemImage");
                        imageOfItem.Opacity = 0.4;
                    }

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
            dataPackage.SetStorageItems(items);
            Clipboard.SetContent(dataPackage);
            Clipboard.Flush();
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
            if (App.CurrentInstance.CurrentPageType == typeof(GenericFileBrowser))
            {
                var CurrentInstance = App.CurrentInstance;
                CopySourcePath = CurrentInstance.ViewModel.WorkingDirectory;

                if ((CurrentInstance.ContentPage as BaseLayout).SelectedItems.Count != 0)
                {
                    foreach (ListedItem StorItem in (CurrentInstance.ContentPage as BaseLayout).SelectedItems)
                    {
                        if (StorItem.PrimaryItemAttribute == StorageItemTypes.File)
                        {
                            var item = await StorageFile.GetFileFromPathAsync(StorItem.ItemPath);
                            items.Add(item);
                        }
                        else
                        {
                            var item = await StorageFolder.GetFolderFromPathAsync(StorItem.ItemPath);
                            items.Add(item);
                        }
                    }
                }
            }
            else if (App.CurrentInstance.CurrentPageType == typeof(PhotoAlbum))
            {
                CopySourcePath = CurrentInstance.ViewModel.WorkingDirectory;

                if ((CurrentInstance.ContentPage as BaseLayout).SelectedItems.Count != 0)
                {
                    foreach (ListedItem StorItem in (CurrentInstance.ContentPage as BaseLayout).SelectedItems)
                    {
                        if (StorItem.PrimaryItemAttribute == StorageItemTypes.File)
                        {
                            var item = await StorageFile.GetFileFromPathAsync(StorItem.ItemPath);
                            items.Add(item);
                        }
                        else
                        {
                            var item = await StorageFolder.GetFolderFromPathAsync(StorItem.ItemPath);
                            items.Add(item);
                        }
                    }
                }
            }
            if (items?.Count > 0)
            {
                dataPackage.SetStorageItems(items);
                Clipboard.SetContent(dataPackage);
                Clipboard.Flush();
            }
        }

        private enum ImpossibleActionResponseTypes
        {
            Skip,
            Abort
        }

        public async void PasteItem_ClickAsync(object sender, RoutedEventArgs e)
        {
            DataPackageView packageView = Clipboard.GetContent();
            string destinationPath = CurrentInstance.ViewModel.WorkingDirectory;

            await PasteItems(packageView, destinationPath, packageView.RequestedOperation);
        }

        public async Task PasteItems(DataPackageView packageView, string destinationPath, DataPackageOperation acceptedOperation)
        {
            itemsToPaste = await packageView.GetStorageItemsAsync();
            HashSet<IStorageItem> pastedItems = new HashSet<IStorageItem>();
            itemsPasted = 0;
            if (itemsToPaste.Count > 3)
            {
                (App.CurrentInstance as ModernShellPage).UpdateProgressFlyout(InteractionOperationType.PasteItems, itemsPasted, itemsToPaste.Count);
            }

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
                        StorageFolder pastedFolder = await CloneDirectoryAsync(item.Path, destinationPath, item.Name, false);
                        pastedItems.Add(pastedFolder);
                        if (destinationPath == CurrentInstance.ViewModel.WorkingDirectory)
                            CurrentInstance.ViewModel.AddFolder(pastedFolder.Path);
                    }
                }
                else if (item.IsOfType(StorageItemTypes.File))
                {
                    if (itemsToPaste.Count > 3)
                    {
                        (App.CurrentInstance as ModernShellPage).UpdateProgressFlyout(InteractionOperationType.PasteItems, ++itemsPasted, itemsToPaste.Count);
                    }
                    StorageFile clipboardFile = await StorageFile.GetFileFromPathAsync(item.Path);
                    StorageFile pastedFile = await clipboardFile.CopyAsync(await StorageFolder.GetFolderFromPathAsync(destinationPath), item.Name, NameCollisionOption.GenerateUniqueName);
                    pastedItems.Add(pastedFile);
                    if (destinationPath == CurrentInstance.ViewModel.WorkingDirectory)
                        CurrentInstance.ViewModel.AddFile(pastedFile.Path);
                }
            }

            if (acceptedOperation == DataPackageOperation.Move)
            {
                foreach (IStorageItem item in itemsToPaste)
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
                    ListedItem listedItem = CurrentInstance.ViewModel.FilesAndFolders.FirstOrDefault(listedItem => listedItem.ItemPath.Equals(item.Path, StringComparison.OrdinalIgnoreCase));
                    if (listedItem != null)
                        CurrentInstance.ViewModel.RemoveFileOrFolder(listedItem);
                }
            }
            if (destinationPath == CurrentInstance.ViewModel.WorkingDirectory)
            {
                List<string> pastedItemPaths = pastedItems.Select(item => item.Path).ToList();
                List<ListedItem> copiedItems = CurrentInstance.ViewModel.FilesAndFolders.Where(listedItem => pastedItemPaths.Contains(listedItem.ItemPath)).ToList();
                CurrentInstance.ContentPage.SelectedItems = copiedItems;
                CurrentInstance.ContentPage.FocusSelectedItems();
            }
            packageView.ReportOperationCompleted(acceptedOperation);
        }

        public async Task<StorageFolder> CloneDirectoryAsync(string SourcePath, string DestinationPath, string sourceRootName, bool suppressProgressFlyout)
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
                        (App.CurrentInstance as ModernShellPage).UpdateProgressFlyout(InteractionOperationType.PasteItems, ++itemsPasted, itemsToPaste.Count + (await SourceFolder.GetItemsAsync()).Count);
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
                        (App.CurrentInstance as ModernShellPage).UpdateProgressFlyout(InteractionOperationType.PasteItems, ++itemsPasted, itemsToPaste.Count + (await SourceFolder.GetItemsAsync()).Count);
                    }
                }

                await CloneDirectoryAsync(folderinSourceDir.Path, DestinationFolder.Path, folderinSourceDir.Name, false);
            }
            return createdRoot;
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
            StorageFile selectedItem = null;
            if (CurrentInstance.ContentFrame.CurrentSourcePageType == typeof(GenericFileBrowser))
            {
                var page = (CurrentInstance.ContentPage as GenericFileBrowser);
                selectedItem = await StorageFile.GetFileFromPathAsync(CurrentInstance.ViewModel.FilesAndFolders[page.AllView.SelectedIndex].ItemPath);
            }
            else if (CurrentInstance.ContentFrame.CurrentSourcePageType == typeof(PhotoAlbum))
            {
                var page = (CurrentInstance.ContentPage as PhotoAlbum);
                selectedItem = await StorageFile.GetFileFromPathAsync(CurrentInstance.ViewModel.FilesAndFolders[page.FileList.SelectedIndex].ItemPath);
            }

            ExtractFilesDialog extractFilesDialog = new ExtractFilesDialog(CurrentInstance.ViewModel.WorkingDirectory);
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

                (App.CurrentInstance.ContentPage as BaseLayout).AssociatedViewModel.LoadIndicator.IsVisible = Visibility.Visible;

                foreach (ZipArchiveEntry archiveEntry in zipArchive.Entries)
                {
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
                        (App.CurrentInstance.ContentPage as BaseLayout).AssociatedViewModel.LoadIndicator.IsVisible = Visibility.Collapsed;
                    }
                }
                await CloneDirectoryAsync(destFolder_InBuffer.Path, destinationPath, destFolder_InBuffer.Name, true)
                    .ContinueWith(async (x) =>
                {
                    await destFolder_InBuffer.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    await CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        Frame rootFrame = Window.Current.Content as Frame;
                        var instanceTabsView = rootFrame.Content as InstanceTabsView;
                        instanceTabsView.AddNewTab(typeof(ModernShellPage), destinationPath + "\\" + selectedItem.DisplayName + "_Extracted");
                    });
                });
            }
            else if (((bool)ApplicationData.Current.LocalSettings.Values["Extract_Destination_Cancelled"]) == true)
            {
                return;
            }
        }

        public void SelectAllItems()
        {
            CurrentInstance.ContentPage.SelectedItems = App.CurrentInstance.ViewModel.FilesAndFolders.ToList();
        }

        public void InvertAllItems()
        {
            List<ListedItem> oldSelectedItems = CurrentInstance.ContentPage.SelectedItems;
            List<ListedItem> newSelectedItems = CurrentInstance.ViewModel.FilesAndFolders.ToList();
            foreach (ListedItem listedItem in oldSelectedItems)
                newSelectedItems.Remove(listedItem);
            CurrentInstance.ContentPage.SelectedItems = newSelectedItems;
        }

        public void ClearAllItems()
        {
            CurrentInstance.ContentPage.SelectedItems = new List<ListedItem>();
        }

        public void ToggleQuickLook_Click(object sender, RoutedEventArgs e)
        {
            ToggleQuickLook();
        }

        public async void ToggleQuickLook()
        {
            try
            {
                string selectedItemPath = null;
                int selectedItemCount;
                Type sourcePageType = App.CurrentInstance.CurrentPageType;
                selectedItemCount = (CurrentInstance.ContentPage as BaseLayout).SelectedItems.Count;
                if (selectedItemCount == 1)
                {
                    selectedItemPath = (CurrentInstance.ContentPage as BaseLayout).SelectedItems[0].ItemPath;
                }

                if (selectedItemCount == 1)
                {
                    var clickedOnItem = (CurrentInstance.ContentPage as BaseLayout).SelectedItems[0];

                    Debug.WriteLine("Toggle QuickLook");
                    ApplicationData.Current.LocalSettings.Values["path"] = clickedOnItem.ItemPath;
                    ApplicationData.Current.LocalSettings.Values["Arguments"] = "ToggleQuickLook";
                    await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
                }
            }
            catch (FileNotFoundException)
            {
                MessageDialog dialog = new MessageDialog("The file you are attempting to preview may have been moved or deleted.", "File Not Found");
                var task = dialog.ShowAsync();
                task.AsTask().Wait();
                NavigationActions.Refresh_Click(null, null);
            }
        }

        public void PushJumpChar(char letter)
        {
            App.CurrentInstance.ViewModel.JumpString += letter.ToString().ToLower();
        }

        public async Task<string> GetHashForFile(ListedItem fileItem, string nameOfAlg)
        {
            HashAlgorithmProvider algorithmProvider = HashAlgorithmProvider.OpenAlgorithm(nameOfAlg);
            var itemFromPath = await StorageFile.GetFileFromPathAsync(fileItem.ItemPath);
            var stream = await itemFromPath.OpenStreamForReadAsync();
            var inputStream = stream.AsInputStream();
            uint capacity = 100000000;
            Windows.Storage.Streams.Buffer buffer = new Windows.Storage.Streams.Buffer(capacity);
            var hash = algorithmProvider.CreateHash();
            while (true)
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
            }
            inputStream.Dispose();
            stream.Dispose();
            return CryptographicBuffer.EncodeToHexString(hash.GetValueAndReset()).ToLower();
        }
    }
}