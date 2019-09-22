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
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Xaml.Interactions.Core;
using Microsoft.Xaml.Interactivity;
using System.Text.RegularExpressions;
using Interaction = Files.Interacts.Interaction;

namespace Files
{

    public sealed partial class PhotoAlbum : Page
    {
        public GridView gv;
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
        ItemViewModel viewModelInstance;
        ProHome tabInstance;
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
            tabInstance = App.selectedTabInstance;
            viewModelInstance = tabInstance.instanceViewModel;
            FileList.DoubleTapped += tabInstance.instanceInteraction.List_ItemClick;
            SidebarPinItem.Click += tabInstance.instanceInteraction.PinItem_Click;
            OpenTerminal.Click += tabInstance.instanceInteraction.OpenDirectoryInTerminal;

        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            var CurrentInstance = tabInstance;
            CurrentInstance.BackButton.IsEnabled = CurrentInstance.accessibleContentFrame.CanGoBack;
            CurrentInstance.ForwardButton.IsEnabled = CurrentInstance.accessibleContentFrame.CanGoForward;
            CurrentInstance.RefreshButton.IsEnabled = true;
            var parameters = eventArgs.Parameter.ToString();
            tabInstance.instanceViewModel.Universal.path = parameters;

            if (tabInstance.instanceViewModel.Universal.path == Path.GetPathRoot(tabInstance.instanceViewModel.Universal.path))
            {
                CurrentInstance.UpButton.IsEnabled = false;
            }
            else
            {
                CurrentInstance.UpButton.IsEnabled = true;
            }

            tabInstance.AlwaysPresentCommands.isEnabled = true;

            TextState.isVisible = Visibility.Collapsed;
            tabInstance.instanceViewModel.CancelLoadAndClearFiles();

            tabInstance.instanceViewModel.AddItemsToCollectionAsync(parameters);

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
                if (parameters.Equals(@"C:\") || parameters.Equals(@"c:\"))
                {
                    CurrentInstance.PathText.Text = @"Local Disk (C:\)";
                }
                else
                {
                    CurrentInstance.PathText.Text = parameters;

                }

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

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            if (tabInstance.instanceViewModel._fileQueryResult != null)
            {
                tabInstance.instanceViewModel._fileQueryResult.ContentsChanged -= tabInstance.instanceViewModel.FileContentsChanged;
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

            var BoxPressed = Interaction.FindParent<GridViewItem>(e.OriginalSource as DependencyObject);
            if (BoxPressed == null)
            {
                gv.SelectedItems.Clear();
            }
        }

        private void PhotoAlbumViewer_PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            
            
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
            tabInstance.instanceInteraction.OpenItem_Click(null, null);
        }

        private void ShareItem_Click(object sender, RoutedEventArgs e)
        {
            tabInstance.instanceInteraction.ShareItem_Click(null, null);
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            tabInstance.instanceInteraction.DeleteItem_Click(null, null);
        }

        private void RenameItem_Click(object sender, RoutedEventArgs e)
        {
            tabInstance.instanceInteraction.RenameItem_Click(null, null);
        }

        private void CutItem_Click(object sender, RoutedEventArgs e)
        {
            tabInstance.instanceInteraction.CutItem_Click(null, null);
        }

        private void CopyItem_Click(object sender, RoutedEventArgs e)
        {
            tabInstance.instanceInteraction.CopyItem_ClickAsync(null, null);
        }

        private void PropertiesItem_Click(object sender, RoutedEventArgs e)
        {
            var CurrentInstance = tabInstance;
            CurrentInstance.ShowPropertiesButton_Click(null, null);
        }

        private void RefreshGrid_Click(object sender, RoutedEventArgs e)
        {
            NavigationActions.Refresh_Click(null, null);
        }

        private void PasteGrid_Click(object sender, RoutedEventArgs e)
        {
            tabInstance.instanceInteraction.PasteItem_ClickAsync(null, null);
        }

        private async void PropertiesItemGrid_Click(object sender, RoutedEventArgs e)
        {
            tabInstance.accessiblePropertiesFrame.Navigate(typeof(Properties), tabInstance.PathText.Text, new SuppressNavigationTransitionInfo());
            await tabInstance.propertiesDialog.ShowAsync();
        }

        private void StackPanel_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var parentContainer = Interaction.FindParent<GridViewItem>(e.OriginalSource as DependencyObject);
            foreach (ListedItem listedItem in FileList.SelectedItems)
            {
                if (FileList.IndexFromContainer(parentContainer) == listedItem.RowIndex)
                {
                    return;
                }
            }
            // The following code is only reachable when a user RightTapped an unselected row
            FileList.SelectedItems.Clear();
            FileList.SelectedItems.Add(FileList.ItemFromContainer(parentContainer) as ListedItem);
        }

        private void PhotoAlbumViewer_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(sender as Page).Properties.IsLeftButtonPressed)
            {
                FileList.SelectedItem = null;
                tabInstance.HomeItems.isEnabled = false;
                tabInstance.ShareItems.isEnabled = false;
            }
        }

        private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                tabInstance.HomeItems.isEnabled = true;
                tabInstance.ShareItems.isEnabled = true;

            }
            else if (FileList.SelectedItems.Count == 0)
            {
                tabInstance.HomeItems.isEnabled = false;
                tabInstance.ShareItems.isEnabled = false;
            }
        }

        private void RightClickContextMenu_Opened(object sender, object e)
        {
            var selectedDataItem = tabInstance.instanceViewModel.FilesAndFolders[gv.SelectedIndex];
            if (selectedDataItem.FileType != "Folder" || gv.SelectedItems.Count > 1)
            {
                SidebarPinItem.IsEnabled = false;
            }
            else if (selectedDataItem.FileType == "Folder")
            {
                SidebarPinItem.IsEnabled = true;
            }
        }
    }
}
