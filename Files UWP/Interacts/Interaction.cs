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

namespace Files.Interacts
{
    public class Interaction
    {
        public static PasteState PS { get; } = new PasteState();

        public static Page page;
        public Interaction(Page p)
        {
            page = p;
        }

        public static MessageDialog message;

        // Double-tap event for DataGrid
        public static async void List_ItemClick(object sender, DoubleTappedRoutedEventArgs e)
        {
            
            if (page.Name == "GenericItemView")
            {
                var index = GenericFileBrowser.data.SelectedIndex;
                if (index > -1)
                {
                    var clickedOnItem = App.ViewModel.FilesAndFolders[index];
                    if (clickedOnItem.FileType == "Folder")
                    {
                        App.ViewModel.Universal.path = clickedOnItem.FilePath;
                        App.PathText.Text = clickedOnItem.FilePath;
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        History.ForwardList.Clear();
                        App.ViewModel.FS.isEnabled = false;
                        App.ViewModel.FilesAndFolders.Clear();
                        if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory))
                        {
                            App.PathText.Text = "Desktop";
                            foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                            {
                                if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "DesktopIC")
                                {
                                    MainPage.Select.itemSelected = NavItemChoice;
                                    break;
                                }
                            }
                            MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.DesktopPath, new SuppressNavigationTransitionInfo());
                            MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Desktop";
                        }
                        else if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
                        {
                            App.PathText.Text = "Documents";
                            foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                            {
                                if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "DocumentsIC")
                                {
                                    MainPage.Select.itemSelected = NavItemChoice;
                                    break;
                                }
                            }
                            MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.DocumentsPath, new SuppressNavigationTransitionInfo());
                            MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Documents";
                        }
                        else if (clickedOnItem.FilePath == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
                        {
                            App.PathText.Text = "Downloads";
                            foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                            {
                                if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "DownloadsIC")
                                {
                                    MainPage.Select.itemSelected = NavItemChoice;
                                    break;
                                }
                            }
                            MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.DownloadsPath, new SuppressNavigationTransitionInfo());
                            MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Downloads";
                        }
                        else if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.MyPictures))
                        {
                            foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                            {
                                if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "PicturesIC")
                                {
                                    MainPage.Select.itemSelected = NavItemChoice;
                                    break;
                                }
                            }
                            MainPage.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.PicturesPath, new SuppressNavigationTransitionInfo());
                            MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Pictures";
                            App.PathText.Text = "Pictures";
                        }
                        else if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))
                        {
                            App.PathText.Text = "Music";
                            foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                            {
                                if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "MusicIC")
                                {
                                    MainPage.Select.itemSelected = NavItemChoice;
                                    break;
                                }
                            }
                            MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.MusicPath, new SuppressNavigationTransitionInfo());
                            MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Music";
                        }
                        else if (clickedOnItem.FilePath == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
                        {
                            App.PathText.Text = "OneDrive";
                            foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                            {
                                if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "OneD_IC")
                                {
                                    MainPage.Select.itemSelected = NavItemChoice;
                                    break;
                                }
                            }
                            MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.OneDrivePath, new SuppressNavigationTransitionInfo());
                            MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search OneDrive";
                        }
                        else if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.MyVideos))
                        {
                            App.PathText.Text = "Videos";
                            foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                            {
                                if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "VideosIC")
                                {
                                    MainPage.Select.itemSelected = NavItemChoice;
                                    break;
                                }
                            }
                            MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.VideosPath, new SuppressNavigationTransitionInfo());
                            MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Videos";
                        }
                        else
                        {
                            if (clickedOnItem.FilePath.Contains("C:"))
                            {
                                foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                                {
                                    if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "LocD_IC")
                                    {
                                        MainPage.Select.itemSelected = NavItemChoice;
                                        break;
                                    }
                                }
                            }
                            App.ViewModel.Universal.path = clickedOnItem.FilePath;
                            App.ViewModel.AddItemsToCollectionAsync(App.ViewModel.Universal.path, GenericFileBrowser.GFBPageName);
                        }
                    }
                    else if (clickedOnItem.FileType == "Application")
                    {
                        //message = new MessageDialog("We noticed you’re trying to run an Application file. This type of file may be a security risk to your device, and is not supported by the Universal Windows Platform. If you're not sure what this means, check out the Microsoft Store for a large selection of secure apps, games, and more.");
                        //message.Title = "Unsupported Functionality";
                        //message.Commands.Add(new UICommand("Continue...", new UICommandInvokedHandler(Interaction.CommandInvokedHandler)));
                        //message.Commands.Add(new UICommand("Cancel"));
                        //await message.ShowAsync();
                        await LaunchExe(clickedOnItem.FilePath);

                    }
                    else
                    {
                        StorageFile file = await StorageFile.GetFileFromPathAsync(clickedOnItem.FilePath);
                        var options = new LauncherOptions
                        {
                            DisplayApplicationPicker = false

                        };
                        await Launcher.LaunchFileAsync(file, options);
                    }
                }
            }
            else if (page.Name == "PhotoAlbumViewer")
            {
                var index = PhotoAlbum.gv.SelectedIndex;

                if (index > -1)
                {
                    var clickedOnItem = App.ViewModel.FilesAndFolders[index];
                    if (clickedOnItem.FileType == "Folder")
                    {
                        App.ViewModel.Universal.path = clickedOnItem.FilePath;
                        App.PathText.Text = clickedOnItem.FilePath;
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        History.ForwardList.Clear();
                        App.ViewModel.FS.isEnabled = false;
                        App.ViewModel.FilesAndFolders.Clear();
                        if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory))
                        {
                            App.PathText.Text = "Desktop";
                            foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                            {
                                if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "DesktopIC")
                                {
                                    MainPage.Select.itemSelected = NavItemChoice;
                                    break;
                                }
                            }
                            MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.DesktopPath, new SuppressNavigationTransitionInfo());
                            MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Desktop";
                        }
                        else if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
                        {
                            App.PathText.Text = "Documents";
                            foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                            {
                                if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "DocumentsIC")
                                {
                                    MainPage.Select.itemSelected = NavItemChoice;
                                    break;
                                }
                            }
                            MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.DocumentsPath, new SuppressNavigationTransitionInfo());
                            MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Documents";
                        }
                        else if (clickedOnItem.FilePath == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
                        {
                            App.PathText.Text = "Downloads";
                            foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                            {
                                if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "DownloadsIC")
                                {
                                    MainPage.Select.itemSelected = NavItemChoice;
                                    break;
                                }
                            }
                            MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.DownloadsPath, new SuppressNavigationTransitionInfo());
                            MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Downloads";
                        }
                        else if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.MyPictures))
                        {
                            foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                            {
                                if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "PicturesIC")
                                {
                                    MainPage.Select.itemSelected = NavItemChoice;
                                    break;
                                }
                            }
                            MainPage.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.PicturesPath, new SuppressNavigationTransitionInfo());
                            MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Pictures";
                            App.PathText.Text = "Pictures";
                        }
                        else if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))
                        {
                            App.PathText.Text = "Music";
                            foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                            {
                                if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "MusicIC")
                                {
                                    MainPage.Select.itemSelected = NavItemChoice;
                                    break;
                                }
                            }
                            MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.MusicPath, new SuppressNavigationTransitionInfo());
                            MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Music";
                        }
                        else if (clickedOnItem.FilePath == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
                        {
                            App.PathText.Text = "OneDrive";
                            foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                            {
                                if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "OneD_IC")
                                {
                                    MainPage.Select.itemSelected = NavItemChoice;
                                    break;
                                }
                            }
                            MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.OneDrivePath, new SuppressNavigationTransitionInfo());
                            MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search OneDrive";
                        }
                        else if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.MyVideos))
                        {
                            App.PathText.Text = "Videos";
                            foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                            {
                                if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "VideosIC")
                                {
                                    MainPage.Select.itemSelected = NavItemChoice;
                                    break;
                                }
                            }
                            MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.VideosPath, new SuppressNavigationTransitionInfo());
                            MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Videos";
                        }
                        else
                        {
                            App.PathText.Text = clickedOnItem.FilePath;
                            foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                            {
                                if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "LocD_IC")
                                {
                                    MainPage.Select.itemSelected = NavItemChoice;
                                    break;
                                }
                            }
                            App.ViewModel.AddItemsToCollectionAsync(clickedOnItem.FilePath, PhotoAlbum.PAPageName);
                        }
                    }
                    else if (clickedOnItem.FileType == "Application")
                    {
                        //Interaction.message = new MessageDialog("We noticed you’re trying to run an Application file. This type of file may be a security risk to your device, and is not supported by the Universal Windows Platform. If you're not sure what this means, check out the Microsoft Store for a large selection of secure apps, games, and more.");
                        //Interaction.message.Title = "Unsupported Functionality";
                        //Interaction.message.Commands.Add(new UICommand("Continue...", new UICommandInvokedHandler(Interaction.CommandInvokedHandler)));
                        //Interaction.message.Commands.Add(new UICommand("Cancel"));
                        //await Interaction.message.ShowAsync();
                        await LaunchExe(clickedOnItem.FilePath);

                    }
                    else
                    {
                        StorageFile file = await StorageFile.GetFileFromPathAsync(clickedOnItem.FilePath);
                        var options = new LauncherOptions
                        {
                            DisplayApplicationPicker = false

                        };
                        await Launcher.LaunchFileAsync(file, options);
                    }
                }

            }

        }


        public static async Task LaunchExe(string ApplicationPath)
        {
            Debug.WriteLine("Launching EXE in FullTrustProcess");
            ApplicationData.Current.LocalSettings.Values["Application"] = ApplicationPath;
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }

        public static async void CommandInvokedHandler(IUICommand command)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-windows-store://home"));
        }

        public static async void GrantAccessPermissionHandler(IUICommand command)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess"));
        }

        public static DataGrid dataGrid;

        public static void AllView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            dataGrid = (DataGrid)sender;
            var RowPressed = FindParent<DataGridRow>(e.OriginalSource as DependencyObject);

            // If user clicks on header
            if (RowPressed == null)
            {
                GenericFileBrowser.HeaderContextMenu.ShowAt(dataGrid, e.GetPosition(dataGrid));
            }
            // If user clicks on actual row
            else
            {
                var ObjectPressed = ((ObservableCollection<ListedItem>)dataGrid.ItemsSource)[RowPressed.GetIndex()];
                dataGrid.SelectedItems.Add(ObjectPressed);
                GenericFileBrowser.context.ShowAt(dataGrid, e.GetPosition(dataGrid));
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

        public static void FileList_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            GridView gridView = (GridView)sender;
            var selItems = gridView.SelectedItems;
            if(selItems.Count > 0)
            {
                PhotoAlbum.context.ShowAt(gridView);
            }
            else
            {
                PhotoAlbum.gridContext.ShowAt(PhotoAlbum.PAPageName, e.GetPosition(PhotoAlbum.PAPageName));
            }
            
        }

        public static async void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            if (page.Name == "GenericItemView")
            {
                var ItemSelected = GenericFileBrowser.data.SelectedIndex;
                var RowData = App.ViewModel.FilesAndFolders[ItemSelected];

                if (RowData.FileType == "Folder")
                {
                    App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                    History.ForwardList.Clear();
                    App.ViewModel.FS.isEnabled = false;
                    App.ViewModel.FilesAndFolders.Clear();
                    App.ViewModel.Universal.path = RowData.FilePath;
                    App.ViewModel.AddItemsToCollectionAsync(App.ViewModel.Universal.path, GenericFileBrowser.GFBPageName);
                }
                else
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(RowData.FilePath);
                    var options = new LauncherOptions();
                    options.DisplayApplicationPicker = true;
                    await Launcher.LaunchFileAsync(file, options);
                }
            }
            else if (page.Name == "PhotoAlbumViewer")
            {
                var ItemSelected = PhotoAlbum.gv.SelectedIndex;
                var RowData = App.ViewModel.FilesAndFolders[ItemSelected];

                if (RowData.FileType == "Folder")
                {
                    App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                    History.ForwardList.Clear();
                    App.ViewModel.FS.isEnabled = false;
                    App.ViewModel.FilesAndFolders.Clear();
                    App.ViewModel.Universal.path = RowData.FilePath;
                    App.ViewModel.AddItemsToCollectionAsync(RowData.FilePath, PhotoAlbum.PAPageName);
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

        public static void ShareItem_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager manager = DataTransferManager.GetForCurrentView();
            manager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(Manager_DataRequested);
            DataTransferManager.ShowShareUI();
        }

        private async static void Manager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequestDeferral dataRequestDeferral = args.Request.GetDeferral();
            List<IStorageItem> items = new List<IStorageItem>();
            if(page.Name == "GenericItemView")
            {
                foreach (ListedItem li in dataGrid.SelectedItems)
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
            else if (page.Name == "PhotoAlbumViewer")
            {
                foreach (ListedItem li in PhotoAlbum.gv.SelectedItems)
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

        public static async void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (page.Name == "GenericItemView")
                {
                    List<ListedItem> selectedItems = new List<ListedItem>();
                    foreach(ListedItem selectedItem in GenericFileBrowser.data.SelectedItems)
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
                        App.ViewModel.FilesAndFolders.Remove(storItem);
                    }
                    Debug.WriteLine("Ended for loop");
                    History.ForwardList.Clear();
                    App.ViewModel.FS.isEnabled = false;
                }
                else if (page.Name == "PhotoAlbumViewer")
                {
                    List<ListedItem> selectedItems = new List<ListedItem>();
                    foreach (ListedItem selectedItem in PhotoAlbum.gv.SelectedItems)
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
                        App.ViewModel.FilesAndFolders.Remove(storItem);
                    }
                    Debug.WriteLine("Ended for loop");
                    History.ForwardList.Clear();
                    App.ViewModel.FS.isEnabled = false;
                }
                
            }
            catch (UnauthorizedAccessException)
            {
                MessageDialog AccessDeniedDialog = new MessageDialog("Access Denied", "Unable to delete this item");
                await AccessDeniedDialog.ShowAsync();
            }
        }

        public static async void RenameItem_Click(object sender, RoutedEventArgs e)
        {
            if(page.Name == "GenericItemView")
            {
                try
                {
                    var ItemSelected = GenericFileBrowser.data.SelectedIndex;
                    var RowData = App.ViewModel.FilesAndFolders[ItemSelected];
                    await GenericFileBrowser.NameBox.ShowAsync();
                    var input = GenericFileBrowser.inputForRename;
                    if (input != null)
                    {
                        if (RowData.FileType == "Folder")
                        {
                            var item = await StorageFolder.GetFolderFromPathAsync(RowData.FilePath);
                            await item.RenameAsync(input, NameCollisionOption.FailIfExists);
                            App.ViewModel.FilesAndFolders.Remove(RowData);
                            App.ViewModel.FilesAndFolders.Add(new ListedItem() { FileName = input, FileDate = "Now", EmptyImgVis = Visibility.Collapsed, FolderImg = Visibility.Visible, FileIconVis = Visibility.Collapsed, FileType = "Folder", FileImg = null, FilePath = (App.ViewModel.Universal.path + "\\" + input) });
                        }
                        else
                        {
                            var item = await StorageFile.GetFileFromPathAsync(RowData.FilePath);
                            await item.RenameAsync(input + RowData.DotFileExtension, NameCollisionOption.FailIfExists);
                            App.ViewModel.FilesAndFolders.Remove(RowData);
                            App.ViewModel.FilesAndFolders.Add(new ListedItem() { FileName = input, FileDate = "Now", EmptyImgVis = Visibility.Visible, FolderImg = Visibility.Collapsed, FileIconVis = Visibility.Collapsed, FileType = RowData.FileType, FileImg = null, FilePath = (App.ViewModel.Universal.path + "\\" + input + RowData.DotFileExtension), DotFileExtension = RowData.DotFileExtension });

                        }
                    }

                }
                catch (Exception)
                {
                    MessageDialog itemAlreadyExistsDialog = new MessageDialog("An item with this name already exists in this folder", "Try again");
                    await itemAlreadyExistsDialog.ShowAsync();
                }
            }
            else if (page.Name == "PhotoAlbumViewer")
            {
                try
                {
                    var ItemSelected = PhotoAlbum.gv.SelectedIndex;
                    var BoxData = App.ViewModel.FilesAndFolders[ItemSelected];
                    await PhotoAlbum.NameBox.ShowAsync();
                    var input = PhotoAlbum.inputForRename;
                    if (input != null)
                    {
                        if (BoxData.FileType == "Folder")
                        {
                            var item = await StorageFolder.GetFolderFromPathAsync(BoxData.FilePath);
                            await item.RenameAsync(input, NameCollisionOption.FailIfExists);
                            App.ViewModel.FilesAndFolders.Remove(BoxData);
                            App.ViewModel.FilesAndFolders.Add(new ListedItem() { FileName = input, FileDate = "Now", EmptyImgVis = Visibility.Collapsed, FolderImg = Visibility.Visible, FileIconVis = Visibility.Collapsed, FileType = "Folder", FileImg = null, FilePath = (App.ViewModel.Universal.path + "\\" + input) });
                        }
                        else
                        {
                            var item = await StorageFile.GetFileFromPathAsync(BoxData.FilePath);
                            await item.RenameAsync(input + BoxData.DotFileExtension, NameCollisionOption.FailIfExists);
                            App.ViewModel.FilesAndFolders.Remove(BoxData);
                            App.ViewModel.FilesAndFolders.Add(new ListedItem() { FileName = input, FileDate = "Now", EmptyImgVis = Visibility.Visible, FolderImg = Visibility.Collapsed, FileIconVis = Visibility.Collapsed, FileType = BoxData.FileType, FileImg = null, FilePath = (App.ViewModel.Universal.path + "\\" + input + BoxData.DotFileExtension), DotFileExtension = BoxData.DotFileExtension });

                        }
                    }

                }
                catch (Exception)
                {
                    MessageDialog itemAlreadyExistsDialog = new MessageDialog("An item with this name already exists in this folder", "Try again");
                    await itemAlreadyExistsDialog.ShowAsync();
                }
            }
            History.ForwardList.Clear();
            App.ViewModel.FS.isEnabled = false;
        }

        static List<string> pathsToDeleteAfterPaste = new List<string>();

        public async static void CutItem_Click(object sender, RoutedEventArgs e)
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Move;
            pathsToDeleteAfterPaste.Clear();
            List<IStorageItem> items = new List<IStorageItem>();
            if (page.Name == "GenericItemView")
            {
                if (GenericFileBrowser.data.SelectedItems.Count != 0)
                {
                    foreach (ListedItem StorItem in GenericFileBrowser.data.SelectedItems)
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
            else if (page.Name == "PhotoAlbumViewer")
            {
                if (PhotoAlbum.gv.SelectedItems.Count != 0)
                {
                    foreach (ListedItem StorItem in PhotoAlbum.gv.SelectedItems)
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
        }
        public static string CopySourcePath;
        public static async void CopyItem_ClickAsync(object sender, RoutedEventArgs e)
        {
            CopySourcePath = App.ViewModel.Universal.path;
            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            List<IStorageItem> items = new List<IStorageItem>();
            if (page.Name == "GenericItemView")
            {
                if (GenericFileBrowser.data.SelectedItems.Count != 0)
                {
                    foreach (ListedItem StorItem in GenericFileBrowser.data.SelectedItems)
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
            else if (page.Name == "PhotoAlbumViewer")
            {
                if (PhotoAlbum.gv.SelectedItems.Count != 0)
                {
                    foreach (ListedItem StorItem in PhotoAlbum.gv.SelectedItems)
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

        }

        public static async void PasteItem_ClickAsync(object sender, RoutedEventArgs e)
        {
            var DestinationPath = App.ViewModel.Universal.path;
            DataPackageView packageView = Clipboard.GetContent();
            var oldCount = App.ViewModel.FilesAndFolders.Count;
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
                foreach(string path in pathsToDeleteAfterPaste)
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
                    if (page.Name == "GenericItemView")
                    {
                        NavigationActions.Refresh_Click(null, null);
                    }
                    else if (page.Name == "PhotoAlbumViewer")
                    {
                        PhotoAlbumNavActions.Refresh_Click(null, null);
                    }
                
            
        }

        public static async void CloneDirectoryAsync(string SourcePath, string DestinationPath, string sourceRootName)
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
                await GenericFileBrowser.NameBox.ShowAsync();
                var newName = GenericFileBrowser.inputForRename;
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
    }
}