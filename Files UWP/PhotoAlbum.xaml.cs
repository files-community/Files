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
using Windows.ApplicationModel.DataTransfer;
using Files.Navigation;
using System.Diagnostics;
using Windows.UI.Xaml.Media.Animation;

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
            gridContext = GridRightClickContextMenu;
            Clipboard.ContentChanged += Clipboard_ContentChanged;
        }



        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            ItemViewModel.GetCurrentSelectedTabInstance<ProHome>().BackButton.IsEnabled = ItemViewModel.GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.CanGoBack;
            ItemViewModel.GetCurrentSelectedTabInstance<ProHome>().ForwardButton.IsEnabled = ItemViewModel.GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.CanGoForward;
            ItemViewModel.GetCurrentSelectedTabInstance<ProHome>().RefreshButton.IsEnabled = true;
            App.AlwaysPresentCommands.isEnabled = true;
            var parameters = eventArgs.Parameter.ToString();
            App.ViewModel.AddItemsToCollectionAsync(parameters, PhotoAlbumViewer);
            Interaction.page = this;
            FileList.DoubleTapped += Interaction.List_ItemClick;

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

            if (Clipboard.GetContent().Contains(StandardDataFormats.StorageItems))
            {
                Interaction.PS.isEnabled = true;
            }
            else
            {
                Interaction.PS.isEnabled = false;
            }
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
            App.HomeItems.isEnabled = false;
            App.ShareItems.isEnabled = false;
        }

        private void PhotoAlbumViewer_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            gridContext.ShowAt(sender as Grid, e.GetPosition(sender as Grid));
        }

        private void NameDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            inputForRename = inputFromRename.Text;
        }

        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            Interaction.OpenItem_Click(null, null);
        }

        private void ShareItem_Click(object sender, RoutedEventArgs e)
        {
            Interaction.ShareItem_Click(null, null);
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            Interaction.DeleteItem_Click(null, null);
        }

        private void RenameItem_Click(object sender, RoutedEventArgs e)
        {
            Interaction.RenameItem_Click(null, null);
        }

        private void CutItem_Click(object sender, RoutedEventArgs e)
        {
            Interaction.CutItem_Click(null, null);
        }

        private void CopyItem_Click(object sender, RoutedEventArgs e)
        {
            Interaction.CopyItem_ClickAsync(null, null);
        }

        private async void PropertiesItem_Click(object sender, RoutedEventArgs e)
        {
            ItemViewModel.GetCurrentSelectedTabInstance<ProHome>().accessiblePropertiesFrame.Navigate(typeof(Properties), (PhotoAlbum.gv.SelectedItem as ListedItem).FilePath, new SuppressNavigationTransitionInfo());
            await ItemViewModel.GetCurrentSelectedTabInstance<ProHome>().propertiesBox.ShowAsync();
            
        }

        private void RefreshGrid_Click(object sender, RoutedEventArgs e)
        {
            NavigationActions.Refresh_Click(null, null);
        }

        private void PasteGrid_Click(object sender, RoutedEventArgs e)
        {
            Interaction.PasteItem_ClickAsync(null, null);
        }

        private async void PropertiesItemGrid_Click(object sender, RoutedEventArgs e)
        {
            ItemViewModel.GetCurrentSelectedTabInstance<ProHome>().accessiblePropertiesFrame.Navigate(typeof(Properties), App.PathText.Text, new SuppressNavigationTransitionInfo());
            await ItemViewModel.GetCurrentSelectedTabInstance<ProHome>().propertiesBox.ShowAsync();
        }
    }
}
