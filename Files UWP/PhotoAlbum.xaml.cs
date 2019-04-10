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
            //ProHome.BackButton.Click += Navigation.PhotoAlbumNavActions.Back_Click;
            //ProHome.ForwardButton.Click += Navigation.PhotoAlbumNavActions.Forward_Click;
            ProHome.RefreshButton.Click += Navigation.PhotoAlbumNavActions.Refresh_Click;
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

        private void NameDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            inputForRename = inputFromRename.Text;
        }
    }
}
