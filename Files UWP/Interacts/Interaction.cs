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
using System.ComponentModel;
using Files.Filesystem;
using Files.Navigation;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using System.Collections;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;
using System.IO;
using System.Reflection;

namespace Files.Interacts
{
    public class Interaction<PageType> where PageType : class
    {
        public PageType type;
        public Interaction(PageType contentPageInstance)
        {
            type = contentPageInstance;
        }

        public async void List_ItemClick(object sender, DoubleTappedRoutedEventArgs e)
        {
            try
            {
                if (typeof(PageType) == typeof(GenericFileBrowser))
                {
                    var CurrentInstance = ItemViewModel<GenericFileBrowser>.GetCurrentSelectedTabInstance<ProHome>();
                    var index = (type as GenericFileBrowser).data.SelectedIndex;
                    if (index > -1)
                    {
                        var clickedOnItem = (type as GenericFileBrowser).instanceViewModel.FilesAndFolders[index];
                        // Access MRU List
                        var mostRecentlyUsed = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;

                        if (clickedOnItem.FileType == "Folder")
                        {
                            // Add location to MRU List
                            mostRecentlyUsed.Add(await StorageFolder.GetFolderFromPathAsync(clickedOnItem.FilePath));

                            var TabInstance = CurrentInstance;
                            (type as GenericFileBrowser).instanceViewModel.Universal.path = clickedOnItem.FilePath;
                            TabInstance.PathText.Text = clickedOnItem.FilePath;
                            TabInstance.TextState.isVisible = Visibility.Collapsed;
                            TabInstance.FS.isEnabled = false;
                            (type as GenericFileBrowser).instanceViewModel.CancelLoadAndClearFiles();
                            if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory))
                            {
                                TabInstance.PathText.Text = "Desktop";
                                TabInstance.locationsList.SelectedIndex = 1;
                                TabInstance.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.DesktopPath, new SuppressNavigationTransitionInfo());

                            }
                            else if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
                            {
                                TabInstance.PathText.Text = "Documents";
                                TabInstance.locationsList.SelectedIndex = 3;
                                TabInstance.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.DocumentsPath, new SuppressNavigationTransitionInfo());
                            }
                            else if (clickedOnItem.FilePath == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
                            {
                                TabInstance.PathText.Text = "Downloads";
                                TabInstance.locationsList.SelectedIndex = 2;
                                TabInstance.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.DownloadsPath, new SuppressNavigationTransitionInfo());
                            }
                            else if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.MyPictures))
                            {
                                TabInstance.locationsList.SelectedIndex = 4;
                                TabInstance.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.PicturesPath, new SuppressNavigationTransitionInfo());
                                TabInstance.PathText.Text = "Pictures";
                            }
                            else if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))
                            {
                                TabInstance.PathText.Text = "Music";
                                TabInstance.locationsList.SelectedIndex = 5;
                                TabInstance.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.MusicPath, new SuppressNavigationTransitionInfo());
                            }
                            else if (clickedOnItem.FilePath == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
                            {
                                TabInstance.PathText.Text = "OneDrive";
                                TabInstance.drivesList.SelectedIndex = 1;
                                TabInstance.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.OneDrivePath, new SuppressNavigationTransitionInfo());
                            }
                            else if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.MyVideos))
                            {
                                TabInstance.PathText.Text = "Videos";
                                TabInstance.locationsList.SelectedIndex = 6;
                                TabInstance.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.VideosPath, new SuppressNavigationTransitionInfo());
                            }
                            else
                            {
                                if (clickedOnItem.FilePath.Split(@"\")[0].Contains("C:"))
                                {
                                    TabInstance.drivesList.SelectedIndex = 0;
                                }
                                (type as GenericFileBrowser).instanceViewModel.Universal.path = clickedOnItem.FilePath;
                                TabInstance.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), (type as GenericFileBrowser).instanceViewModel.Universal.path, new SuppressNavigationTransitionInfo());
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
                                DisplayApplicationPicker = false

                            };
                            await Launcher.LaunchFileAsync(file, options);
                        }
                    }
                }
                else if (typeof(PageType) == typeof(PhotoAlbum))
                {
                    var index = (type as PhotoAlbum).gv.SelectedIndex;
                    var CurrentInstance = ItemViewModel<PhotoAlbum>.GetCurrentSelectedTabInstance<ProHome>();
                    if (index > -1)
                    {
                        var clickedOnItem = (type as PhotoAlbum).instanceViewModel.FilesAndFolders[index];
                        // Access MRU List
                        var mostRecentlyUsed = Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList;

                        if (clickedOnItem.FileType == "Folder")
                        {
                            // Add location to MRU List
                            mostRecentlyUsed.Add(await StorageFolder.GetFolderFromPathAsync(clickedOnItem.FilePath));

                            var TabInstance = CurrentInstance;
                            (type as PhotoAlbum).instanceViewModel.Universal.path = clickedOnItem.FilePath;
                            TabInstance.PathText.Text = clickedOnItem.FilePath;
                            TabInstance.TextState.isVisible = Visibility.Collapsed;
                            TabInstance.FS.isEnabled = false;
                            (type as PhotoAlbum).instanceViewModel.CancelLoadAndClearFiles();
                            if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory))
                            {
                                TabInstance.PathText.Text = "Desktop";
                                TabInstance.locationsList.SelectedIndex = 1;
                                TabInstance.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.DesktopPath, new SuppressNavigationTransitionInfo());
                            }
                            else if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
                            {
                                TabInstance.PathText.Text = "Documents";
                                TabInstance.locationsList.SelectedIndex = 3;
                                TabInstance.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.DocumentsPath, new SuppressNavigationTransitionInfo());
                            }
                            else if (clickedOnItem.FilePath == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
                            {
                                TabInstance.PathText.Text = "Downloads";
                                TabInstance.locationsList.SelectedIndex = 2;
                                TabInstance.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.DownloadsPath, new SuppressNavigationTransitionInfo());
                            }
                            else if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.MyPictures))
                            {
                                TabInstance.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.PicturesPath, new SuppressNavigationTransitionInfo());
                                TabInstance.locationsList.SelectedIndex = 4;
                                TabInstance.PathText.Text = "Pictures";
                            }
                            else if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))
                            {
                                TabInstance.PathText.Text = "Music";
                                TabInstance.locationsList.SelectedIndex = 5;
                                TabInstance.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.MusicPath, new SuppressNavigationTransitionInfo());
                            }
                            else if (clickedOnItem.FilePath == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
                            {
                                TabInstance.PathText.Text = "OneDrive";
                                TabInstance.drivesList.SelectedIndex = 1;
                                TabInstance.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.OneDrivePath, new SuppressNavigationTransitionInfo());
                            }
                            else if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.MyVideos))
                            {
                                TabInstance.PathText.Text = "Videos";
                                TabInstance.drivesList.SelectedIndex = 6;
                                TabInstance.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.VideosPath, new SuppressNavigationTransitionInfo());
                            }
                            else
                            {
                                TabInstance.drivesList.SelectedIndex = 0;
                                TabInstance.PathText.Text = clickedOnItem.FilePath;
                                TabInstance.accessibleContentFrame.Navigate(typeof(PhotoAlbum), clickedOnItem.FilePath, new SuppressNavigationTransitionInfo());
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
                                DisplayApplicationPicker = false

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

        public void GetPath_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.Clear();
            DataPackage data = new DataPackage();
            if(typeof(PageType) == typeof(GenericFileBrowser))
            {
                data.SetText((type as GenericFileBrowser).instanceViewModel.Universal.path);
                Clipboard.SetContent(data);
                Clipboard.Flush();
            }
            else if(typeof(PageType) == typeof(PhotoAlbum))
            {
                data.SetText((type as PhotoAlbum).instanceViewModel.Universal.path);
                Clipboard.SetContent(data);
                Clipboard.Flush();
            }
            // Eventually notify user via flyout            

        }

        public async Task LaunchExe(string ApplicationPath)
        {
            Debug.WriteLine("Launching EXE in FullTrustProcess");
            ApplicationData.Current.LocalSettings.Values["Application"] = ApplicationPath;
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }

        public async void CommandInvokedHandler(IUICommand command)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-windows-store://home"));
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
                var CurrentInstance = ItemViewModel<GenericFileBrowser>.GetCurrentSelectedTabInstance<ProHome>();
                foreach (ListedItem listedItem in (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.SelectedItems)
                {
                    if (RowPressed.GetIndex() == listedItem.RowIndex)
                    {
                        return;
                    }
                }
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
        
        public async void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            if (typeof(PageType) == typeof(GenericFileBrowser))
            {
                var CurrentInstance = ItemViewModel<GenericFileBrowser>.GetCurrentSelectedTabInstance<ProHome>();
                var ItemSelected = (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.SelectedIndex;
                var RowData = (type as GenericFileBrowser).instanceViewModel.FilesAndFolders[ItemSelected];

                if (RowData.FileType == "Folder")
                {
                    CurrentInstance.TextState.isVisible = Visibility.Collapsed;
                    CurrentInstance.FS.isEnabled = false;
                    (type as GenericFileBrowser).instanceViewModel.CancelLoadAndClearFiles();
                    (type as GenericFileBrowser).instanceViewModel.Universal.path = RowData.FilePath;
                    (type as GenericFileBrowser).instanceViewModel.AddItemsToCollectionAsync((type as GenericFileBrowser).instanceViewModel.Universal.path, (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).GFBPageName);
                }
                else
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(RowData.FilePath);
                    var options = new LauncherOptions();
                    options.DisplayApplicationPicker = true;
                    await Launcher.LaunchFileAsync(file, options);
                }
            }
            else if (typeof(PageType) == typeof(PhotoAlbum))
            {
                var CurrentInstance = ItemViewModel<PhotoAlbum>.GetCurrentSelectedTabInstance<ProHome>();
                var ItemSelected = (type as PhotoAlbum).gv.SelectedIndex;
                var RowData = (type as PhotoAlbum).instanceViewModel.FilesAndFolders[ItemSelected];

                if (RowData.FileType == "Folder")
                {
                    CurrentInstance.TextState.isVisible = Visibility.Collapsed;
                    CurrentInstance.FS.isEnabled = false;
                    (type as PhotoAlbum).instanceViewModel.CancelLoadAndClearFiles();
                    (type as PhotoAlbum).instanceViewModel.Universal.path = RowData.FilePath;
                    (type as PhotoAlbum).instanceViewModel.AddItemsToCollectionAsync(RowData.FilePath, (type as PhotoAlbum).PAPageName);
                }
                else
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(RowData.FilePath);
                    var options = new LauncherOptions();
                    options.DisplayApplicationPicker = true;
                    await Launcher.LaunchFileAsync(file, options);
                }
            }

        }

        public void ShareItem_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager manager = DataTransferManager.GetForCurrentView();
            manager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(Manager_DataRequested);
            DataTransferManager.ShowShareUI();
        }

        private async void Manager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequestDeferral dataRequestDeferral = args.Request.GetDeferral();
            List<IStorageItem> items = new List<IStorageItem>();
            if(typeof(PageType) == typeof(GenericFileBrowser))
            {
                var CurrentInstance = ItemViewModel<GenericFileBrowser>.GetCurrentSelectedTabInstance<ProHome>();

                foreach (ListedItem li in (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.SelectedItems)
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
            else if (typeof(PageType) == typeof(PhotoAlbum))
            {
                foreach (ListedItem li in (type as PhotoAlbum).gv.SelectedItems)
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
                if (typeof(PageType) == typeof(GenericFileBrowser))
                {
                    var CurrentInstance = ItemViewModel<GenericFileBrowser>.GetCurrentSelectedTabInstance<ProHome>();
                    List<ListedItem> selectedItems = new List<ListedItem>();
                    foreach(ListedItem selectedItem in (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.SelectedItems)
                    {
                        selectedItems.Add(selectedItem);
                    }
                    foreach (ListedItem storItem in selectedItems)
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
                        (type as GenericFileBrowser).instanceViewModel.RemoveFileOrFolder(storItem);
                    }
                    Debug.WriteLine("Ended for loop");
                    CurrentInstance.FS.isEnabled = false;
                }
                else if (typeof(PageType) == typeof(PhotoAlbum))
                {
                    var CurrentInstance = ItemViewModel<PhotoAlbum>.GetCurrentSelectedTabInstance<ProHome>();
                    List<ListedItem> selectedItems = new List<ListedItem>();
                    foreach (ListedItem selectedItem in (type as PhotoAlbum).gv.SelectedItems)
                    {
                        selectedItems.Add(selectedItem);
                    }
                    foreach (ListedItem storItem in selectedItems)
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
                        (type as PhotoAlbum).instanceViewModel.RemoveFileOrFolder(storItem);
                    }
                    Debug.WriteLine("Ended for loop");
                    CurrentInstance.FS.isEnabled = false;
                }
                
            }
            catch (UnauthorizedAccessException)
            {
                MessageDialog AccessDeniedDialog = new MessageDialog("Access Denied", "Unable to delete this item");
                await AccessDeniedDialog.ShowAsync();
            }
        }

        public async void RenameItem_Click(object sender, RoutedEventArgs e)
        {

            if (typeof(PageType) == typeof(GenericFileBrowser))
            {
                var CurrentInstance = ItemViewModel<GenericFileBrowser>.GetCurrentSelectedTabInstance<ProHome>();

                CurrentInstance.inputFromRename.Text = "";
                try
                {
                    var ItemSelected = (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.SelectedIndex;
                    var RowData = (type as GenericFileBrowser).instanceViewModel.FilesAndFolders[ItemSelected];
                    await CurrentInstance.NameBox.ShowAsync();
                    var input = CurrentInstance.inputForRename;
                    if (input != null)
                    {
                        if (RowData.FileType == "Folder")
                        {
                            var item = await StorageFolder.GetFolderFromPathAsync(RowData.FilePath);
                            await item.RenameAsync(input, NameCollisionOption.FailIfExists);
                            (type as GenericFileBrowser).instanceViewModel.RemoveFileOrFolder(RowData);
                            (type as GenericFileBrowser).instanceViewModel.AddFileOrFolder(new ListedItem(item.FolderRelativeId)
                            {
                                FileName = input,
                                FileDateReal = DateTimeOffset.Now,
                                EmptyImgVis = Visibility.Collapsed,
                                FolderImg = Visibility.Visible,
                                FileIconVis = Visibility.Collapsed,
                                FileType = "Folder",
                                FileImg = null,
                                FilePath = Path.Combine((type as GenericFileBrowser).instanceViewModel.Universal.path, input)
                            });
                        }
                        else
                        {
                            var item = await StorageFile.GetFileFromPathAsync(RowData.FilePath);
                            await item.RenameAsync(input + RowData.DotFileExtension, NameCollisionOption.FailIfExists);
                            (type as GenericFileBrowser).instanceViewModel.RemoveFileOrFolder(RowData);
                            (type as GenericFileBrowser).instanceViewModel.AddFileOrFolder(new ListedItem(item.FolderRelativeId)
                            {
                                FileName = input,
                                FileDateReal = DateTimeOffset.Now,
                                EmptyImgVis = Visibility.Visible,
                                FolderImg = Visibility.Collapsed,
                                FileIconVis = Visibility.Collapsed,
                                FileType = RowData.FileType,
                                FileImg = null,
                                FilePath = Path.Combine((type as GenericFileBrowser).instanceViewModel.Universal.path, input + RowData.DotFileExtension),
                                DotFileExtension = RowData.DotFileExtension
                            });
                        }
                    }

                }
                catch (Exception)
                {
                    MessageDialog itemAlreadyExistsDialog = new MessageDialog("An item with this name already exists in this folder", "Try again");
                    await itemAlreadyExistsDialog.ShowAsync();
                }
                CurrentInstance.FS.isEnabled = false;
            }
            else if (typeof(PageType) == typeof(PhotoAlbum))
            {
                var CurrentInstance = ItemViewModel<PhotoAlbum>.GetCurrentSelectedTabInstance<ProHome>();
                try
                {
                    var ItemSelected = (type as PhotoAlbum).gv.SelectedIndex;
                    var BoxData = (type as PhotoAlbum).instanceViewModel.FilesAndFolders[ItemSelected];
                    await CurrentInstance.NameBox.ShowAsync();
                    var input = CurrentInstance.inputForRename;
                    if (input != null)
                    {
                        if (BoxData.FileType == "Folder")
                        {
                            var item = await StorageFolder.GetFolderFromPathAsync(BoxData.FilePath);
                            await item.RenameAsync(input, NameCollisionOption.FailIfExists);
                            (type as PhotoAlbum).instanceViewModel.RemoveFileOrFolder(BoxData);
                            (type as PhotoAlbum).instanceViewModel.AddFileOrFolder(new ListedItem(item.FolderRelativeId)
                            {
                                FileName = input,
                                FileDateReal = DateTimeOffset.Now,
                                EmptyImgVis = Visibility.Collapsed,
                                FolderImg = Visibility.Visible,
                                FileIconVis = Visibility.Collapsed,
                                FileType = "Folder",
                                FileImg = null,
                                FilePath = Path.Combine((type as PhotoAlbum).instanceViewModel.Universal.path, input)
                            });
                        }
                        else
                        {
                            var item = await StorageFile.GetFileFromPathAsync(BoxData.FilePath);
                            await item.RenameAsync(input + BoxData.DotFileExtension, NameCollisionOption.FailIfExists);
                            (type as PhotoAlbum).instanceViewModel.RemoveFileOrFolder(BoxData);
                            (type as PhotoAlbum).instanceViewModel.AddFileOrFolder(new ListedItem(item.FolderRelativeId)
                            {
                                FileName = input,
                                FileDateReal = DateTimeOffset.Now,
                                EmptyImgVis = Visibility.Visible,
                                FolderImg = Visibility.Collapsed,
                                FileIconVis = Visibility.Collapsed,
                                FileType = BoxData.FileType,
                                FileImg = null,
                                FilePath = Path.Combine((type as PhotoAlbum).instanceViewModel.Universal.path, input + BoxData.DotFileExtension),
                                DotFileExtension = BoxData.DotFileExtension
                            });
                        }
                    }

                }
                catch (Exception)
                {
                    MessageDialog itemAlreadyExistsDialog = new MessageDialog("An item with this name already exists in this folder", "Try again");
                    await itemAlreadyExistsDialog.ShowAsync();
                }
                CurrentInstance.FS.isEnabled = false;
            }
        }

        List<string> pathsToDeleteAfterPaste = new List<string>();

        public List<DataGridRow> dataGridRows = new List<DataGridRow>();
        public async void CutItem_Click(object sender, RoutedEventArgs e)
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Move;
            pathsToDeleteAfterPaste.Clear();
            List<IStorageItem> items = new List<IStorageItem>();
            if (typeof(PageType) == typeof(GenericFileBrowser))
            {
                var CurrentInstance = ItemViewModel<GenericFileBrowser>.GetCurrentSelectedTabInstance<ProHome>();
                if ((CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.SelectedItems.Count != 0)
                {
                    FindChildren<DataGridRow>(dataGridRows, (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).GFBPageName.Content);
                    

                    foreach (ListedItem StorItem in (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.SelectedItems)
                    {
                        foreach (DataGridRow dataGridRow in dataGridRows)
                        {
                            if(dataGridRow.GetIndex() == StorItem.RowIndex)
                            {
                                Debug.WriteLine(dataGridRow.GetIndex());
                                (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.Columns[0].GetCellContent(dataGridRow).Opacity = 0.4;
                            }
                        }
                        var RowPressed = FindParent<DataGridRow>((CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data as DependencyObject);
                        pathsToDeleteAfterPaste.Add(StorItem.FilePath);
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
            else if (typeof(PageType) == typeof(PhotoAlbum))
            {
                var CurrentInstance = ItemViewModel<PhotoAlbum>.GetCurrentSelectedTabInstance<ProHome>();

                if ((CurrentInstance.accessibleContentFrame.Content as PhotoAlbum).gv.SelectedItems.Count != 0)
                {
                    foreach (ListedItem StorItem in (type as PhotoAlbum).gv.SelectedItems)
                    {
                        pathsToDeleteAfterPaste.Add(StorItem.FilePath);
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
            if (typeof(PageType) == typeof(GenericFileBrowser))
            {
                var CurrentInstance = ItemViewModel<GenericFileBrowser>.GetCurrentSelectedTabInstance<ProHome>();
                CopySourcePath = (type as GenericFileBrowser).instanceViewModel.Universal.path;

                if ((CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.SelectedItems.Count != 0)
                {
                    foreach (ListedItem StorItem in (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.SelectedItems)
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
            else if (typeof(PageType) == typeof(PhotoAlbum))
            {
                CopySourcePath = (type as PhotoAlbum).instanceViewModel.Universal.path;

                if ((type as PhotoAlbum).gv.SelectedItems.Count != 0)
                {
                    foreach (ListedItem StorItem in (type as PhotoAlbum).gv.SelectedItems)
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
            IEnumerable<IStorageItem> EnumerableOfItems = items;
            dataPackage.SetStorageItems(EnumerableOfItems);
            Clipboard.SetContent(dataPackage);
            Clipboard.Flush();

        }

        public async void PasteItem_ClickAsync(object sender, RoutedEventArgs e)
        {
            string DestinationPath = null;
            int oldCount;
            if (typeof(PageType) == typeof(GenericFileBrowser))
            {
                DestinationPath = (type as GenericFileBrowser).instanceViewModel.Universal.path;
                oldCount = (type as GenericFileBrowser).instanceViewModel.FilesAndFolders.Count;
            }
            else if(typeof(PageType) == typeof(PhotoAlbum))
            {
                DestinationPath = (type as PhotoAlbum).instanceViewModel.Universal.path;
                oldCount = (type as PhotoAlbum).instanceViewModel.FilesAndFolders.Count;
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
                foreach (string path in pathsToDeleteAfterPaste)
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
            NavigationActions.Refresh_Click(null, null);
        }

        public async void CloneDirectoryAsync(string SourcePath, string DestinationPath, string sourceRootName)
        {
            StorageFolder SourceFolder = await StorageFolder.GetFolderFromPathAsync(SourcePath);
            StorageFolder DestinationFolder = await StorageFolder.GetFolderFromPathAsync(DestinationPath);
            try
            {
                await DestinationFolder.CreateFolderAsync(sourceRootName, CreationCollisionOption.FailIfExists);      
                
                DestinationFolder = await StorageFolder.GetFolderFromPathAsync(DestinationPath + @"\" + sourceRootName);
                foreach (StorageFile fileInSourceDir in await SourceFolder.GetFilesAsync())
                {
                    await fileInSourceDir.CopyAsync(DestinationFolder, fileInSourceDir.Name, NameCollisionOption.GenerateUniqueName);
                }
                foreach (StorageFolder folderinSourceDir in await SourceFolder.GetFoldersAsync())
                {
                    CloneDirectoryAsync(folderinSourceDir.Path, DestinationFolder.Path, folderinSourceDir.Name);
                }
            }
            catch (Exception)
            {
                tryagain:
                MessageDialog AlreadyExistsDialog = new MessageDialog("An item with this name already exists. To continue, please enter a different name.", "Name in use");
                AlreadyExistsDialog.Commands.Add(new UICommand("Enter a name"));
                await AlreadyExistsDialog.ShowAsync();
                string newName = null;
                if (typeof(PageType) == typeof(GenericFileBrowser))
                {
                    var CurrentInstance = ItemViewModel<GenericFileBrowser>.GetCurrentSelectedTabInstance<ProHome>();
                    await (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).NameBox.ShowAsync();
                    newName = (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).inputForRename;
                }
                else if (typeof(PageType) == typeof(PhotoAlbum))
                {
                    var CurrentInstance = ItemViewModel<PhotoAlbum>.GetCurrentSelectedTabInstance<ProHome>();
                    await (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).NameBox.ShowAsync();
                    newName = (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).inputForRename;
                }
                
                if(newName != sourceRootName)
                {
                    await DestinationFolder.CreateFolderAsync(newName);
                    
                    DestinationFolder = await StorageFolder.GetFolderFromPathAsync(DestinationPath + @"\" + newName);
                    foreach (StorageFile fileInSourceDir in await SourceFolder.GetFilesAsync())
                    {
                        await fileInSourceDir.CopyAsync(DestinationFolder, fileInSourceDir.Name, NameCollisionOption.GenerateUniqueName);
                    }
                    foreach (StorageFolder folderinSourceDir in await SourceFolder.GetFoldersAsync())
                    {
                        CloneDirectoryAsync(folderinSourceDir.Path, DestinationFolder.Path, folderinSourceDir.Name);
                    }
                }
                else
                {
                    goto tryagain;
                }
            } 
        }

        public void SelectAllItems()
        {
            if(typeof(PageType) == typeof(GenericFileBrowser))
            {
                var CurrentInstance = ItemViewModel<GenericFileBrowser>.GetCurrentSelectedTabInstance<ProHome>();
                foreach (ListedItem li in (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.ItemsSource)
                {
                    if (!(CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.SelectedItems.Contains(li))
                    {
                        (CurrentInstance.accessibleContentFrame.Content as GenericFileBrowser).data.SelectedItems.Add(li);
                    }
                }
            }
            else if(typeof(PageType) == typeof(PhotoAlbum))
            {
                (type as PhotoAlbum).gv.SelectAll();
            }
        }
    }
}