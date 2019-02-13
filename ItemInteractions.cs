using Microsoft.Toolkit.Uwp.UI.Controls;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using System;
using Files;
using ItemListPresenter;
using Navigation;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using System.Collections.Generic;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Media.Animation;
using System.ComponentModel;

namespace Interacts
{



    public class Interaction
    {

        private static PasteState ps = new PasteState();
        public static PasteState PS { get { return ps; } }

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

                if(index > -1)
                {
                    var clickedOnItem = ItemViewModel.FilesAndFolders[index];

                    if (clickedOnItem.FileExtension == "Folder")
                    {
                        ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                        History.ForwardList.Clear();
                        ItemViewModel.FS.isEnabled = false;
                        ItemViewModel.FilesAndFolders.Clear();
                        if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory))
                        {
                            GenericFileBrowser.P.path = "Desktop";
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
                            GenericFileBrowser.P.path = "Documents";
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
                            GenericFileBrowser.P.path = "Downloads";
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
                            GenericFileBrowser.P.path = "Pictures";
                        }
                        else if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))
                        {
                            GenericFileBrowser.P.path = "Music";
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
                        else if(clickedOnItem.FilePath == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
                        {
                            GenericFileBrowser.P.path = "OneDrive";
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
                            GenericFileBrowser.P.path = "Videos";
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
                            GenericFileBrowser.P.path = clickedOnItem.FilePath;
                            ItemViewModel.ViewModel = new ItemViewModel(clickedOnItem.FilePath, GenericFileBrowser.GFBPageName);
                        }
                    }
                    else if (clickedOnItem.FileExtension == "Executable")
                    {
                        message = new MessageDialog("We noticed you’re trying to run an executable file. This type of file may be a security risk to your device, and is not supported by the Universal Windows Platform. If you're not sure what this means, check out the Microsoft Store for a large selection of secure apps, games, and more.");
                        message.Title = "Unsupported Functionality";
                        message.Commands.Add(new UICommand("Continue...", new UICommandInvokedHandler(Interaction.CommandInvokedHandler)));
                        message.Commands.Add(new UICommand("Cancel"));
                        await message.ShowAsync();
                    }
                    else
                    {
                        StorageFile file = await StorageFile.GetFileFromPathAsync(clickedOnItem.FilePath);
                        var options = new LauncherOptions
                        {
                            DisplayApplicationPicker = true
                            
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
                    var clickedOnItem = ItemViewModel.FilesAndFolders[index];

                    if (clickedOnItem.FileExtension == "Folder")
                    {
                        ItemViewModel.TextState.isVisible = Windows.UI.Xaml.Visibility.Collapsed;
                        History.ForwardList.Clear();
                        ItemViewModel.FS.isEnabled = false;
                        ItemViewModel.FilesAndFolders.Clear();
                        if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory))
                        {
                            GenericFileBrowser.P.path = "Desktop";
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
                            GenericFileBrowser.P.path = "Documents";
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
                            GenericFileBrowser.P.path = "Downloads";
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
                            GenericFileBrowser.P.path = "Pictures";
                        }
                        else if (clickedOnItem.FilePath == Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))
                        {
                            GenericFileBrowser.P.path = "Music";
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
                            GenericFileBrowser.P.path = "OneDrive";
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
                            GenericFileBrowser.P.path = "Videos";
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
                            GenericFileBrowser.P.path = clickedOnItem.FilePath;
                            foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                            {
                                if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "LocD_IC")
                                {
                                    MainPage.Select.itemSelected = NavItemChoice;
                                    break;
                                }
                            }
                            ItemViewModel.ViewModel = new ItemViewModel(clickedOnItem.FilePath, PhotoAlbum.PAPageName);
                        }
                    }
                    else if (clickedOnItem.FileExtension == "Executable")
                    {
                        Interaction.message = new MessageDialog("We noticed you’re trying to run an executable file. This type of file may be a security risk to your device, and is not supported by the Universal Windows Platform. If you're not sure what this means, check out the Microsoft Store for a large selection of secure apps, games, and more.");
                        Interaction.message.Title = "Unsupported Functionality";
                        Interaction.message.Commands.Add(new UICommand("Continue...", new UICommandInvokedHandler(Interaction.CommandInvokedHandler)));
                        Interaction.message.Commands.Add(new UICommand("Cancel"));
                        await Interaction.message.ShowAsync();

                    }
                    else
                    {
                        StorageFile file = await StorageFile.GetFileFromPathAsync(clickedOnItem.FilePath);
                        var options = new LauncherOptions
                        {
                            DisplayApplicationPicker = true

                        };
                        await Launcher.LaunchFileAsync(file, options);
                    }
                }

            }
           
        }

        public static async void CommandInvokedHandler(IUICommand command)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-windows-store://home"));
        }

        public static async void GrantAccessPermissionHandler(IUICommand command)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess"));
        }

        //public static async void PhotoAlbumItemList_ClickAsync(object sender, DoubleTappedRoutedEventArgs e)
        //{
        //    GridView grid = sender as GridView;
        //    var index = grid.Items.IndexOf(e.);
        //    var clickedOnItem = ItemViewModel.FilesAndFolders[index];
            
        //    //Debug.WriteLine("Reached PhotoAlbumViewer event");

        //    if (clickedOnItem.FileExtension == "Folder")
        //    {

        //        ItemViewModel.TextState.isVisible = Visibility.Collapsed;
        //        History.ForwardList.Clear();
        //        ItemViewModel.FS.isEnabled = false;
        //        ItemViewModel.FilesAndFolders.Clear();
        //        ItemViewModel.ViewModel = new ItemViewModel(clickedOnItem.FilePath, PhotoAlbum.PAPageName);
        //        GenericFileBrowser.P.path = clickedOnItem.FilePath;

        //    }
        //    else
        //    {
        //        StorageFile file = await StorageFile.GetFileFromPathAsync(clickedOnItem.FilePath);
        //        var options = new LauncherOptions();
        //        options.DisplayApplicationPicker = true;
        //        await Launcher.LaunchFileAsync(file, options); 
        //        //var uri = new Uri(clickedOnItem.FilePath);
        //        //BitmapImage bitmap = new BitmapImage();
        //        //bitmap.UriSource = uri;
        //        //LIS.image = bitmap;
        //        //PhotoAlbum.largeImg.Source = bitmap;
        //    }
        //}

        public static DataGrid dataGrid;

        public static void AllView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            dataGrid = (DataGrid)sender;
            var RowPressed  = FindParent<DataGridRow>(e.OriginalSource as DependencyObject);

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
            while(CurrentParent != null)
            {
                if(CurrentParent is T)
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
            
        }

        public static async void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            if(page.Name == "GenericItemView")
            {
                var ItemSelected = GenericFileBrowser.data.SelectedIndex;
                var RowData = ItemViewModel.FilesAndFolders[ItemSelected];

                if (RowData.FileExtension == "Folder")
                {
                    ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                    History.ForwardList.Clear();
                    ItemViewModel.FS.isEnabled = false;
                    ItemViewModel.FilesAndFolders.Clear();
                    ItemViewModel.ViewModel = new ItemViewModel(RowData.FilePath, GenericFileBrowser.GFBPageName);
                    GenericFileBrowser.P.path = RowData.FilePath;
                }
                else
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(RowData.FilePath);
                    var options = new LauncherOptions();
                    options.DisplayApplicationPicker = true;
                    await Launcher.LaunchFileAsync(file, options);
                }
            }
            else if(page.Name == "PhotoAlbumViewer")
            {
                var ItemSelected = PhotoAlbum.gv.SelectedIndex;
                var RowData = ItemViewModel.FilesAndFolders[ItemSelected];

                if (RowData.FileExtension == "Folder")
                {
                    ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                    History.ForwardList.Clear();
                    ItemViewModel.FS.isEnabled = false;
                    ItemViewModel.FilesAndFolders.Clear();
                    ItemViewModel.ViewModel = new ItemViewModel(RowData.FilePath, PhotoAlbum.PAPageName);
                    GenericFileBrowser.P.path = RowData.FilePath;
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
            manager.DataRequested += Manager_DataRequested;
            DataTransferManager.ShowShareUI();
        }

        private static void Manager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            foreach(IStorageItem item in dataGrid.ItemsSource)
            {

            }
            DataRequest dataRequest = args.Request;
            dataRequest.Data.SetStorageItems(dataGrid.ItemsSource as IEnumerable<IStorageItem>);
            dataRequest.Data.Properties.Title = "Data Shared From Files UWP";
            dataRequest.Data.Properties.Description = "The files/folders you selected will be shared";

        }

        public static void DeleteItem_Click(object sender, RoutedEventArgs e)
        {

        }

        public static void RenameItem_Click(object sender, RoutedEventArgs e)
        {
            var ItemSelected = GenericFileBrowser.data.SelectedIndex;
            var RowData = ItemViewModel.FilesAndFolders[ItemSelected];
            GenericFileBrowser.data.BeginEdit();
            
        }

        public static void CutItem_Click(object sender, RoutedEventArgs e)
        {

        }

        public static async void CopyItem_ClickAsync(object sender, RoutedEventArgs e)
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            if(GenericFileBrowser.data.SelectedItems.Count != 0)
            {
                List<IStorageItem> items = new List<IStorageItem>();
                foreach (ListedItem StorItem in GenericFileBrowser.data.SelectedItems)
                {
                    if(StorItem.FileExtension != "Folder")
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
                
                IEnumerable<IStorageItem> EnumerableOfItems = items;
                dataPackage.SetStorageItems(EnumerableOfItems);
                Clipboard.SetContent(dataPackage);
                
            }
        }
        public static bool isSkipEnabled = false;
        public static bool isReplaceEnabled = false;
        public static bool isReviewEnabled = false;
        public static IStorageItem ItemSnapshot;
        public static string DestinationPathSnapshot;
        public static bool isLoopPaused = false;

        public static async void PasteItem_ClickAsync(object sender, RoutedEventArgs e)
        {
            // TODO: Add progress box and collision options for this operation
            var DestinationPath = ItemViewModel.PUIP.Path;
            DataPackageView packageView = Clipboard.GetContent();
            var ItemsToPaste = await packageView.GetStorageItemsAsync();
            foreach(IStorageItem item in ItemsToPaste)
            {
               
                try // tries to do this if collision doesn't happen
                {
                    if (isReplaceEnabled)
                    {
                        if (item.IsOfType(StorageItemTypes.Folder))
                        {
                            CloneDirectoryAsync(item.Path, DestinationPath, item.Name, true, item);
                        }
                        else if (item.IsOfType(StorageItemTypes.File))
                        {
                            StorageFile ClipboardFile = await StorageFile.GetFileFromPathAsync(item.Path);
                            await ClipboardFile.CopyAndReplaceAsync(await StorageFile.GetFileFromPathAsync(DestinationPath + @"\" + item.Name));
                        }
                    }
                    else if (isSkipEnabled)
                    {
                        // Skip doing anything with file entirely
                    }
                    else if (isReviewEnabled)
                    {
                        ItemSnapshot = item;
                        DestinationPathSnapshot = DestinationPath;
                        ItemViewModel.DisplayReviewUIWithArgs("Skip or Replace This Item?", "An item already exists with the name " + item.Name + ".");
                        isLoopPaused = true;
                    }
                    else   // First time of this collision, so prompt for user choice
                    {
                        ItemViewModel.DisplayCollisionUIWithArgs("Replace All Existing Items?", "You can choose whether to replace or skip all items if there are more than one. Optionally, you can review each one individually.");
                        return;
                    }
                }
                catch (Exception)
                {
                    if (isReplaceEnabled)
                    {
                        if (item.IsOfType(StorageItemTypes.Folder))
                        {
                            CloneDirectoryAsync(item.Path, DestinationPath, item.Name, true, item);
                        }
                        else if (item.IsOfType(StorageItemTypes.File))
                        {
                            StorageFile ClipboardFile = await StorageFile.GetFileFromPathAsync(item.Path);
                            await ClipboardFile.CopyAndReplaceAsync(await StorageFile.GetFileFromPathAsync(DestinationPath + @"\" + item.Name));
                        }
                    }
                    else if (isSkipEnabled)
                    {
                        // Skip doing anything with file entirely
                    }
                    else if (isReviewEnabled)
                    {
                        ItemSnapshot = item;
                        DestinationPathSnapshot = DestinationPath;
                        ItemViewModel.DisplayReviewUIWithArgs("Skip or Replace This Item?", "An item already exists with the name " + item.Name + ".");

                    }
                    else   // First time of this collision, so prompt for user choice
                    {
                        ItemViewModel.DisplayCollisionUIWithArgs("Replace All Existing Items?", "You can choose whether to replace or skip all items if there are more than one. Optionally, you can review each one individually.");
                        return;
                    }
                    
                }
                

            }
            
            NavigationActions.Refresh_Click(null, null);

        }
        static int passNum = 0;
        private static async void CloneDirectoryAsync(string SourcePath, string DestinationPath, string DirName, bool replaceRoot, IStorageItem item)
        {
            passNum++;
            StorageFolder SourceFolder = await StorageFolder.GetFolderFromPathAsync(SourcePath);
            StorageFolder DestinationFolder = await StorageFolder.GetFolderFromPathAsync(DestinationPath);

            if(passNum == 1)
            {
                if (!replaceRoot)
                {
                    try
                    {
                        await DestinationFolder.CreateFolderAsync(DirName);
                        DestinationPath = DestinationPath + @"\" + DirName;
                        DestinationFolder = await StorageFolder.GetFolderFromPathAsync(DestinationPath);
                        //    SourcePath = SourcePath + @"\" + DirName;
                        //    SourceFolder = await StorageFolder.GetFolderFromPathAsync(SourcePath);
                    }
                    catch (Exception)
                    {
                        if (isReplaceEnabled)
                        {
                            if (item.IsOfType(StorageItemTypes.Folder))
                            {
                                CloneDirectoryAsync(item.Path, DestinationPath, item.Name, true, item);
                            }
                            else if (item.IsOfType(StorageItemTypes.File))
                            {
                                StorageFile ClipboardFile = await StorageFile.GetFileFromPathAsync(item.Path);
                                await ClipboardFile.CopyAndReplaceAsync(await StorageFile.GetFileFromPathAsync(DestinationPath + @"\" + item.Name));
                            }
                        }
                        else if (isSkipEnabled)
                        {
                            // Skip doing anything with file entirely
                        }
                        else if (isReviewEnabled)
                        {
                            ItemSnapshot = item;
                            DestinationPathSnapshot = DestinationPath;
                            ItemViewModel.DisplayReviewUIWithArgs("Skip or Replace This Item?", "An item already exists with the name " + item.Name + ".");

                        }
                        else   // First time of this collision, so prompt for user choice
                        {
                            ItemViewModel.DisplayCollisionUIWithArgs("Replace All Existing Items?", "You can choose whether to replace or skip all items if there are more than one. Optionally, you can review each one individually.");
                            return;
                        }

                    }


                }
                else
                {
                    string ExistingFolderPath;
                    ExistingFolderPath = DestinationPath + @"\" + DirName;
                    StorageFolder ExistingFolder = await StorageFolder.GetFolderFromPathAsync(ExistingFolderPath);
                    await ExistingFolder.DeleteAsync();
                    await DestinationFolder.CreateFolderAsync(DirName);
                    DestinationPath = DestinationPath + @"\" + DirName;
                    DestinationFolder = await StorageFolder.GetFolderFromPathAsync(DestinationPath);
                }


            }
            try { 
                Debug.WriteLine("Pass " + passNum);
                foreach (StorageFile file in await SourceFolder.GetFilesAsync())
                {
                    await file.CopyAsync(DestinationFolder);
                }
                foreach (StorageFolder folder in await SourceFolder.GetFoldersAsync())
                {
                    await DestinationFolder.CreateFolderAsync(folder.DisplayName);
                    CloneDirectoryAsync(folder.Path, DestinationPath + @"\" + folder.DisplayName, folder.DisplayName, false, item);
                }
            }
            catch (Exception)
            {
                if (isReplaceEnabled)
                {
                    if (item.IsOfType(StorageItemTypes.Folder))
                    {
                        CloneDirectoryAsync(item.Path, DestinationPath, item.Name, true, item);
                    }
                    else if (item.IsOfType(StorageItemTypes.File))
                    {
                        StorageFile ClipboardFile = await StorageFile.GetFileFromPathAsync(item.Path);
                        await ClipboardFile.CopyAndReplaceAsync(await StorageFile.GetFileFromPathAsync(DestinationPath + @"\" + item.Name));
                    }
                }
                else if (isSkipEnabled)
                {
                    // Skip doing anything with file entirely
                }
                else if (isReviewEnabled)
                {
                    ItemSnapshot = item;
                    DestinationPathSnapshot = DestinationPath;
                    ItemViewModel.DisplayReviewUIWithArgs("Skip or Replace This Item?", "An item already exists with the name " + item.Name + ".");

                }
                else   // First time of this collision, so prompt for user choice
                {
                    ItemViewModel.DisplayCollisionUIWithArgs("Replace All Existing Items?", "You can choose whether to replace or skip all items if there are more than one. Optionally, you can review each one individually.");
                    return;
                }

            }
        }

        public static void CollisionLVItemClick(object sender, ItemClickEventArgs e)
        {
            var clicked = e.ClickedItem as ListViewBase;
            var trulyclicked = Interaction.FindParent<ListViewItem>(e.ClickedItem as DependencyObject);
            Debug.WriteLine("Collison Choice Selected");
            if (trulyclicked.Name == "ReplaceAll")
            {
                isReplaceEnabled = true;
                //ItemViewModel.CollisionUIVisibility.isVisible = Visibility.Collapsed;
                GenericFileBrowser.collisionBox.Hide();
                PasteItem_ClickAsync(null, null);
                isReplaceEnabled = false;
                isSkipEnabled = false;
                isReviewEnabled = false;

            }
            else if (trulyclicked.Name == "SkipAll")
            {
                isSkipEnabled = true;
                //ItemViewModel.CollisionUIVisibility.isVisible = Visibility.Collapsed;
                GenericFileBrowser.collisionBox.Hide();
                PasteItem_ClickAsync(null, null);
                isReplaceEnabled = false;
                isSkipEnabled = false;
                isReviewEnabled = false;
            }
            else
            {
                isReviewEnabled = true;
                //ItemViewModel.CollisionUIVisibility.isVisible = Visibility.Collapsed;
                GenericFileBrowser.collisionBox.Hide();
                PasteItem_ClickAsync(null, null);
                isReplaceEnabled = false;
                isSkipEnabled = false;
                isReviewEnabled = false;
            }
        }

        public static async void ReplaceChoiceClick(object sender, ContentDialogButtonClickEventArgs e)
        {
            if (ItemSnapshot.IsOfType(StorageItemTypes.Folder))
            {
                CloneDirectoryAsync(ItemSnapshot.Path, DestinationPathSnapshot, ItemSnapshot.Name, true, ItemSnapshot);
            }
            else if (ItemSnapshot.IsOfType(StorageItemTypes.File))
            {
                StorageFile ClipboardFile = await StorageFile.GetFileFromPathAsync(ItemSnapshot.Path);
                await ClipboardFile.CopyAndReplaceAsync(await StorageFile.GetFileFromPathAsync(DestinationPathSnapshot + @"\" + ItemSnapshot.Name));
            }
            GenericFileBrowser.reviewBox.Hide();
            isLoopPaused = false;
        }

        public static void SkipChoiceClick(object sender, ContentDialogButtonClickEventArgs e)
        {
            GenericFileBrowser.reviewBox.Hide();
            isLoopPaused = false;
        }
    }

    public class PasteState : INotifyPropertyChanged
    {
        public bool _isEnabled;
        public bool isEnabled
        {
            get
            {
                return _isEnabled;
            }

            set
            {
                if (value != _isEnabled)
                {
                    _isEnabled = value;
                    NotifyPropertyChanged("isEnabled");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }

}