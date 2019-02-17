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

namespace Files
{

    public sealed partial class GenericFileBrowser : Page
    {
        public TextBlock textBlock;
        public static DataGrid data;
        public static MenuFlyout context;
        public static MenuFlyout HeaderContextMenu;
        public static Page GFBPageName;
        public static ContentDialog collisionBox;
        public static ContentDialog reviewBox;
        public static ContentDialog AddItemBox;
        public static ContentDialog NameBox;
        public static TextBox inputFromRename;
        public static string inputForRename;



        public GenericFileBrowser()
        {
            this.InitializeComponent();
            GFBPageName = GenericItemView;
            string env = Environment.ExpandEnvironmentVariables("%userprofile%");
            //var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            //CoreTitleBar.ExtendViewIntoTitleBar = false;
            //var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            //titleBar.ButtonBackgroundColor = Color.FromArgb(100, 255, 255, 255);
            //titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 10, 10, 10);
            //titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 10, 10, 10);
            ItemViewModel.TextState.isVisible = Visibility.Collapsed;
            ItemViewModel.PVIS.isVisible = Visibility.Collapsed;
            ItemViewModel.CollisionUIVisibility.isVisible = Visibility.Collapsed;
            ItemViewModel.ConflictUIVisibility.isVisible = Visibility.Collapsed;
            data = AllView;
            context = RightClickContextMenu;
            HeaderContextMenu = HeaderRightClickMenu;
            reviewBox = ReviewBox;
            collisionBox = CollisionBox;
            Interacts.Interaction.page = this;
            OpenItem.Click += Interacts.Interaction.OpenItem_Click;
            ShareItem.Click += Interacts.Interaction.ShareItem_Click;
            DeleteItem.Click += Interacts.Interaction.DeleteItem_Click;
            RenameItem.Click += Interacts.Interaction.RenameItem_Click;
            CutItem.Click += Interacts.Interaction.CutItem_Click;
            CopyItem.Click += Interacts.Interaction.CopyItem_ClickAsync;
            AllView.RightTapped += Interacts.Interaction.AllView_RightTapped;
            Back.Click += Navigation.NavigationActions.Back_Click;
            Forward.Click += Navigation.NavigationActions.Forward_Click;
            Refresh.Click += NavigationActions.Refresh_Click;
            AddItem.Click += AddItem_ClickAsync;
            AllView.DoubleTapped += Interacts.Interaction.List_ItemClick;
            Paste.Click += Interacts.Interaction.PasteItem_ClickAsync;
            Clipboard.ContentChanged += Clipboard_ContentChanged;
            CollisonLV.ItemClick += Interacts.Interaction.CollisionLVItemClick;
            ReviewBox.PrimaryButtonClick += Interacts.Interaction.ReplaceChoiceClick;
            ReviewBox.SecondaryButtonClick += Interacts.Interaction.SkipChoiceClick;
            AddItemBox = AddDialog;
            NameBox = NameDialog;
            inputFromRename = RenameInput;
        }

        

        private async void AddItem_ClickAsync(object sender, RoutedEventArgs e)
        {
            await AddDialog.ShowAsync();
            //var CurrentView = ApplicationView.GetForCurrentView();
            //CoreApplicationView NewView = CoreApplication.CreateNewView();

            //await NewView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            //{
            //    var newWindow = Window.Current;
            //    var newAppView = ApplicationView.GetForCurrentView();
            //    Frame frame = new Frame();
            //    frame.Navigate(typeof(AddItem), null);
            //    Window.Current.Content = frame;
            //    Window.Current.Activate();
            //    await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newAppView.Id, ViewSizePreference.Default, CurrentView.Id, ViewSizePreference.Default);

            //});
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

        public static UniversalPath p = new UniversalPath();
        public static UniversalPath P { get { return p; } }
        
        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            var parameters = (string)eventArgs.Parameter;
            ItemViewModel.FilesAndFolders.Clear();
            ItemViewModel.ViewModel = new ItemViewModel(parameters, GFBPageName);
            if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)))
            {
                P.path = "Desktop";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)))
            {
                P.path = "Documents";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
            {
                P.path = "Downloads";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)))
            {
                P.path = "Pictures";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)))
            {
                P.path = "Music";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
            {
                P.path = "OneDrive";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)))
            {
                P.path = "Videos";
            }
            else
            {
                P.path = parameters;
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
                var items = await e.DataView.GetStorageItemsAsync();
                if(items.Count() == 1)
                {
                    DataPackage data = new DataPackage();
                    foreach(IStorageItem storageItem in items)
                    {
                        var itemPath = storageItem.Path;

                    } 
                }
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            ItemViewModel.PVIS.isVisible = Visibility.Collapsed;
        }

        private async void AllView_CellEditEnded(object sender, DataGridCellEditEndedEventArgs e)
        {
            var newCellText = (GenericFileBrowser.data.SelectedItem as ListedItem)?.FileName;
            var selectedItem = ItemViewModel.FilesAndFolders[e.Row.GetIndex()];
            if(selectedItem.FileExtension == "Folder")
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
            AddDialogFrame.Navigate(typeof(AddItem), new SuppressNavigationTransitionInfo());
        }

        private void GenericItemView_PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            data.CommitEdit();
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