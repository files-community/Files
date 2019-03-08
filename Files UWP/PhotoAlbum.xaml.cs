using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Files.Filesystem;
using Files.Interacts;
using Windows.UI.Xaml.Input;
using Windows.UI.Popups;
using System.IO;
using Windows.Storage;
using Windows.System;
using Microsoft.Toolkit.Uwp.UI.Controls;

namespace Files
{

    public sealed partial class PhotoAlbum : Page
    {
        public static AdaptiveGridView gv;
        public static Image largeImg;
        public static MenuFlyout context;
        public static MenuFlyout gridContext;
        public static Page PAPageName;
        public static ContentDialog AddItemBox;
        public static ContentDialog NameBox;
        public static TextBox inputFromRename;
        public static string inputForRename;

        public PhotoAlbum()
        {
            this.InitializeComponent();
            PAPageName = PhotoAlbumViewer;
            gv = FileList;
            context = RightClickContextMenu;
            gridContext = GridRightClickContextMenu;
            AddItemBox = AddDialog;
            NameBox = NameDialog;
            inputFromRename = RenameInput;
            ShareItem.Click += Interaction.ShareItem_Click;
            RenameItem.Click += Interaction.RenameItem_Click;
        }



        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            var parameters = eventArgs.Parameter.ToString();
            App.ViewModel.AddItemsToCollectionAsync(parameters, PhotoAlbumViewer);
            Interaction.page = this;
            FileList.DoubleTapped += Interaction.List_ItemClick;
            Back.Click += Navigation.PhotoAlbumNavActions.Back_Click;
            Forward.Click += Navigation.PhotoAlbumNavActions.Forward_Click;
            Refresh.Click += Navigation.PhotoAlbumNavActions.Refresh_Click;
            FileList.RightTapped += Interaction.FileList_RightTapped;
            OpenItem.Click += Interaction.OpenItem_Click;
            CopyItem.Click += Interaction.CopyItem_ClickAsync;
            RefreshGrid.Click += Navigation.PhotoAlbumNavActions.Refresh_Click;
            DeleteItem.Click += Interaction.DeleteItem_Click;


            if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)))
            {
               App.PathText.Text = "Desktop";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)))
            {
               App.PathText.Text = "Documents";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
            {
               App.PathText.Text = "Downloads";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)))
            {
               App.PathText.Text = "Pictures";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)))
            {
               App.PathText.Text = "Music";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
            {
               App.PathText.Text = "OneDrive";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)))
            {
               App.PathText.Text = "Videos";
            }
            else
            {
               App.PathText.Text = parameters;
            }


        }

        private void Grid_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            var ObjectPressed = (sender as Grid).DataContext as ListedItem;
            gv.SelectedItem = ObjectPressed;
            context.ShowAt(sender as Grid, e.GetPosition(sender as Grid));
        }

        private void FileList_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var BoxPressed = Interaction.FindParent<GridViewItem>(e.OriginalSource as DependencyObject);
            if (BoxPressed == null)
            {
                gv.SelectedItems.Clear();
            }
        }

        private void PhotoAlbumViewer_PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            FileList.SelectedItem = null;
        }

        private void PhotoAlbumViewer_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            gridContext.ShowAt(sender as Grid, e.GetPosition(sender as Grid));
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
                        PathBox.Text = "Desktop";
                    }
                    else if (CurrentInput == "Documents" || CurrentInput == "documents")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MainPage.DocumentsPath);
                        MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Documents";
                        PathBox.Text = "Documents";

                    }
                    else if (CurrentInput == "Downloads" || CurrentInput == "downloads")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MainPage.DownloadsPath);
                        MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Downloads";
                        PathBox.Text = "Downloads";

                    }
                    else if (CurrentInput == "Pictures" || CurrentInput == "pictures")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        MainPage.accessibleContentFrame.Navigate(typeof(PhotoAlbum), MainPage.PicturesPath);
                        MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Pictures";

                    }
                    else if (CurrentInput == "Music" || CurrentInput == "music")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MainPage.MusicPath);
                        MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Music";
                        PathBox.Text = "Music";

                    }
                    else if (CurrentInput == "Videos" || CurrentInput == "videos")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MainPage.VideosPath);
                        MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Videos";
                        PathBox.Text = "Videos";

                    }
                    else if (CurrentInput == "OneDrive" || CurrentInput == "Onedrive" || CurrentInput == "onedrive")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MainPage.OneDrivePath);
                        MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search OneDrive";
                        PathBox.Text = "OneDrive";

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
                                    PathBox.Text =App.PathText.Text;
                                }
                                else
                                {
                                    MessageDialog dialog = new MessageDialog("The path typed was not correct. Please try again.", "Invalid Path");
                                    await dialog.ShowAsync();
                                }
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
                                    PathBox.Text =App.PathText.Text;
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

        private void NameDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            inputForRename = inputFromRename.Text;
        }
    }
}
