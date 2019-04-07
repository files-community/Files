using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Files.Filesystem;
using Files.Navigation;
using Files.Interacts;
using Windows.System;
using Windows.UI.Xaml.Input;
using Windows.UI.Popups;
using System.IO;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using System.Collections.Generic;

namespace Files
{

    public sealed partial class GenericFileBrowser : Page
    {
        public TextBlock textBlock;
        public static DataGrid data;
        public static MenuFlyout context;
        public static MenuFlyout emptySpaceContext;
        public static MenuFlyout HeaderContextMenu;
        public static Page GFBPageName;
        public static ContentDialog AddItemBox;
        public static ContentDialog NameBox;
        public static TextBox inputFromRename;
        public static string inputForRename;
        public static Flyout CopiedFlyout;
        public static Grid grid;


        public GenericFileBrowser()
        {
            this.InitializeComponent();
            GFBPageName = GenericItemView;
            App.ViewModel.TextState.isVisible = Visibility.Collapsed;
            App.ViewModel.PVIS.isVisible = Visibility.Collapsed;
            data = AllView;
            context = RightClickContextMenu;
            HeaderContextMenu = HeaderRightClickMenu;
            Interaction.page = this;
            OpenItem.Click += Interaction.OpenItem_Click;
            ShareItem.Click += Interaction.ShareItem_Click;
            DeleteItem.Click += Interaction.DeleteItem_Click;
            RenameItem.Click += Interaction.RenameItem_Click;
            CutItem.Click += Interaction.CutItem_Click;
            CopyItem.Click += Interaction.CopyItem_ClickAsync;
            AllView.RightTapped += Interaction.AllView_RightTapped;
            //Back.Click += NavigationActions.Back_Click;
            //Forward.Click += NavigationActions.Forward_Click;
            //Refresh.Click += NavigationActions.Refresh_Click;
            //AddItem.Click += AddItem_ClickAsync;
            AllView.DoubleTapped += Interaction.List_ItemClick;
            //Clipboard.ContentChanged += Clipboard_ContentChanged;
            //AddItemBox = AddDialog;
            //NameBox = NameDialog;
            //inputFromRename = RenameInput;
            emptySpaceContext = EmptySpaceFlyout;
            RefreshEmptySpace.Click += NavigationActions.Refresh_Click;
            PasteEmptySpace.Click += Interaction.PasteItem_ClickAsync;
            //CopiedFlyout = CopiedPathFlyout;
            grid = RootGrid;
            //GetPath.Click += Interaction.GetPath_Click;
            //PathBarTip.IsOpen = true;
        }

        

        private async void AddItem_ClickAsync(object sender, RoutedEventArgs e)
        {
            //await AddDialog.ShowAsync();
        }

        private void Clipboard_ContentChanged(object sender, object e)
        {
            try
            {
                DataPackageView packageView = Clipboard.GetContent();
                if (packageView.Contains(StandardDataFormats.StorageItems))
                {
                    Interacts.Interaction.PS.isEnabled = true;
                }
                else
                {
                    Interacts.Interaction.PS.isEnabled = false;
                }
            }
            catch (Exception)
            {
                Interacts.Interaction.PS.isEnabled = false;
            }

        }


        
        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            var parameters = (string)eventArgs.Parameter;
            App.ViewModel.CancelLoadAndClearFiles();
            App.ViewModel.Universal.path = parameters;
            App.ViewModel.AddItemsToCollectionAsync(App.ViewModel.Universal.path, GenericItemView);
            if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)))
            {
                App.PathText.Text = "Desktop";
                //foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                //{
                //    if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "DesktopIC")
                //    {
                //        MainPage.Select.itemSelected = NavItemChoice;
                //        break;
                //    }
                //}
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)))
            {
                App.PathText.Text = "Documents";
                //foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                //{
                //    if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "DocumentsIC")
                //    {
                //        MainPage.Select.itemSelected = NavItemChoice;
                //        break;
                //    }
                //}
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
            {
                App.PathText.Text = "Downloads";
                //foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                //{
                //    if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "DownloadsIC")
                //    {
                //        MainPage.Select.itemSelected = NavItemChoice;
                //        break;
                //    }
                //}
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)))
            {
                App.PathText.Text = "Pictures";
                //foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                //{
                //    if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "PicturesIC")
                //    {
                //        MainPage.Select.itemSelected = NavItemChoice;
                //        break;
                //    }
                //}
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)))
            {
                App.PathText.Text = "Music";
                //foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                //{
                //    if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "MusicIC")
                //    {
                //        MainPage.Select.itemSelected = NavItemChoice;
                //        break;
                //    }
                //}
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
            {
                App.PathText.Text = "OneDrive";
                //foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                //{
                //    if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "OneD_IC")
                //    {
                //        MainPage.Select.itemSelected = NavItemChoice;
                //        break;
                //    }
                //}
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)))
            {
                App.PathText.Text = "Videos";
                //foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                //{
                //    if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "VideosIC")
                //    {
                //        MainPage.Select.itemSelected = NavItemChoice;
                //        break;
                //    }
                //}
            }
            else
            {
                //App.ViewModel.Universal.path = parameters;
                App.PathText.Text = parameters;
                if (parameters.Contains("C:\\") || parameters.Contains("c:\\"))
                {
                    //foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    //{
                    //    if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Tag.ToString() == "LDPage")
                    //    {
                    //        MainPage.Select.itemSelected = NavItemChoice;
                    //        break;
                    //    }
                    //}
                }
                else
                {
                    //foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    //{
                    //    if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Tag.ToString().Contains(parameters.Split("\\")[0]))
                    //    {
                    //        MainPage.Select.itemSelected = NavItemChoice;
                    //        break;
                    //    }
                    //}
                }
            }

        }




        private void AllView_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            
        }

        private async void AllView_DropAsync(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                    foreach (IStorageItem item in await e.DataView.GetStorageItemsAsync())
                    {
                        if (item.IsOfType(StorageItemTypes.Folder))
                        {
                            Interaction.CloneDirectoryAsync((item as StorageFolder).Path, App.ViewModel.Universal.path, (item as StorageFolder).DisplayName);
                        }
                        else
                        {
                            await (item as StorageFile).CopyAsync(await StorageFolder.GetFolderFromPathAsync(App.ViewModel.Universal.path));
                        }
                    }
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            App.ViewModel.PVIS.isVisible = Visibility.Collapsed;
        }

        private async void AllView_CellEditEnded(object sender, DataGridCellEditEndedEventArgs e)
        {
            var newCellText = (data.SelectedItem as ListedItem)?.FileName;
            var selectedItem = App.ViewModel.FilesAndFolders[e.Row.GetIndex()];
            if(selectedItem.FileType == "Folder")
            {
                StorageFolder FolderToRename = await StorageFolder.GetFolderFromPathAsync(selectedItem.FilePath);
                if(FolderToRename.Name != newCellText)
                {
                    await FolderToRename.RenameAsync(newCellText);
                    AllView.CommitEdit();
                }
                else
                {
                    AllView.CancelEdit();
                }
            }
            else
            {
                StorageFile fileToRename = await StorageFile.GetFileFromPathAsync(selectedItem.FilePath);
                if (fileToRename.Name != newCellText)
                {
                    await fileToRename.RenameAsync(newCellText);
                    AllView.CommitEdit();
                }
                else
                {
                    AllView.CancelEdit();
                }
            }
            //Navigation.NavigationActions.Refresh_Click(null, null);
        }

        private void ContentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            //AddDialogFrame.Navigate(typeof(AddItem), new SuppressNavigationTransitionInfo());
        }

        private void GenericItemView_PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            data.SelectedItems.Clear();
        }

        private void AllView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AllView.CommitEdit();
        }

        private void NameDialog_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void NameDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            inputForRename = inputFromRename.Text;
        }

        private void NameDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

        }

        private async void VisiblePath_TextChanged(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                var PathBox = (sender as TextBox);
                var CurrentInput = PathBox.Text;
                if (CurrentInput != App.ViewModel.Universal.path)
                {
                    App.ViewModel.CancelLoadAndClearFiles();

                    if (CurrentInput == "Home" || CurrentInput == "home")
                    {
                        MainPage.accessibleContentFrame.Navigate(typeof(YourHome));
                        MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Recents";
                    }
                    else if (CurrentInput == "Desktop" || CurrentInput == "desktop")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MainPage.DesktopPath);
                        MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Desktop";
                        App.PathText.Text = "Desktop";
                    }
                    else if (CurrentInput == "Documents" || CurrentInput == "documents")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MainPage.DocumentsPath);
                        MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Documents";
                        App.PathText.Text = "Documents";

                    }
                    else if (CurrentInput == "Downloads" || CurrentInput == "downloads")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MainPage.DownloadsPath);
                        MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Downloads";
                        App.PathText.Text = "Downloads";

                    }
                    else if (CurrentInput == "Pictures" || CurrentInput == "pictures")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        MainPage.accessibleContentFrame.Navigate(typeof(PhotoAlbum), MainPage.PicturesPath);
                        MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Pictures";
                        App.PathText.Text = "Pictures";
                    }
                    else if (CurrentInput == "Music" || CurrentInput == "music")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MainPage.MusicPath);
                        MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Music";
                        App.PathText.Text = "Music";

                    }
                    else if (CurrentInput == "Videos" || CurrentInput == "videos")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MainPage.VideosPath);
                        MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Videos";
                        App.PathText.Text = "Videos";

                    }
                    else if (CurrentInput == "OneDrive" || CurrentInput == "Onedrive" || CurrentInput == "onedrive")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MainPage.OneDrivePath);
                        MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search OneDrive";
                        App.PathText.Text = "OneDrive";

                    }
                    else
                    {
                        if (CurrentInput.Contains("."))
                        {
                            if (CurrentInput.Contains(".exe") || CurrentInput.Contains(".EXE"))
                            {
                                if (StorageFile.GetFileFromPathAsync(CurrentInput) != null)
                                {
                                    await Interaction.LaunchExe(CurrentInput);
                                    PathBox.Text = App.ViewModel.Universal.path;
                                }
                                else
                                {
                                    MessageDialog dialog = new MessageDialog("The path typed was not correct. Please try again.", "Invalid Path");
                                    await dialog.ShowAsync();
                                }    
                            }
                            else if (StorageFolder.GetFolderFromPathAsync(CurrentInput) != null)
                            {
                                await StorageFolder.GetFolderFromPathAsync(CurrentInput);
                                App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                                MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), CurrentInput);
                                MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search";
                            }
                            else
                            {
                                try
                                {
                                    await StorageFile.GetFileFromPathAsync(CurrentInput);
                                    StorageFile file = await StorageFile.GetFileFromPathAsync(CurrentInput);
                                    var options = new LauncherOptions
                                    {
                                        DisplayApplicationPicker = false

                                    };
                                    await Launcher.LaunchFileAsync(file, options);
                                    PathBox.Text = App.ViewModel.Universal.path;
                                }
                                catch (ArgumentException)
                                {
                                    MessageDialog dialog = new MessageDialog("The path typed was not correct. Please try again.", "Invalid Path");
                                    await dialog.ShowAsync();
                                }
                                catch (FileNotFoundException)
                                {
                                    MessageDialog dialog = new MessageDialog("The path typed was not correct. Please try again.", "Invalid Path");
                                    await dialog.ShowAsync();
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                await StorageFolder.GetFolderFromPathAsync(CurrentInput);
                                App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                                MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), CurrentInput);
                                MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search";
                            }
                            catch (ArgumentException)
                            {
                                MessageDialog dialog = new MessageDialog("The path typed was not correct. Please try again.", "Invalid Path");
                                await dialog.ShowAsync();
                            }
                            catch (FileNotFoundException)
                            {
                                MessageDialog dialog = new MessageDialog("The path typed was not correct. Please try again.", "Invalid Path");
                                await dialog.ShowAsync();
                            }
                            
                        }

                    }
                }
            }
            
        }

        private void GenericItemView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            GFBPageName.ContextFlyout.ShowAt(GFBPageName);
        }

        private void AllView_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            
        }

        private void AllView_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            args.DragUI.SetContentFromDataPackage();
        }

        private void AllView_DragLeave(object sender, DragEventArgs e)
        {
            
        }
    }

    public class EmptyFolderTextState : INotifyPropertyChanged
    {


        public Visibility _isVisible;
        public Visibility isVisible
        {
            get
            {
                return _isVisible;
            }

            set
            {
                if (value != _isVisible)
                {
                    _isVisible = value;
                    NotifyPropertyChanged("isVisible");
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