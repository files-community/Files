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
using System.ComponentModel;

namespace Files
{

    public sealed partial class PhotoAlbum : Page
    {
        public AdaptiveGridView gv;
        public Image largeImg;
        public MenuFlyout context;
        public MenuFlyout gridContext;
        public Page PAPageName;
        public ContentDialog AddItemBox;
        public ContentDialog NameBox;
        public TextBox inputFromRename;
        public TextBlock EmptyTextPA;
        public string inputForRename;
        public ProgressBar progressBar;
        public ItemViewModel<PhotoAlbum> instanceViewModel;
        public Interaction<PhotoAlbum> instanceInteraction;
        public EmptyFolderTextState TextState { get; set; } = new EmptyFolderTextState();


        public PhotoAlbum()
        {
            this.InitializeComponent();
            EmptyTextPA = EmptyText;
            PAPageName = PhotoAlbumViewer;
            gv = FileList;
            progressBar = ProgBar;
            gridContext = GridRightClickContextMenu;
            Clipboard.ContentChanged += Clipboard_ContentChanged;
            instanceViewModel = new ItemViewModel<PhotoAlbum>(this, null);
            instanceInteraction = new Interaction<PhotoAlbum>(this);
            gv.ItemsSource = instanceViewModel.FilesAndFolders;
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            var CurrentInstance = ItemViewModel<PhotoAlbum>.GetCurrentSelectedTabInstance<ProHome>();
            CurrentInstance.BackButton.IsEnabled = CurrentInstance.accessibleContentFrame.CanGoBack;
            CurrentInstance.ForwardButton.IsEnabled = CurrentInstance.accessibleContentFrame.CanGoForward;
            CurrentInstance.RefreshButton.IsEnabled = true;
            instanceViewModel.AlwaysPresentCommands.isEnabled = true;
            var parameters = eventArgs.Parameter.ToString();
            instanceViewModel.AddItemsToCollectionAsync(parameters, this);
            TextState_PropertyChanged(null, null);
            FileList.DoubleTapped += instanceInteraction.List_ItemClick;

            if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)))
            {
               CurrentInstance.PathText.Text = "Desktop";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)))
            {
               CurrentInstance.PathText.Text = "Documents";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
            {
               CurrentInstance.PathText.Text = "Downloads";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)))
            {
               CurrentInstance.PathText.Text = "Pictures";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)))
            {
               CurrentInstance.PathText.Text = "Music";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
            {
               CurrentInstance.PathText.Text = "OneDrive";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)))
            {
               CurrentInstance.PathText.Text = "Videos";
            }
            else
            {
               CurrentInstance.PathText.Text = parameters;
            }

            if (Clipboard.GetContent().Contains(StandardDataFormats.StorageItems))
            {
                App.PS.isEnabled = true;
            }
            else
            {
                App.PS.isEnabled = false;
            }
        }

        private void Clipboard_ContentChanged(object sender, object e)
        {
            try
            {
                DataPackageView packageView = Clipboard.GetContent();
                if (packageView.Contains(StandardDataFormats.StorageItems))
                {
                    App.PS.isEnabled = true;
                }
                else
                {
                   App.PS.isEnabled = false;
                }
            }
            catch (Exception)
            {
                App.PS.isEnabled = false;
            }

        }


        private void FileList_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {

            var BoxPressed = Interaction<PhotoAlbum>.FindParent<GridViewItem>(e.OriginalSource as DependencyObject);
            if (BoxPressed == null)
            {
                gv.SelectedItems.Clear();
            }
        }

        private void PhotoAlbumViewer_PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            FileList.SelectedItem = null;
            instanceViewModel.HomeItems.isEnabled = false;
            instanceViewModel.ShareItems.isEnabled = false;
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
            instanceInteraction.OpenItem_Click(null, null);
        }

        private void ShareItem_Click(object sender, RoutedEventArgs e)
        {
            instanceInteraction.ShareItem_Click(null, null);
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            instanceInteraction.DeleteItem_Click(null, null);
        }

        private void RenameItem_Click(object sender, RoutedEventArgs e)
        {
            instanceInteraction.RenameItem_Click(null, null);
        }

        private void CutItem_Click(object sender, RoutedEventArgs e)
        {
            instanceInteraction.CutItem_Click(null, null);
        }

        private void CopyItem_Click(object sender, RoutedEventArgs e)
        {
            instanceInteraction.CopyItem_ClickAsync(null, null);
        }

        private async void PropertiesItem_Click(object sender, RoutedEventArgs e)
        {
            ItemViewModel<PhotoAlbum>.GetCurrentSelectedTabInstance<ProHome>().accessiblePropertiesFrame.Navigate(typeof(Properties), (this.gv.SelectedItem as ListedItem).FilePath, new SuppressNavigationTransitionInfo());
            await ItemViewModel<PhotoAlbum>.GetCurrentSelectedTabInstance<ProHome>().propertiesBox.ShowAsync();
            
        }

        private void RefreshGrid_Click(object sender, RoutedEventArgs e)
        {
            NavigationActions.Refresh_Click(null, null);
        }

        private void PasteGrid_Click(object sender, RoutedEventArgs e)
        {
            instanceInteraction.PasteItem_ClickAsync(null, null);
        }

        private async void PropertiesItemGrid_Click(object sender, RoutedEventArgs e)
        {
            ItemViewModel<PhotoAlbum>.GetCurrentSelectedTabInstance<ProHome>().accessiblePropertiesFrame.Navigate(typeof(Properties), ItemViewModel<PhotoAlbum>.GetCurrentSelectedTabInstance<ProHome>().PathText.Text, new SuppressNavigationTransitionInfo());
            await ItemViewModel<PhotoAlbum>.GetCurrentSelectedTabInstance<ProHome>().propertiesBox.ShowAsync();
        }

        internal void TextState_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            TextState.isVisible = ItemViewModel<PhotoAlbum>.GetCurrentSelectedTabInstance<ProHome>().TextState.isVisible;
        }
    }
}
