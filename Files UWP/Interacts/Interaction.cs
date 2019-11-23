using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Files.Filesystem;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using System.Collections;
using Windows.Foundation;
using System.IO;
using System.Reflection;
using Files.Dialogs;
using System.IO.Compression;
using System.Linq;

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
            var CurrentSourceType = App.OccupiedInstance.accessibleContentFrame.CurrentSourcePageType;
            if (CurrentSourceType == typeof(GenericFileBrowser))
            {
                var items = (currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems;
                foreach (ListedItem listedItem in items)
                {
                    var selectedItemPath = listedItem.FilePath;
                    var folderUri = new Uri("files-uwp:" + "?folder=" + @selectedItemPath);
                    await Launcher.LaunchUriAsync(folderUri);
                }

            }
            else if (CurrentSourceType == typeof(PhotoAlbum))
            {
                var items = (currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems;
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
            var CurrentSourceType = App.OccupiedInstance.accessibleContentFrame.CurrentSourcePageType;
            if(CurrentSourceType == typeof(GenericFileBrowser))
            {
                var items = (currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems;
                foreach (ListedItem listedItem in items)
                {
                    instanceTabsView.AddNewTab(typeof(ProHome), listedItem.FilePath);
                }
                
            }
            else if(CurrentSourceType == typeof(PhotoAlbum))
            {
                var items = (currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems;
                foreach (ListedItem listedItem in items)
                {
                    instanceTabsView.AddNewTab(typeof(ProHome), listedItem.FilePath);
                }
            }
        }

        public async void OpenDirectoryInTerminal(object sender, RoutedEventArgs e)
        {

            ApplicationData.Current.LocalSettings.Values["Application"] = "cmd.exe";
            if(App.OccupiedInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                ApplicationData.Current.LocalSettings.Values["Arguments"] = "/k \"cd /d "+ currentInstance.instanceViewModel.Universal.path + "&& title Command Prompt" + "\""; 
            }
            else if(App.OccupiedInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                ApplicationData.Current.LocalSettings.Values["Arguments"] = "/k \"cd /d " + currentInstance.instanceViewModel.Universal.path + "&& title Command Prompt" + "\"";
            }

            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }

        public async void PinItem_Click(object sender, RoutedEventArgs e)
        {
            if (App.OccupiedInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                StorageFolder cacheFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;
                List<string> items = new List<string>();

                try
                {
                    foreach (ListedItem listedItem in (currentInstance.accessibleContentFrame.Content as GenericFileBrowser).AllView.SelectedItems)
                    {
                        items.Add(listedItem.FilePath);
                    }
                    var ListFile = await cacheFolder.GetFileAsync("PinnedItems.txt");
                    await FileIO.AppendLinesAsync(ListFile, items);
                }
                catch (FileNotFoundException)
                {
                    foreach (ListedItem listedItem in (currentInstance.accessibleContentFrame.Content as GenericFileBrowser).AllView.SelectedItems)
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
            else if(App.OccupiedInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                StorageFolder cacheFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;
                List<string> items = new List<string>();

                try
                {
                    foreach (ListedItem listedItem in (currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems)
                    {
                        items.Add(listedItem.FilePath);
                    }
                    var ListFile = await cacheFolder.GetFileAsync("PinnedItems.txt");
                    await FileIO.AppendLinesAsync(ListFile, items);
                }
                catch (FileNotFoundException)
                {
                    foreach (ListedItem listedItem in (currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems)
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
            if(App.OccupiedInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                data.SetText(currentInstance.instanceViewModel.Universal.path);
                Clipboard.SetContent(data);
                Clipboard.Flush();
            }
            else if(App.OccupiedInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
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
                if ((currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems.Contains(ObjectPressed))
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
                Type sourcePageType = App.OccupiedInstance.accessibleContentFrame.SourcePageType;
                selectedItemCount = (currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems.Count;
                if (selectedItemCount == 1)
                {
                    selectedItemPath = (currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems[0].FilePath;
                }

                // Access MRU List
                var mostRecentlyUsed = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;

                if (selectedItemCount == 1)
                {
                    var clickedOnItem = (currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems[0];
                    if (clickedOnItem.FileType == "Folder")
                    {
                        // Add location to MRU List
                        mostRecentlyUsed.Add(await StorageFolder.GetFolderFromPathAsync(selectedItemPath));

                        currentInstance.instanceViewModel.Universal.path = selectedItemPath;
                        currentInstance.PathText.Text = selectedItemPath;

                        (currentInstance.accessibleContentFrame.Content as BaseLayout).EmptyTextState.isVisible = Visibility.Collapsed;
                        currentInstance.FS.isEnabled = false;
                        if (selectedItemPath == Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory))
                        {
                            currentInstance.PathText.Text = "Desktop";
                            currentInstance.locationsList.SelectedIndex = 1;
                            currentInstance.accessibleContentFrame.Navigate(sourcePageType, App.DesktopPath, new SuppressNavigationTransitionInfo());

                        }
                        else if (selectedItemPath == Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
                        {
                            currentInstance.PathText.Text = "Documents";
                            currentInstance.locationsList.SelectedIndex = 3;
                            currentInstance.accessibleContentFrame.Navigate(sourcePageType, App.DocumentsPath, new SuppressNavigationTransitionInfo());
                        }
                        else if (selectedItemPath == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
                        {
                            currentInstance.PathText.Text = "Downloads";
                            currentInstance.locationsList.SelectedIndex = 2;
                            currentInstance.accessibleContentFrame.Navigate(sourcePageType, App.DownloadsPath, new SuppressNavigationTransitionInfo());
                        }
                        else if (selectedItemPath == Environment.GetFolderPath(Environment.SpecialFolder.MyPictures))
                        {
                            currentInstance.PathText.Text = "Pictures";
                            currentInstance.locationsList.SelectedIndex = 4;
                            currentInstance.accessibleContentFrame.Navigate(sourcePageType, App.PicturesPath, new SuppressNavigationTransitionInfo());
                        }
                        else if (selectedItemPath == Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))
                        {
                            currentInstance.PathText.Text = "Music";
                            currentInstance.locationsList.SelectedIndex = 5;
                            currentInstance.accessibleContentFrame.Navigate(sourcePageType, App.MusicPath, new SuppressNavigationTransitionInfo());
                        }
                        else if (selectedItemPath == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
                        {
                            currentInstance.PathText.Text = "OneDrive";
                            currentInstance.drivesList.SelectedItem = currentInstance.drivesList.Items.Where(x => (x as DriveItem).tag == "OneDrive").First();
                            currentInstance.accessibleContentFrame.Navigate(sourcePageType, App.OneDrivePath, new SuppressNavigationTransitionInfo());
                        }
                        else if (selectedItemPath == Environment.GetFolderPath(Environment.SpecialFolder.MyVideos))
                        {
                            currentInstance.PathText.Text = "Videos";
                            currentInstance.locationsList.SelectedIndex = 6;
                            currentInstance.accessibleContentFrame.Navigate(sourcePageType, App.VideosPath, new SuppressNavigationTransitionInfo());
                        }
                        else
                        {
                            if (selectedItemPath.Split(@"\")[0].Contains("C:"))
                            {
                                currentInstance.drivesList.SelectedItem = currentInstance.drivesList.Items.Where(x => (x as DriveItem).tag == "C:\\").First();
                            }
                            else
                            {
                                currentInstance.drivesList.SelectedItem = currentInstance.drivesList.Items.Where(x => (x as DriveItem).tag.Contains(selectedItemPath.Split(@"\")[0])).First();
                            }
                            currentInstance.accessibleContentFrame.Navigate(sourcePageType, selectedItemPath, new SuppressNavigationTransitionInfo());
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
                    foreach (ListedItem clickedOnItem in (currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems)
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
            App.OccupiedInstance.propertiesDialog.accessiblePropertiesFrame.Tag = App.OccupiedInstance.propertiesDialog;
            App.OccupiedInstance.propertiesDialog.accessiblePropertiesFrame.Navigate(typeof(Properties), (App.OccupiedInstance.accessibleContentFrame.Content as BaseLayout).selectedItems, new SuppressNavigationTransitionInfo());
            await App.OccupiedInstance.propertiesDialog.ShowAsync(ContentDialogPlacement.Popup);
        }

        private async void Manager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequestDeferral dataRequestDeferral = args.Request.GetDeferral();
            List<IStorageItem> items = new List<IStorageItem>();
            if(App.OccupiedInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                var CurrentInstance = App.OccupiedInstance;

                foreach (ListedItem li in (currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems)
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
            else if (App.OccupiedInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                foreach (ListedItem li in (currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems)
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
                if (App.OccupiedInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
                {
                    var CurrentInstance = App.OccupiedInstance;
                    List<ListedItem> selectedItems = new List<ListedItem>();
                    foreach (ListedItem selectedItem in (currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems)
                    {
                        selectedItems.Add(selectedItem);
                    }

                    CurrentInstance.deleteProgressBoxIndicator.Maximum = selectedItems.Count;
                    CurrentInstance.deleteProgressBoxIndicator.Value = 0;
                    CurrentInstance.deleteProgressBoxTitle.Text = "Moving " + selectedItems.Count + " items to the Recycle Bin";
                    if(selectedItems.Count > 5)
                    {
                        CurrentInstance.deleteProgressBox.Visibility = Visibility.Visible;
                    }
                    CurrentInstance.deleteProgressBoxTextInfo.Text = "Removing item (0/" + selectedItems.Count + ")";
                    foreach (ListedItem storItem in selectedItems)
                    {
                        CurrentInstance.deleteProgressBoxTextInfo.Text = "Removing item (" + (CurrentInstance.deleteProgressBoxIndicator.Value + 1) + "/" + selectedItems.Count + ")";
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
                        CurrentInstance.deleteProgressBoxIndicator.Value++;
                    }
                    CurrentInstance.deleteProgressBox.Visibility = Visibility.Collapsed;
                    CurrentInstance.FS.isEnabled = false;
                }
                else if (App.OccupiedInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
                {
                    var CurrentInstance = App.OccupiedInstance;
                    List<ListedItem> selectedItems = new List<ListedItem>();
                    foreach (ListedItem selectedItem in (currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems)
                    {
                        selectedItems.Add(selectedItem);
                    }

                    CurrentInstance.deleteProgressBoxIndicator.Maximum = selectedItems.Count;
                    CurrentInstance.deleteProgressBoxIndicator.Value = 0;
                    CurrentInstance.deleteProgressBoxTitle.Text = "Moving " + selectedItems.Count + " items to the Recycle Bin";

                    if (selectedItems.Count > 5)
                    {
                        CurrentInstance.deleteProgressBox.Visibility = Visibility.Visible;
                    }
                    CurrentInstance.deleteProgressBoxTextInfo.Text = "Removing item (0/" + selectedItems.Count + ")";

                    foreach (ListedItem storItem in selectedItems)
                    {
                        CurrentInstance.deleteProgressBoxTextInfo.Text = "Removing item (" + (CurrentInstance.deleteProgressBoxIndicator.Value + 1) + "/" + selectedItems.Count + ")";
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
                        CurrentInstance.deleteProgressBoxIndicator.Value++;
                    }
                    CurrentInstance.deleteProgressBox.Visibility = Visibility.Collapsed;
                    CurrentInstance.FS.isEnabled = false;
                }
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
            if (App.OccupiedInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                var fileBrowser = App.OccupiedInstance.accessibleContentFrame.Content as GenericFileBrowser;
                if (fileBrowser.AllView.SelectedItem != null)
                    fileBrowser.AllView.CurrentColumn = fileBrowser.AllView.Columns[1];
                fileBrowser.AllView.BeginEdit();
            }
            else if (App.OccupiedInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                var photoAlbum = App.OccupiedInstance.accessibleContentFrame.Content as PhotoAlbum;
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
            currentInstance.FS.isEnabled = false;
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
            if (App.OccupiedInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                var CurrentInstance = App.OccupiedInstance;
                if ((currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems.Count != 0)
                {
                    dataGridRows.Clear();
                    FindChildren<DataGridRow>(dataGridRows, (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).AllView);
                    
                    // First, reset DataGrid Rows that may be in "cut" command mode
                    foreach (DataGridRow row in dataGridRows)
                    {
                        if ((CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).AllView.Columns[0].GetCellContent(row).Opacity < 1)
                        {
                            (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).AllView.Columns[0].GetCellContent(row).Opacity = 1;
                        }
                    }

                    foreach (ListedItem StorItem in (currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems)
                    {
                        IEnumerator allItems = (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).AllView.ItemsSource.GetEnumerator();
                        int index = -1;
                        while (allItems.MoveNext())
                        {
                            index++;
                            var item = allItems.Current;
                            if(item == StorItem)
                            {
                                DataGridRow dataGridRow = dataGridRows[index];
                                (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).AllView.Columns[0].GetCellContent(dataGridRow).Opacity = 0.4;
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
            else if (App.OccupiedInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                var CurrentInstance = App.OccupiedInstance;
                if ((currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems.Count != 0)
                {

                    gridViewItems.Clear();
                    FindChildren<GridViewItem>(gridViewItems, (CurrentInstance.accessibleContentFrame.Content as PhotoAlbum).FileList);

                    // First, reset GridView items that may be in "cut" command mode
                    foreach (GridViewItem gridViewItem in gridViewItems)
                    {
                        List<Grid> itemContentGrids = new List<Grid>();
                        FindChildren<Grid>(itemContentGrids, (CurrentInstance.accessibleContentFrame.Content as PhotoAlbum).FileList.ContainerFromItem(gridViewItem.Content));
                        var imageOfItem = itemContentGrids.Find(x => x.Tag?.ToString() == "ItemImage");
                        if (imageOfItem.Opacity < 1)
                        {
                            imageOfItem.Opacity = 1;
                        }
                    }

                    foreach (ListedItem StorItem in (currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems)
                    {
                        GridViewItem itemToDimForCut = (GridViewItem) (currentInstance.accessibleContentFrame.Content as PhotoAlbum).FileList.ContainerFromItem(StorItem);
                        List<Grid> itemContentGrids = new List<Grid>();
                        FindChildren<Grid>(itemContentGrids, (CurrentInstance.accessibleContentFrame.Content as PhotoAlbum).FileList.ContainerFromItem(itemToDimForCut.Content));
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
        public async void CopyItem_ClickAsync(object sender, RoutedEventArgs e)
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            List<IStorageItem> items = new List<IStorageItem>();
            if (App.OccupiedInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                var CurrentInstance = App.OccupiedInstance;
                CopySourcePath = currentInstance.instanceViewModel.Universal.path;

                if ((currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems.Count != 0)
                {
                    foreach (ListedItem StorItem in (currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems)
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
            else if (App.OccupiedInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                CopySourcePath = currentInstance.instanceViewModel.Universal.path;

                if ((currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems.Count != 0)
                {
                    foreach (ListedItem StorItem in (currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems)
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
            string DestinationPath = null;
            int oldCount;
            if (App.OccupiedInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                DestinationPath = currentInstance.instanceViewModel.Universal.path;
                oldCount = currentInstance.instanceViewModel.FilesAndFolders.Count;
            }
            else if(App.OccupiedInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                DestinationPath = currentInstance.instanceViewModel.Universal.path;
                oldCount = currentInstance.instanceViewModel.FilesAndFolders.Count;
            }
            DataPackageView packageView = Clipboard.GetContent();
            var ItemsToPaste = await packageView.GetStorageItemsAsync();
            foreach (IStorageItem item in ItemsToPaste)
            {
                if (item.IsOfType(StorageItemTypes.Folder))
                {
                    CloneDirectoryAsync(item.Path, DestinationPath, item.Name);
                }
                else if (item.IsOfType(StorageItemTypes.File))
                {
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

        public async void CloneDirectoryAsync(string SourcePath, string DestinationPath, string sourceRootName)
        {
            StorageFolder SourceFolder = await StorageFolder.GetFolderFromPathAsync(SourcePath);
            StorageFolder DestinationFolder = await StorageFolder.GetFolderFromPathAsync(DestinationPath);
            var createdRoot = await DestinationFolder.CreateFolderAsync(sourceRootName, CreationCollisionOption.GenerateUniqueName);
            DestinationFolder = await StorageFolder.GetFolderFromPathAsync(createdRoot.Path);

            foreach (StorageFile fileInSourceDir in await SourceFolder.GetFilesAsync())
            {
                await fileInSourceDir.CopyAsync(DestinationFolder, fileInSourceDir.Name, NameCollisionOption.GenerateUniqueName);
            }
            foreach (StorageFolder folderinSourceDir in await SourceFolder.GetFoldersAsync())
            {
                CloneDirectoryAsync(folderinSourceDir.Path, DestinationFolder.Path, folderinSourceDir.Name);
            }

        }

        public void NewFolder_Click(object sender, RoutedEventArgs e)
        {
            AddItem.CreateFile(currentInstance, "Folder");
        }

        public void NewTextDocument_Click(object sender, RoutedEventArgs e)
        {
            AddItem.CreateFile(currentInstance, "Text Document");
        }

        public void NewBitmapImage_Click(object sender, RoutedEventArgs e)
        {
            AddItem.CreateFile(currentInstance, "Bitmap Image");
        }

        public async void ExtractItems_Click(object sender, RoutedEventArgs e)
        {
            StorageFile selectedItem = null;
            if (currentInstance.accessibleContentFrame.CurrentSourcePageType == typeof(GenericFileBrowser))
            {
                var page = (currentInstance.accessibleContentFrame.Content as GenericFileBrowser);
                selectedItem = await StorageFile.GetFileFromPathAsync(currentInstance.instanceViewModel.FilesAndFolders[page.AllView.SelectedIndex].FilePath);

            }
            else if (currentInstance.accessibleContentFrame.CurrentSourcePageType == typeof(PhotoAlbum))
            {
                var page = (currentInstance.accessibleContentFrame.Content as PhotoAlbum);
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

                    (App.OccupiedInstance.accessibleContentFrame.Content as BaseLayout).LoadIndicator.isVisible = Visibility.Visible;

                    foreach (ZipArchiveEntry archiveEntry in zipArchive.Entries)
                    {
                        archiveEntry.ExtractToFile(destFolder_InBuffer.Path + "\\" + archiveEntry.Name);
                        index++;
                        if (index == totalCount)
                        {
                            (App.OccupiedInstance.accessibleContentFrame.Content as BaseLayout).LoadIndicator.isVisible = Visibility.Collapsed;
                        }
                    }
                    CloneDirectoryAsync(destFolder_InBuffer.Path, destinationPath, destFolder_InBuffer.Name);
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
            if(App.OccupiedInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                var CurrentInstance = App.OccupiedInstance;
                foreach (ListedItem li in (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).AllView.ItemsSource)
                {
                    if (!(currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems.Contains(li))
                    {
                        (currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems.Add(li);
                    }
                }
            }
            else if(App.OccupiedInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                (currentInstance.accessibleContentFrame.Content as PhotoAlbum).FileList.SelectAll();
            }
        }

        public void ClearAllItems()
        {
            if (App.OccupiedInstance.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                var CurrentInstance = App.OccupiedInstance;
                (currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems.Clear();
            }
            else if (App.OccupiedInstance.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                (currentInstance.accessibleContentFrame.Content as BaseLayout).selectedItems.Clear();
            }
        }
    }
}