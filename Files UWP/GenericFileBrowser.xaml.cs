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
using System.Text.RegularExpressions;
using System.IO;
using Windows.System;
using Windows.UI.Xaml.Input;

namespace Files
{
    public sealed partial class GenericFileBrowser : Page
    {
        public TextBlock emptyTextGFB;
        public TextBlock textBlock;
        public DataGrid data;
        public MenuFlyout context;
        public MenuFlyout emptySpaceContext;
        public MenuFlyout HeaderContextMenu;
        public Page GFBPageName;
        public ContentDialog AddItemBox;
        public ContentDialog NameBox;
        public TextBox inputFromRename;
        public string inputForRename;
        public Flyout CopiedFlyout;
        public Grid grid;
        public ProgressBar progressBar;
        ItemViewModel viewModelInstance;
        ProHome tabInstance;

        public EmptyFolderTextState TextState { get; set; } = new EmptyFolderTextState();

        public GenericFileBrowser()
        {
            this.InitializeComponent();
            GFBPageName = GenericItemView;
            emptyTextGFB = EmptyText;
            progressBar = progBar;
            progressBar.Visibility = Visibility.Collapsed;
            data = AllView;
            context = RightClickContextMenu;
            HeaderContextMenu = HeaderRightClickMenu;
            grid = RootGrid;
            Clipboard.ContentChanged += Clipboard_ContentChanged;
            RefreshEmptySpace.Click += NavigationActions.Refresh_Click;
            Frame rootFrame = Window.Current.Content as Frame;
            InstanceTabsView instanceTabsView = rootFrame.Content as InstanceTabsView;
            instanceTabsView.TabStrip_SelectionChanged(null, null);
            tabInstance = App.selectedTabInstance;
            if (tabInstance.instanceViewModel == null && tabInstance.instanceInteraction == null)
            {
                tabInstance.instanceViewModel = new ItemViewModel();
                tabInstance.instanceInteraction = new Interaction();
            }
            viewModelInstance = tabInstance.instanceViewModel;
            PasteEmptySpace.Click += tabInstance.instanceInteraction.PasteItem_ClickAsync;
            OpenItem.Click += tabInstance.instanceInteraction.OpenItem_Click;
            ShareItem.Click += tabInstance.instanceInteraction.ShareItem_Click;
            DeleteItem.Click += tabInstance.instanceInteraction.DeleteItem_Click;
            RenameItem.Click += tabInstance.instanceInteraction.RenameItem_Click;
            CutItem.Click += tabInstance.instanceInteraction.CutItem_Click;
            CopyItem.Click += tabInstance.instanceInteraction.CopyItem_ClickAsync;
            SidebarPinItem.Click += tabInstance.instanceInteraction.PinItem_Click;
            OpenInNewTab.Click += tabInstance.instanceInteraction.OpenDirectoryInNewTab_Click;
            AllView.RightTapped += tabInstance.instanceInteraction.AllView_RightTapped;
            AllView.DoubleTapped += tabInstance.instanceInteraction.List_ItemClick;
            OpenTerminal.Click += tabInstance.instanceInteraction.OpenDirectoryInTerminal;
            NewFolder.Click += tabInstance.instanceInteraction.NewFolder_Click;
            NewBitmapImage.Click += tabInstance.instanceInteraction.NewBitmapImage_Click;
            NewTextDocument.Click += tabInstance.instanceInteraction.NewTextDocument_Click;
            PropertiesItem.Click += tabInstance.ShowPropertiesButton_Click;
            OpenInNewWindowItem.Click += tabInstance.instanceInteraction.OpenInNewWindowItem_Click;
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

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            
            tabInstance.BackButton.IsEnabled = tabInstance.accessibleContentFrame.CanGoBack;
            tabInstance.ForwardButton.IsEnabled = tabInstance.accessibleContentFrame.CanGoForward;
            tabInstance.RefreshButton.IsEnabled = true;
            var parameters = (string)eventArgs.Parameter;
            tabInstance.instanceViewModel.Universal.path = parameters;

            if (tabInstance.instanceViewModel.Universal.path == Path.GetPathRoot(tabInstance.instanceViewModel.Universal.path))
            {
                tabInstance.UpButton.IsEnabled = false;
            }
            else
            {
                tabInstance.UpButton.IsEnabled = true;
            }

            Clipboard_ContentChanged(null, null);
            tabInstance.AlwaysPresentCommands.isEnabled = true;
            tabInstance.AddItemButton.Click += AddItem_Click;

            TextState.isVisible = Visibility.Collapsed;

            tabInstance.instanceViewModel.AddItemsToCollectionAsync(tabInstance.instanceViewModel.Universal.path);
            if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)))
            {
                tabInstance.PathText.Text = "Desktop";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)))
            {
                tabInstance.PathText.Text = "Documents";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
            {
                tabInstance.PathText.Text = "Downloads";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)))
            {
                tabInstance.PathText.Text = "Pictures";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)))
            {
                tabInstance.PathText.Text = "Music";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
            {
                tabInstance.PathText.Text = "OneDrive";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)))
            {
                tabInstance.PathText.Text = "Videos";
            }
            else
            {
                if (parameters.Equals(@"C:\") || parameters.Equals(@"c:\"))
                {
                    tabInstance.PathText.Text = @"Local Disk (C:\)";
                }
                else
                {
                    tabInstance.PathText.Text = parameters;

                }
            }

            // Reset DataGrid Rows that may be in "cut" command mode
            tabInstance.instanceInteraction.dataGridRows.Clear();
            Interaction.FindChildren<DataGridRow>(tabInstance.instanceInteraction.dataGridRows, (tabInstance.accessibleContentFrame.Content as GenericFileBrowser).GFBPageName.Content);
            foreach (DataGridRow dataGridRow in tabInstance.instanceInteraction.dataGridRows)
            {
                if (data.Columns[0].GetCellContent(dataGridRow).Opacity < 1)
                {
                    data.Columns[0].GetCellContent(dataGridRow).Opacity = 1;
                }
            }
            
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            if(tabInstance.instanceViewModel._fileQueryResult != null)
            {
                tabInstance.instanceViewModel._fileQueryResult.ContentsChanged -= tabInstance.instanceViewModel.FileContentsChanged;
            }

            //this.Bindings.StopTracking();
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
                            tabInstance.instanceInteraction.CloneDirectoryAsync((item as StorageFolder).Path, tabInstance.instanceViewModel.Universal.path, (item as StorageFolder).DisplayName);
                        }
                        else
                        {
                            await (item as StorageFile).CopyAsync(await StorageFolder.GetFolderFromPathAsync(tabInstance.instanceViewModel.Universal.path));
                        }
                    }
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            this.progressBar.Visibility = Visibility.Collapsed;
        }

        private async void AllView_CellEditEnded(object sender, DataGridCellEditEndedEventArgs e)
        {
            var newCellText = (data.SelectedItem as ListedItem)?.FileName;
            var selectedItem = tabInstance.instanceViewModel.FilesAndFolders[e.Row.GetIndex()];
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
            tabInstance.HomeItems.isEnabled = false;
            tabInstance.ShareItems.isEnabled = false;
        }

        private void AllView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AllView.CommitEdit();
            if(e.AddedItems.Count > 0)
            {
                tabInstance.HomeItems.isEnabled = true;
                tabInstance.ShareItems.isEnabled = true;

            }
            else if(data.SelectedItems.Count == 0)
            {
                tabInstance.HomeItems.isEnabled = false;
                tabInstance.ShareItems.isEnabled = false;
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

        private void RightClickContextMenu_Opened(object sender, object e)
        {
            var selectedDataItem = tabInstance.instanceViewModel.FilesAndFolders[AllView.SelectedIndex];
            if (selectedDataItem.FileType != "Folder" || AllView.SelectedItems.Count > 1)
            {
                SidebarPinItem.Visibility = Visibility.Collapsed;
                OpenInNewTab.Visibility = Visibility.Collapsed;
                OpenInNewWindowItem.Visibility = Visibility.Collapsed;
            }
            else if (selectedDataItem.FileType == "Folder")
            {
                SidebarPinItem.Visibility = Visibility.Visible;
                OpenInNewTab.Visibility = Visibility.Visible;
                OpenInNewWindowItem.Visibility = Visibility.Visible;
            }
        }

        private void AllView_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                tabInstance.instanceInteraction.List_ItemClick(null, null);
                e.Handled = true;
            }
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