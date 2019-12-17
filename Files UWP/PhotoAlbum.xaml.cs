using Files.Dialogs;
using Files.Enums;
using Files.Filesystem;
using Files.Navigation;
using Interaction = Files.Interacts.Interaction;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Xaml.Interactions.Core;
using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

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
        public ListedItem renamingItem;
        private bool isRenaming = false;
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
            FileList.DoubleTapped += tabInstance.instanceInteraction.List_ItemClick;
            SidebarPinItem.Click += tabInstance.instanceInteraction.PinItem_Click;
            OpenTerminal.Click += tabInstance.instanceInteraction.OpenDirectoryInTerminal;
            OpenInNewWindowItem.Click += tabInstance.instanceInteraction.OpenInNewWindowItem_Click;
            OpenInNewTab.Click += tabInstance.instanceInteraction.OpenDirectoryInNewTab_Click;
            NewFolder.Click += tabInstance.instanceInteraction.NewFolder_Click;
            NewBitmapImage.Click += tabInstance.instanceInteraction.NewBitmapImage_Click;
            NewTextDocument.Click += tabInstance.instanceInteraction.NewTextDocument_Click;
            UnzipItem.Click += tabInstance.instanceInteraction.ExtractItems_Click;

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

            // Add item jumping handler
            Window.Current.CoreWindow.CharacterReceived += Page_CharacterReceived;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            if (tabInstance.instanceViewModel._fileQueryResult != null)
            {
                tabInstance.instanceViewModel._fileQueryResult.ContentsChanged -= tabInstance.instanceViewModel.FileContentsChanged;
            }

            // Remove item jumping handler
            Window.Current.CoreWindow.CharacterReceived -= Page_CharacterReceived;
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
            if (FileList.SelectedItems.Contains(FileList.ItemFromContainer(parentContainer)))
            {
                return;
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
            var selectedDataItem = gv.SelectedItem as ListedItem;

            // Search selected items for non-Folders
            if (gv.SelectedItems.Cast<ListedItem>().Any(x => x.FileType != "Folder"))
            {
                SidebarPinItem.Visibility = Visibility.Collapsed;
                OpenInNewTab.Visibility = Visibility.Collapsed;
                OpenInNewWindowItem.Visibility = Visibility.Collapsed;
                if (gv.SelectedItems.Count == 1)
                {
                    if (selectedDataItem.DotFileExtension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        OpenItem.Visibility = Visibility.Collapsed;
                        UnzipItem.Visibility = Visibility.Collapsed;
                    }
                    else if (!selectedDataItem.DotFileExtension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        OpenItem.Visibility = Visibility.Visible;
                        UnzipItem.Visibility = Visibility.Collapsed;
                    }
                }
                else if (gv.SelectedItems.Count > 1)
                {
                    OpenItem.Visibility = Visibility.Collapsed;
                    UnzipItem.Visibility = Visibility.Collapsed;
                }
            }
            else     // All are Folders
            {
                OpenItem.Visibility = Visibility.Collapsed;
                if (gv.SelectedItems.Count <= 5 && gv.SelectedItems.Count > 0)
                {
                    SidebarPinItem.Visibility = Visibility.Visible;
                    OpenInNewTab.Visibility = Visibility.Visible;
                    OpenInNewWindowItem.Visibility = Visibility.Visible;
                    UnzipItem.Visibility = Visibility.Collapsed;
                }
                else if (gv.SelectedItems.Count > 5)
                {
                    SidebarPinItem.Visibility = Visibility.Visible;
                    OpenInNewTab.Visibility = Visibility.Collapsed;
                    OpenInNewWindowItem.Visibility = Visibility.Collapsed;
                    UnzipItem.Visibility = Visibility.Collapsed;
                }

            }
        }

        public void StartRename()
        {
            renamingItem = gv.SelectedItem as ListedItem;
            GridViewItem gridViewItem = gv.ContainerFromItem(renamingItem) as GridViewItem;
            StackPanel stackPanel = (gridViewItem.ContentTemplateRoot as Grid).Children[1] as StackPanel;
            TextBlock textBlock = stackPanel.Children[0] as TextBlock;
            TextBox textBox = stackPanel.Children[1] as TextBox;
            int extensionLength = renamingItem.DotFileExtension?.Length ?? 0;

            textBlock.Visibility = Visibility.Collapsed;
            textBox.Visibility = Visibility.Visible;
            textBox.Focus(FocusState.Pointer);
            textBox.LostFocus += RenameTextBox_LostFocus;
            textBox.KeyDown += RenameTextBox_KeyDown;
            textBox.Select(0, renamingItem.FileName.Length - extensionLength);
            isRenaming = true;
        }

        private void RenameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                TextBox textBox = sender as TextBox;
                textBox.LostFocus -= RenameTextBox_LostFocus;
                EndRename(textBox);
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Enter)
            {
                TextBox textBox = sender as TextBox;
                CommitRename(textBox);
                e.Handled = true;
            }
        }

        private void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = e.OriginalSource as TextBox;
            CommitRename(textBox);
        }

        private async void CommitRename(TextBox textBox)
        {
            EndRename(textBox);

            try
            {
                var selectedItem = renamingItem;
                string currentName = selectedItem.FileName;
                string newName = textBox.Text;

                if (newName == null)
                    return;

                await tabInstance.instanceInteraction.RenameFileItem(selectedItem, currentName, newName);
            }
            catch (Exception)
            {

            }
        }

        private void EndRename(TextBox textBox)
        {
            StackPanel parentPanel = textBox.Parent as StackPanel;
            TextBlock textBlock = parentPanel.Children[0] as TextBlock;
            textBox.Visibility = Visibility.Collapsed;
            textBlock.Visibility = Visibility.Visible;
            textBox.LostFocus -= RenameTextBox_LostFocus;
            textBox.KeyDown += RenameTextBox_KeyDown;
            isRenaming = false;
        }

        private void FileList_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                if (!isRenaming)
                {
                    tabInstance.instanceInteraction.List_ItemClick(null, null);
                    e.Handled = true;
                }
            }
        }

        private void Page_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            var focusedElement = FocusManager.GetFocusedElement(XamlRoot) as FrameworkElement;
            if (focusedElement is TextBox)
                return;

            char letterPressed = Convert.ToChar(args.KeyCode);
            gv.Focus(FocusState.Keyboard);
            tabInstance.instanceInteraction.PushJumpChar(letterPressed);
        }
    }
}
