using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Files.Filesystem;
using Files.Interacts;


namespace Files
{

    public sealed partial class PhotoAlbum : Page
    {
        public static GridView gv;
        public static Image largeImg;
        public static MenuFlyout context;
        public static MenuFlyout gridContext;
        public static Page PAPageName;

        public PhotoAlbum()
        {
            this.InitializeComponent();
            PAPageName = PhotoAlbumViewer;
            gv = FileList;
            context = RightClickContextMenu;
            gridContext = GridRightClickContextMenu;
        }



        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            var parameters = eventArgs.Parameter.ToString();
            ItemViewModel.ViewModel = new ItemViewModel(parameters, PhotoAlbumViewer);
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
                GenericFileBrowser.P.path = "Desktop";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)))
            {
                GenericFileBrowser.P.path = "Documents";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
            {
                GenericFileBrowser.P.path = "Downloads";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)))
            {
                GenericFileBrowser.P.path = "Pictures";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)))
            {
                GenericFileBrowser.P.path = "Music";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
            {
                GenericFileBrowser.P.path = "OneDrive";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)))
            {
                GenericFileBrowser.P.path = "Videos";
            }
            else
            {
                GenericFileBrowser.P.path = parameters;
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
    }
}
