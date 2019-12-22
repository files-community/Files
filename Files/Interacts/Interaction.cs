using Files.Dialogs;
using Files.Filesystem;
using Files.Navigation;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Files.Interacts
{
    public class Interaction
    {
        private ProHome currentInstance;
        InstanceTabsView instanceTabsView;
        public Interaction()
        {
            currentInstance = App.OccupiedInstance;
            instanceTabsView = (Window.Current.Content as Frame).Content as InstanceTabsView;
        }

        public void List_ItemClick(object sender, DoubleTappedRoutedEventArgs e)
        {
            OpenSelectedItems(false);
        }

        public async void OpenInNewWindowItem_Click(object sender, RoutedEventArgs e)
        {
            var CurrentSourceType = App.OccupiedInstance.ItemDisplayFrame.CurrentSourcePageType;
            if (CurrentSourceType == typeof(GenericFileBrowser))
            {
                var items = (currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems;
                foreach (ListedItem listedItem in items)
                {
                    var selectedItemPath = listedItem.FilePath;
                    var folderUri = new Uri("files-uwp:" + "?folder=" + @selectedItemPath);
                    await Launcher.LaunchUriAsync(folderUri);
                }

            }
            else if (CurrentSourceType == typeof(PhotoAlbum))
            {
                var items = (currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems;
                foreach (ListedItem listedItem in items)
                {
                    var selectedItemPath = listedItem.FilePath;
                    var folderUri = new Uri("files-uwp:" + "?folder=" + @selectedItemPath);
                    await Launcher.LaunchUriAsync(folderUri);
                }
            }
        }

        public void OpenDirectoryInNewTab_Click(object sender, RoutedEventArgs e)
        {
            var CurrentSourceType = App.OccupiedInstance.ItemDisplayFrame.CurrentSourcePageType;
            if(CurrentSourceType == typeof(GenericFileBrowser))
            {
                var items = (currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems;
                foreach (ListedItem listedItem in items)
                {
                    instanceTabsView.AddNewTab(typeof(ProHome), listedItem.FilePath);
                }
                
            }
            else if(CurrentSourceType == typeof(PhotoAlbum))
            {
                var items = (currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems;
                foreach (ListedItem listedItem in items)
                {
                    instanceTabsView.AddNewTab(typeof(ProHome), listedItem.FilePath);
                }
            }
        }

        public async void OpenDirectoryInTerminal(object sender, RoutedEventArgs e)
        {

            ApplicationData.Current.LocalSettings.Values["Application"] = "cmd.exe";
            if(App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                ApplicationData.Current.LocalSettings.Values["Arguments"] = "/k \"cd /d "+ currentInstance.instanceViewModel.Universal.path + "&& title Command Prompt" + "\""; 
            }
            else if(App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(PhotoAlbum))
            {
                ApplicationData.Current.LocalSettings.Values["Arguments"] = "/k \"cd /d " + currentInstance.instanceViewModel.Universal.path + "&& title Command Prompt" + "\"";
            }

            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }

        public async void PinItem_Click(object sender, RoutedEventArgs e)
        {
            if (App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                StorageFolder cacheFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;
                List<string> itemsToPin = new List<string>();


                foreach (ListedItem listedItem in (currentInstance.ItemDisplayFrame.Content as GenericFileBrowser).AllView.SelectedItems)
                {
                    itemsToPin.Add(listedItem.FilePath);
                }
                var ListFile = await cacheFolder.CreateFileAsync("PinnedItems.txt", CreationCollisionOption.OpenIfExists);
                await FileIO.AppendLinesAsync(ListFile, itemsToPin);

                foreach (string itemPath in itemsToPin)
                {
                    try
                    {
                        StorageFolder fol = await StorageFolder.GetFolderFromPathAsync(itemPath);
                        var name = fol.DisplayName;
                        var content = name;
                        var icon = "\uE8B7";

                        bool isDuplicate = false;
                        foreach (SidebarItem sbi in App.sideBarItems)
                        {
                            if (!string.IsNullOrWhiteSpace(sbi.Path) && !sbi.isDefaultLocation)
                            {
                                if (sbi.Path.ToString() == itemPath)
                                {
                                    isDuplicate = true;

                                }
                            }

                        }

                        if (!isDuplicate)
                        {
                            App.sideBarItems.Add(new SidebarItem() { Path = itemPath, IconGlyph = icon, isDefaultLocation = false, Text = content });
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                    catch (FileNotFoundException ex)
                    {
                        Debug.WriteLine("Pinned item was deleted and will be removed from the file lines list soon: " + ex.Message);
                        App.LinesToRemoveFromFile.Add(itemPath);
                    }
                    catch (System.Runtime.InteropServices.COMException ex)
                    {
                        Debug.WriteLine("Pinned item's drive was ejected and will be removed from the file lines list soon: " + ex.Message);
                        App.LinesToRemoveFromFile.Add(itemPath);
                    }
                }

            }
            else if (App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(PhotoAlbum))
            {
                StorageFolder cacheFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;
                List<string> items = new List<string>();

                try
                {
                    foreach (ListedItem listedItem in (currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems)
                    {
                        items.Add(listedItem.FilePath);
                    }
                    var ListFile = await cacheFolder.GetFileAsync("PinnedItems.txt");
                    await FileIO.AppendLinesAsync(ListFile, items);
                }
                catch (FileNotFoundException)
                {
                    foreach (ListedItem listedItem in (currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems)
                    {
                        items.Add(listedItem.FilePath);
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
                            foreach (SidebarItem sbi in App.sideBarItems)
                            {
                                if (!string.IsNullOrWhiteSpace(sbi.Path) && !sbi.isDefaultLocation)
                                {
                                    if (sbi.Path.ToString() == itemPath)
                                    {
                                        isDuplicate = true;

                                    }
                                }
                            }

                            if (!isDuplicate)
                            {
                                App.sideBarItems.Add(new SidebarItem() { Path = itemPath, IconGlyph = icon, isDefaultLocation = false, Text = content });
                            }
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                        catch (FileNotFoundException ex)
                        {
                            Debug.WriteLine("Pinned item was deleted and will be removed from the file lines list soon: " + ex.Message);
                            App.LinesToRemoveFromFile.Add(itemPath);
                        }
                        catch (System.Runtime.InteropServices.COMException ex)
                        {
                            Debug.WriteLine("Pinned item's drive was ejected and will be removed from the file lines list soon: " + ex.Message);
                            App.LinesToRemoveFromFile.Add(itemPath);
                        }
                    }
                }
            }
            App.RemoveStaleSidebarItems();
        }

        public void GetPath_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.Clear();
            DataPackage data = new DataPackage();
            if(App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                data.SetText(currentInstance.instanceViewModel.Universal.path);
                Clipboard.SetContent(data);
                Clipboard.Flush();
            }
            else if(App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(PhotoAlbum))
            {
                data.SetText(currentInstance.instanceViewModel.Universal.path);
                Clipboard.SetContent(data);
                Clipboard.Flush();
            }
        }

        public static async Task LaunchExe(string ApplicationPath)
        {
            Debug.WriteLine("Launching EXE in FullTrustProcess");
            ApplicationData.Current.LocalSettings.Values["Application"] = ApplicationPath;
            ApplicationData.Current.LocalSettings.Values["Arguments"] = null;
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
            if(RowPressed != null)
            {
                var ObjectPressed = ((ReadOnlyObservableCollection<ListedItem>)dataGrid.ItemsSource)[RowPressed.GetIndex()];
                // Check if RightTapped row is currently selected
                var CurrentInstance = App.OccupiedInstance;
                if ((currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems.Contains(ObjectPressed))
                    return;
                // The following code is only reachable when a user RightTapped an unselected row
                dataGrid.SelectedItems.Clear();
                dataGrid.SelectedItems.Add(ObjectPressed);
            }
            
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
        
        public void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectedItems(true);
        }

        private async void OpenSelectedItems(bool displayApplicationPicker)
        {
            try
            {
                string selectedItemPath = null;
                int selectedItemCount;
                Type sourcePageType = App.OccupiedInstance.ItemDisplayFrame.SourcePageType;
                selectedItemCount = (currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems.Count;
                if (selectedItemCount == 1)
                {
                    selectedItemPath = (currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems[0].FilePath;
                }

                // Access MRU List
                var mostRecentlyUsed = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;

                if (selectedItemCount == 1)
                {
                    var clickedOnItem = (currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems[0];
                    if (clickedOnItem.FileType == "Folder")
                    {
                        // Add location to MRU List
                        mostRecentlyUsed.Add(await StorageFolder.GetFolderFromPathAsync(selectedItemPath));

                        currentInstance.instanceViewModel.Universal.path = selectedItemPath;
                        currentInstance.PathText.Text = selectedItemPath;

                        (currentInstance.ItemDisplayFrame.Content as BaseLayout).AssociatedViewModel.EmptyTextState.isVisible = Visibility.Collapsed;
                        if (selectedItemPath == Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory))
                        {
                            currentInstance.PathText.Text = "Desktop";
                            App.OccupiedInstance.LocationsList.SelectedItem = App.sideBarItems.First(x => (x as SidebarItem).Path.Equals(App.DesktopPath, StringComparison.OrdinalIgnoreCase));
                            currentInstance.ItemDisplayFrame.Navigate(sourcePageType, App.DesktopPath, new SuppressNavigationTransitionInfo());

                        }
                        else if (selectedItemPath == Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
                        {
                            currentInstance.PathText.Text = "Documents";
                            App.OccupiedInstance.LocationsList.SelectedItem = App.sideBarItems.First(x => (x as SidebarItem).Path.Equals(App.DocumentsPath, StringComparison.OrdinalIgnoreCase));
                            currentInstance.ItemDisplayFrame.Navigate(sourcePageType, App.DocumentsPath, new SuppressNavigationTransitionInfo());
                        }
                        else if (selectedItemPath == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
                        {
                            currentInstance.PathText.Text = "Downloads";
                            App.OccupiedInstance.LocationsList.SelectedItem = App.sideBarItems.First(x => (x as SidebarItem).Path.Equals(App.DownloadsPath, StringComparison.OrdinalIgnoreCase));
                            currentInstance.ItemDisplayFrame.Navigate(sourcePageType, App.DownloadsPath, new SuppressNavigationTransitionInfo());
                        }
                        else if (selectedItemPath == Environment.GetFolderPath(Environment.SpecialFolder.MyPictures))
                        {
                            currentInstance.PathText.Text = "Pictures";
                            App.OccupiedInstance.LocationsList.SelectedItem = App.sideBarItems.First(x => (x as SidebarItem).Path.Equals(App.PicturesPath, StringComparison.OrdinalIgnoreCase));
                            currentInstance.ItemDisplayFrame.Navigate(sourcePageType, App.PicturesPath, new SuppressNavigationTransitionInfo());
                        }
                        else if (selectedItemPath == Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))
                        {
                            currentInstance.PathText.Text = "Music";
                            App.OccupiedInstance.LocationsList.SelectedItem = App.sideBarItems.First(x => (x as SidebarItem).Path.Equals(App.MusicPath, StringComparison.OrdinalIgnoreCase));
                            currentInstance.ItemDisplayFrame.Navigate(sourcePageType, App.MusicPath, new SuppressNavigationTransitionInfo());
                        }
                        else if (selectedItemPath == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
                        {
                            currentInstance.PathText.Text = "OneDrive";
                            App.OccupiedInstance.DrivesList.SelectedItem = App.foundDrives.First(x => (x as DriveItem).tag.ToString().Equals("OneDrive", StringComparison.OrdinalIgnoreCase));
                            currentInstance.ItemDisplayFrame.Navigate(sourcePageType, App.OneDrivePath, new SuppressNavigationTransitionInfo());
                        }
                        else if (selectedItemPath == Environment.GetFolderPath(Environment.SpecialFolder.MyVideos))
                        {
                            currentInstance.PathText.Text = "Videos";
                            App.OccupiedInstance.LocationsList.SelectedItem = App.sideBarItems.First(x => (x as SidebarItem).Path.Equals(App.VideosPath, StringComparison.OrdinalIgnoreCase));
                            currentInstance.ItemDisplayFrame.Navigate(sourcePageType, App.VideosPath, new SuppressNavigationTransitionInfo());
                        }
                        else
                        {
                            if (selectedItemPath.Split(@"\")[0].Contains("C:"))
                            {
                                currentInstance.DrivesList.SelectedItem = App.foundDrives.Where(x => (x as DriveItem).tag == "C:\\").First();
                            }
                            else
                            {
                                currentInstance.DrivesList.SelectedItem = App.foundDrives.Where(x => (x as DriveItem).tag.Contains(selectedItemPath.Split(@"\")[0])).First();
                            }
                            currentInstance.ItemDisplayFrame.Navigate(sourcePageType, selectedItemPath, new SuppressNavigationTransitionInfo());
                        }
                    }
                    else if (clickedOnItem.FileType == "Application")
                    {
                        // Add location to MRU List
                        mostRecentlyUsed.Add(await StorageFile.GetFileFromPathAsync(clickedOnItem.FilePath));
                        await LaunchExe(clickedOnItem.FilePath);
                    }
                    else
                    {
                        StorageFile file = await StorageFile.GetFileFromPathAsync(clickedOnItem.FilePath);
                        // Add location to MRU List
                        mostRecentlyUsed.Add(file);
                        var options = new LauncherOptions
                        {
                            DisplayApplicationPicker = displayApplicationPicker
                        };
                        await Launcher.LaunchFileAsync(file, options);
                    }
                }
                else if(selectedItemCount > 1)
                {
                    foreach (ListedItem clickedOnItem in (currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems)
                    {

                        if (clickedOnItem.FileType == "Folder")
                        {
                            instanceTabsView.AddNewTab(typeof(ProHome), clickedOnItem.FilePath);
                        }
                        else if (clickedOnItem.FileType == "Application")
                        {
                            // Add location to MRU List
                            mostRecentlyUsed.Add(await StorageFile.GetFileFromPathAsync(clickedOnItem.FilePath));
                            await LaunchExe(clickedOnItem.FilePath);
                        }
                        else
                        {
                            StorageFile file = await StorageFile.GetFileFromPathAsync(clickedOnItem.FilePath);
                            // Add location to MRU List
                            mostRecentlyUsed.Add(file);
                            var options = new LauncherOptions
                            {
                                DisplayApplicationPicker = displayApplicationPicker
                            };
                            await Launcher.LaunchFileAsync(file, options);
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

        public void ShareItem_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager manager = DataTransferManager.GetForCurrentView();
            manager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(Manager_DataRequested);
            DataTransferManager.ShowShareUI();
        }

        public async void ShowPropertiesButton_Click(object sender, RoutedEventArgs e)
        {
            App.propertiesDialog.accessiblePropertiesFrame.Tag = App.propertiesDialog;
            App.propertiesDialog.accessiblePropertiesFrame.Navigate(typeof(Properties), (App.OccupiedInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItem, new SuppressNavigationTransitionInfo());
            await App.propertiesDialog.ShowAsync(ContentDialogPlacement.Popup);
        }

        public async void ShowFolderPropertiesButton_Click(object sender, RoutedEventArgs e)
        {
            App.propertiesDialog.accessiblePropertiesFrame.Tag = App.propertiesDialog;
            App.propertiesDialog.accessiblePropertiesFrame.Navigate(typeof(Properties), App.OccupiedInstance.instanceViewModel.currentFolder, new SuppressNavigationTransitionInfo());
            await App.propertiesDialog.ShowAsync(ContentDialogPlacement.Popup);
        }

        private async void Manager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequestDeferral dataRequestDeferral = args.Request.GetDeferral();
            List<IStorageItem> items = new List<IStorageItem>();
            if(App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                var CurrentInstance = App.OccupiedInstance;

                foreach (ListedItem li in (currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems)
                {
                    if (li.FileType == "Folder")
                    {
                        var folderAsItem = await StorageFolder.GetFolderFromPathAsync(li.FilePath);
                        items.Add(folderAsItem);
                    }
                    else
                    {
                        var fileAsItem = await StorageFile.GetFileFromPathAsync(li.FilePath);
                        items.Add(fileAsItem);
                    }
                }
            }
            else if (App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(PhotoAlbum))
            {
                foreach (ListedItem li in (currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems)
                {
                    if (li.FileType == "Folder")
                    {
                        var folderAsItem = await StorageFolder.GetFolderFromPathAsync(li.FilePath);
                        items.Add(folderAsItem);
                    }
                    else
                    {
                        var fileAsItem = await StorageFile.GetFileFromPathAsync(li.FilePath);
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
            try
            {
                var CurrentInstance = App.OccupiedInstance;
                List<ListedItem> selectedItems = new List<ListedItem>();
                foreach (ListedItem selectedItem in (currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems)
                {
                    selectedItems.Add(selectedItem);
                }
                int itemsDeleted = 0;
                if (selectedItems.Count > 3)
                {
                    App.OccupiedInstance.UpdateProgressFlyout(InteractionOperationType.DeleteItems, itemsDeleted, selectedItems.Count);
                }

                foreach (ListedItem storItem in selectedItems)
                {
                    if (selectedItems.Count > 3) { App.OccupiedInstance.UpdateProgressFlyout(InteractionOperationType.DeleteItems, ++itemsDeleted, selectedItems.Count); }

                    try
                    {
                        if (storItem.FileType != "Folder")
                        {
                            var item = await StorageFile.GetFileFromPathAsync(storItem.FilePath);
                            await item.DeleteAsync(StorageDeleteOption.Default);

                        }
                        else
                        {
                            var item = await StorageFolder.GetFolderFromPathAsync(storItem.FilePath);
                            await item.DeleteAsync(StorageDeleteOption.Default);

                        }
                    }
                    catch (FileLoadException)
                    {
                        // try again
                        if (storItem.FileType != "Folder")
                        {
                            var item = await StorageFile.GetFileFromPathAsync(storItem.FilePath);
                            await item.DeleteAsync(StorageDeleteOption.Default);

                        }
                        else
                        {
                            var item = await StorageFolder.GetFolderFromPathAsync(storItem.FilePath);
                            await item.DeleteAsync(StorageDeleteOption.Default);

                        }
                    }

                    currentInstance.instanceViewModel.RemoveFileOrFolder(storItem);
                }
                App.OccupiedInstance.RibbonArea.Forward.IsEnabled = false;

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
        }

        public void RenameItem_Click(object sender, RoutedEventArgs e)
        {
            if (App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                var fileBrowser = App.OccupiedInstance.ItemDisplayFrame.Content as GenericFileBrowser;
                if (fileBrowser.AllView.SelectedItem != null)
                    fileBrowser.AllView.CurrentColumn = fileBrowser.AllView.Columns[1];
                fileBrowser.AllView.BeginEdit();
            }
            else if (App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(PhotoAlbum))
            {
                var photoAlbum = App.OccupiedInstance.ItemDisplayFrame.Content as PhotoAlbum;
                photoAlbum.StartRename();
            }
        }

        public async Task<bool> RenameFileItem(ListedItem item, string oldName, string newName)
        {
            if (oldName == newName)
                return true;
            bool isRenamedSameNameDiffCase = oldName.ToLower() == newName.ToLower();
            try
            {
                if (newName != "")
                {
                    if (item.FileType == "Folder")
                    {
                        var folder = await StorageFolder.GetFolderFromPathAsync(item.FilePath);
                        if (isRenamedSameNameDiffCase)
                            throw new InvalidOperationException();
                        //await folder.RenameAsync(newName, NameCollisionOption.ReplaceExisting);
                        else
                            await folder.RenameAsync(newName, NameCollisionOption.FailIfExists);
                    }
                    else
                    {
                        var file = await StorageFile.GetFileFromPathAsync(item.FilePath);
                        if (isRenamedSameNameDiffCase)
                            throw new InvalidOperationException();
                        //await file.RenameAsync(newName, NameCollisionOption.ReplaceExisting);
                        else
                            await file.RenameAsync(newName, NameCollisionOption.FailIfExists);
                    }
                }
            }
            catch (Exception)
            {
                MessageDialog itemAlreadyExistsDialog = new MessageDialog("An item with this name already exists in this folder", "Try again");
                await itemAlreadyExistsDialog.ShowAsync();
                return false;
            }
            currentInstance.RibbonArea.Forward.IsEnabled = false;
            return true;
        }

        public List<DataGridRow> dataGridRows = new List<DataGridRow>();
        public List<GridViewItem> gridViewItems = new List<GridViewItem>();
        public async void CutItem_Click(object sender, RoutedEventArgs e)
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Move;
            App.pathsToDeleteAfterPaste.Clear();
            List<IStorageItem> items = new List<IStorageItem>();
            if (App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                var CurrentInstance = App.OccupiedInstance;
                if ((currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems.Count != 0)
                {
                    dataGridRows.Clear();
                    FindChildren<DataGridRow>(dataGridRows, (CurrentInstance.ItemDisplayFrame.Content as GenericFileBrowser).AllView);
                    
                    // First, reset DataGrid Rows that may be in "cut" command mode
                    foreach (DataGridRow row in dataGridRows)
                    {
                        if ((CurrentInstance.ItemDisplayFrame.Content as GenericFileBrowser).AllView.Columns[0].GetCellContent(row).Opacity < 1)
                        {
                            (CurrentInstance.ItemDisplayFrame.Content as GenericFileBrowser).AllView.Columns[0].GetCellContent(row).Opacity = 1;
                        }
                    }

                    foreach (ListedItem StorItem in (currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems)
                    {
                        IEnumerator allItems = (CurrentInstance.ItemDisplayFrame.Content as GenericFileBrowser).AllView.ItemsSource.GetEnumerator();
                        int index = -1;
                        while (allItems.MoveNext())
                        {
                            index++;
                            var item = allItems.Current;
                            if(item == StorItem)
                            {
                                DataGridRow dataGridRow = dataGridRows[index];
                                (CurrentInstance.ItemDisplayFrame.Content as GenericFileBrowser).AllView.Columns[0].GetCellContent(dataGridRow).Opacity = 0.4;
                            }
                        }

                        App.pathsToDeleteAfterPaste.Add(StorItem.FilePath);
                        if (StorItem.FileType != "Folder")
                        {
                            var item = await StorageFile.GetFileFromPathAsync(StorItem.FilePath);
                            items.Add(item);
                        }
                        else
                        {
                            var item = await StorageFolder.GetFolderFromPathAsync(StorItem.FilePath);
                            items.Add(item);
                        }
                    }
                }
            }
            else if (App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(PhotoAlbum))
            {
                var CurrentInstance = App.OccupiedInstance;
                if ((currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems.Count != 0)
                {

                    gridViewItems.Clear();
                    FindChildren<GridViewItem>(gridViewItems, (CurrentInstance.ItemDisplayFrame.Content as PhotoAlbum).FileList);

                    // First, reset GridView items that may be in "cut" command mode
                    foreach (GridViewItem gridViewItem in gridViewItems)
                    {
                        List<Grid> itemContentGrids = new List<Grid>();
                        FindChildren<Grid>(itemContentGrids, (CurrentInstance.ItemDisplayFrame.Content as PhotoAlbum).FileList.ContainerFromItem(gridViewItem.Content));
                        var imageOfItem = itemContentGrids.Find(x => x.Tag?.ToString() == "ItemImage");
                        if (imageOfItem.Opacity < 1)
                        {
                            imageOfItem.Opacity = 1;
                        }
                    }

                    foreach (ListedItem StorItem in (currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems)
                    {
                        GridViewItem itemToDimForCut = (GridViewItem) (currentInstance.ItemDisplayFrame.Content as PhotoAlbum).FileList.ContainerFromItem(StorItem);
                        List<Grid> itemContentGrids = new List<Grid>();
                        FindChildren<Grid>(itemContentGrids, (CurrentInstance.ItemDisplayFrame.Content as PhotoAlbum).FileList.ContainerFromItem(itemToDimForCut.Content));
                        var imageOfItem = itemContentGrids.Find(x => x.Tag?.ToString() == "ItemImage");
                        imageOfItem.Opacity = 0.4;
                    
                        App.pathsToDeleteAfterPaste.Add(StorItem.FilePath);
                        if (StorItem.FileType != "Folder")
                        {
                            var item = await StorageFile.GetFileFromPathAsync(StorItem.FilePath);
                            items.Add(item);
                        }
                        else
                        {
                            var item = await StorageFolder.GetFolderFromPathAsync(StorItem.FilePath);
                            items.Add(item);
                        }
                    }
                }
            }
            IEnumerable<IStorageItem> EnumerableOfItems = items;
            dataPackage.SetStorageItems(EnumerableOfItems);
            Clipboard.SetContent(dataPackage);
            Clipboard.Flush();
        }
        public string CopySourcePath;
        public IReadOnlyList<IStorageItem> ItemsToPaste;
        public int itemsPasted;

        public async void CopyItem_ClickAsync(object sender, RoutedEventArgs e)
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            List<IStorageItem> items = new List<IStorageItem>();
            if (App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                var CurrentInstance = App.OccupiedInstance;
                CopySourcePath = currentInstance.instanceViewModel.Universal.path;

                if ((currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems.Count != 0)
                {
                    foreach (ListedItem StorItem in (currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems)
                    {
                        if (StorItem.FileType != "Folder")
                        {
                            var item = await StorageFile.GetFileFromPathAsync(StorItem.FilePath);
                            items.Add(item);
                        }
                        else
                        {
                            var item = await StorageFolder.GetFolderFromPathAsync(StorItem.FilePath);
                            items.Add(item);
                        }
                    }
                }
            }
            else if (App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(PhotoAlbum))
            {
                CopySourcePath = currentInstance.instanceViewModel.Universal.path;

                if ((currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems.Count != 0)
                {
                    foreach (ListedItem StorItem in (currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems)
                    {
                        if (StorItem.FileType != "Folder")
                        {
                            var item = await StorageFile.GetFileFromPathAsync(StorItem.FilePath);
                            items.Add(item);
                        }
                        else
                        {
                            var item = await StorageFolder.GetFolderFromPathAsync(StorItem.FilePath);
                            items.Add(item);
                        }
                    }
                }
            }
            if (items?.Count > 0)
            {
                IEnumerable<IStorageItem> EnumerableOfItems = items;
                dataPackage.SetStorageItems(EnumerableOfItems);
                Clipboard.SetContent(dataPackage);
                Clipboard.Flush();
            }

        }

        public async void PasteItem_ClickAsync(object sender, RoutedEventArgs e)
        {
            string DestinationPath = currentInstance.instanceViewModel.Universal.path;
            int oldCount = currentInstance.instanceViewModel.FilesAndFolders.Count;

            DataPackageView packageView = Clipboard.GetContent();
            ItemsToPaste = await packageView.GetStorageItemsAsync();
            itemsPasted = 0;
            if(ItemsToPaste.Count > 3)
            {
                App.OccupiedInstance.UpdateProgressFlyout(InteractionOperationType.PasteItems, itemsPasted, ItemsToPaste.Count);
            }

            foreach (IStorageItem item in ItemsToPaste)
            {

                if (item.IsOfType(StorageItemTypes.Folder))
                {
                    CloneDirectoryAsync(item.Path, DestinationPath, item.Name, false);
                }
                else if (item.IsOfType(StorageItemTypes.File))
                {
                    if (ItemsToPaste.Count > 3)
                    {
                        App.OccupiedInstance.UpdateProgressFlyout(InteractionOperationType.PasteItems, ++itemsPasted, ItemsToPaste.Count);
                    }
                    StorageFile ClipboardFile = await StorageFile.GetFileFromPathAsync(item.Path);
                    await ClipboardFile.CopyAsync(await StorageFolder.GetFolderFromPathAsync(DestinationPath), item.Name, NameCollisionOption.GenerateUniqueName);
                }
            }

            if (packageView.RequestedOperation == DataPackageOperation.Move)
            {
                foreach (string path in App.pathsToDeleteAfterPaste)
                {
                    if (path.Contains("."))
                    {
                        StorageFile file = await StorageFile.GetFileFromPathAsync(path);
                        await file.DeleteAsync();
                    }
                    if (!path.Contains("."))
                    {
                        StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);
                        await folder.DeleteAsync();
                    }
                }
            }

        }

        public async void CloneDirectoryAsync(string SourcePath, string DestinationPath, string sourceRootName, bool suppressProgressFlyout)
        {
            StorageFolder SourceFolder = await StorageFolder.GetFolderFromPathAsync(SourcePath);
            StorageFolder DestinationFolder = await StorageFolder.GetFolderFromPathAsync(DestinationPath);
            var createdRoot = await DestinationFolder.CreateFolderAsync(sourceRootName, CreationCollisionOption.GenerateUniqueName);
            DestinationFolder = await StorageFolder.GetFolderFromPathAsync(createdRoot.Path);

            foreach (StorageFile fileInSourceDir in await SourceFolder.GetFilesAsync())
            {
                if (ItemsToPaste.Count > 3 && !suppressProgressFlyout)
                {
                    App.OccupiedInstance.UpdateProgressFlyout(InteractionOperationType.PasteItems, ++itemsPasted, ItemsToPaste.Count + (await SourceFolder.GetItemsAsync()).Count);
                }
                await fileInSourceDir.CopyAsync(DestinationFolder, fileInSourceDir.Name, NameCollisionOption.GenerateUniqueName);
            }
            foreach (StorageFolder folderinSourceDir in await SourceFolder.GetFoldersAsync())
            {
                if (ItemsToPaste.Count > 3 && !suppressProgressFlyout)
                {
                    App.OccupiedInstance.UpdateProgressFlyout(InteractionOperationType.PasteItems, ++itemsPasted, ItemsToPaste.Count + (await SourceFolder.GetItemsAsync()).Count);
                }
                CloneDirectoryAsync(folderinSourceDir.Path, DestinationFolder.Path, folderinSourceDir.Name, false);
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
            StorageFile selectedItem = null;
            if (currentInstance.ItemDisplayFrame.CurrentSourcePageType == typeof(GenericFileBrowser))
            {
                var page = (currentInstance.ItemDisplayFrame.Content as GenericFileBrowser);
                selectedItem = await StorageFile.GetFileFromPathAsync(currentInstance.instanceViewModel.FilesAndFolders[page.AllView.SelectedIndex].FilePath);

            }
            else if (currentInstance.ItemDisplayFrame.CurrentSourcePageType == typeof(PhotoAlbum))
            {
                var page = (currentInstance.ItemDisplayFrame.Content as PhotoAlbum);
                selectedItem = await StorageFile.GetFileFromPathAsync(currentInstance.instanceViewModel.FilesAndFolders[page.FileList.SelectedIndex].FilePath);
            }

            ExtractFilesDialog extractFilesDialog = new ExtractFilesDialog(currentInstance.instanceViewModel.Universal.path);
            await extractFilesDialog.ShowAsync();
            if (((bool)ApplicationData.Current.LocalSettings.Values["Extract_Destination_Cancelled"]) == false)
            {
                var bufferItem = await selectedItem.CopyAsync(ApplicationData.Current.TemporaryFolder, selectedItem.DisplayName, NameCollisionOption.ReplaceExisting);
                string destinationPath = ApplicationData.Current.LocalSettings.Values["Extract_Destination_Path"].ToString();
                //ZipFile.ExtractToDirectory(selectedItem.Path, destinationPath, );
                var destFolder_InBuffer = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(selectedItem.DisplayName + "_Extracted", CreationCollisionOption.ReplaceExisting);
                using (FileStream fs = new FileStream(bufferItem.Path, FileMode.Open))
                {
                    ZipArchive zipArchive = new ZipArchive(fs);
                    int totalCount = zipArchive.Entries.Count;
                    int index = 0;

                    (App.OccupiedInstance.ItemDisplayFrame.Content as BaseLayout).AssociatedViewModel.LoadIndicator.isVisible = Visibility.Visible;

                    foreach (ZipArchiveEntry archiveEntry in zipArchive.Entries)
                    {
                        archiveEntry.ExtractToFile(destFolder_InBuffer.Path + "\\" + archiveEntry.Name);
                        index++;
                        if (index == totalCount)
                        {
                            (App.OccupiedInstance.ItemDisplayFrame.Content as BaseLayout).AssociatedViewModel.LoadIndicator.isVisible = Visibility.Collapsed;
                        }
                    }
                    CloneDirectoryAsync(destFolder_InBuffer.Path, destinationPath, destFolder_InBuffer.Name, true);
                    await destFolder_InBuffer.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    Frame rootFrame = Window.Current.Content as Frame;
                    var instanceTabsView = rootFrame.Content as InstanceTabsView;
                    instanceTabsView.AddNewTab(typeof(ProHome), destinationPath + "\\" + selectedItem.DisplayName);
                }
            }
            else if (((bool)ApplicationData.Current.LocalSettings.Values["Extract_Destination_Cancelled"]) == true)
            {
                return;
            }
        }

        public void SelectAllItems()
        {
            if(App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                var CurrentInstance = App.OccupiedInstance;
                foreach (ListedItem li in (CurrentInstance.ItemDisplayFrame.Content as GenericFileBrowser).AllView.ItemsSource)
                {
                    if (!(currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems.Contains(li))
                    {
                        (currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems.Add(li);
                    }
                }
            }
            else if(App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(PhotoAlbum))
            {
                (currentInstance.ItemDisplayFrame.Content as PhotoAlbum).FileList.SelectAll();
            }
        }

        public void ClearAllItems()
        {
            if (App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                var CurrentInstance = App.OccupiedInstance;
                (currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems.Clear();
            }
            else if (App.OccupiedInstance.ItemDisplayFrame.SourcePageType == typeof(PhotoAlbum))
            {
                (currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems.Clear();
            }
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
                Type sourcePageType = App.OccupiedInstance.ItemDisplayFrame.SourcePageType;
                selectedItemCount = (currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems.Count;
                if (selectedItemCount == 1)
                {
                    selectedItemPath = (currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems[0].FilePath;
                }

                if (selectedItemCount == 1)
                {
                    var clickedOnItem = (currentInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems[0];

                    Debug.WriteLine("Toggle QuickLook");
                    ApplicationData.Current.LocalSettings.Values["path"] = clickedOnItem.FilePath;
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
            App.OccupiedInstance.instanceViewModel.JumpString += letter.ToString().ToLower();
        }
    }
}
