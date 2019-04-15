using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.ComponentModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Files.Filesystem;
using Files.Navigation;
using Files.Interacts;
using System.Diagnostics;
using Windows.UI.Core;

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
            AllView.DoubleTapped += Interaction.List_ItemClick;
            Clipboard.ContentChanged += Clipboard_ContentChanged;
            //AddItemBox = AddDialog;
            //NameBox = NameDialog;
            //inputFromRename = RenameInput;
            RefreshEmptySpace.Click += NavigationActions.Refresh_Click;
            PasteEmptySpace.Click += Interaction.PasteItem_ClickAsync;
            //CopiedFlyout = CopiedPathFlyout;
            grid = RootGrid;
            //GetPath.Click += Interaction.GetPath_Click;
            //PathBarTip.IsOpen = true;
        }

        private void SelectAllAcceleratorDG_Invoked(Windows.UI.Xaml.Input.KeyboardAccelerator sender, Windows.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
        {
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
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
            ProHome.BackButton.IsEnabled = ProHome.accessibleContentFrame.CanGoBack;
            ProHome.ForwardButton.IsEnabled = ProHome.accessibleContentFrame.CanGoForward;
            ProHome.RS.isEnabled = true;
            App.AlwaysPresentCommands.isEnabled = true;
            var parameters = (string)eventArgs.Parameter;
            App.ViewModel.CancelLoadAndClearFiles();
            App.ViewModel.Universal.path = parameters;
            ProHome.RefreshButton.Click += NavigationActions.Refresh_Click;
            ProHome.AddItemButton.Click += AddItem_Click;
            App.ViewModel.AddItemsToCollectionAsync(App.ViewModel.Universal.path, GenericItemView);
            Interaction.page = this;
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

            // Reset DataGrid Rows that may be in "cut" command mode
            Interaction.FindChildren<DataGridRow>(Interaction.dataGridRows, GenericFileBrowser.GFBPageName.Content);
            foreach (DataGridRow dataGridRow in Interaction.dataGridRows)
            {
                if (data.Columns[0].GetCellContent(dataGridRow).Opacity < 1)
                {
                    data.Columns[0].GetCellContent(dataGridRow).Opacity = 1;
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
            data.SelectedItem = null;
            App.HomeItems.isEnabled = false;
            App.ShareItems.isEnabled = false;
        }

        private void AllView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            AllView.CommitEdit();
            if(e.AddedItems.Count > 0)
            {
                App.HomeItems.isEnabled = true;
                App.ShareItems.isEnabled = true;

            }
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