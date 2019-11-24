using Files.Dialogs;
using Files.Filesystem;
using Files.Interacts;
using Files.Navigation;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Files
{
    public sealed partial class ProHome : Page
    {
        public Grid deleteProgressBox;
        public ProgressBar deleteProgressBoxIndicator;
        public TextBlock deleteProgressBoxTitle;
        public TextBlock deleteProgressBoxTextInfo;
        public DisplayedPathText PathText { get; set; } = new DisplayedPathText();
        public Interacts.Home.HomeItemsState HomeItems { get; set; } = new Interacts.Home.HomeItemsState();
        public Interacts.Share.ShareItemsState ShareItems { get; set; } = new Interacts.Share.ShareItemsState();
        public Interacts.Layout.LayoutItemsState LayoutItems { get; set; } = new Interacts.Layout.LayoutItemsState();
        public AlwaysPresentCommandsState AlwaysPresentCommands { get; set; } = new AlwaysPresentCommandsState();
        public ObservableCollection<PathBoxItem> pathBoxItems = new ObservableCollection<PathBoxItem>();
        public Interaction instanceInteraction;
        private ItemViewModel _instanceViewModel;
        public ItemViewModel instanceViewModel
        {
            get
            {
                return _instanceViewModel;
            }
            set
            {
                _instanceViewModel = value;
                Bindings.Update();
            }
        }

        public ProHome()
        {
            this.InitializeComponent();
            PathText.Text = "New tab";
            LocationsList.SelectedIndex = 0;
            
            deleteProgressBox = DeleteProgressFakeDialog;
            deleteProgressBoxIndicator = deleteInfoCurrentIndicator;
            deleteProgressBoxTitle = title;
            deleteProgressBoxTextInfo = deleteInfoCurrentText;
        }


        private void VisiblePath_TextChanged(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                var PathBox = (sender as TextBox);
                var CurrentInput = PathBox.Text;
                if (ItemDisplayFrame.SourcePageType == typeof(GenericFileBrowser))
                {
                    var contentInstance = instanceViewModel;
                    CheckPathInput(contentInstance, CurrentInput);
                }
                else if (ItemDisplayFrame.SourcePageType == typeof(PhotoAlbum))
                {
                    var contentInstance = instanceViewModel;
                    CheckPathInput(contentInstance, CurrentInput);
                }
                else if (ItemDisplayFrame.SourcePageType == typeof(YourHome))
                {
                    var contentInstance = instanceViewModel;
                    CheckPathInput(contentInstance, CurrentInput);
                }
                VisiblePath.Visibility = Visibility.Collapsed;
                ClickablePath.Visibility = Visibility.Visible;
            }
            else if(e.Key == VirtualKey.Escape)
            {
                VisiblePath.Visibility = Visibility.Collapsed;
                ClickablePath.Visibility = Visibility.Visible;
            }
        }

        public async void CheckPathInput(ItemViewModel instance, string CurrentInput)
        {
            if (CurrentInput != instance.Universal.path)
            {
                HomeItems.isEnabled = false;
                ShareItems.isEnabled = false;

                if (CurrentInput == "Favorites" || CurrentInput.Equals("Home", StringComparison.OrdinalIgnoreCase) || CurrentInput == "favorites" || CurrentInput.Equals("New tab", StringComparison.OrdinalIgnoreCase))
                {
                    this.ItemDisplayFrame.Navigate(typeof(YourHome), "New tab");
                    PathText.Text = "New tab";
                    LayoutItems.isEnabled = false;
                }
                else if (CurrentInput.Equals("Start", StringComparison.OrdinalIgnoreCase))
                {
                    this.ItemDisplayFrame.Navigate(typeof(YourHome), "Start");
                    PathText.Text = "Start";
                    LayoutItems.isEnabled = false;
                }
                else if (CurrentInput == "Desktop" || CurrentInput == "desktop")
                {
                    this.ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.DesktopPath);
                    PathText.Text = "Desktop";
                    LayoutItems.isEnabled = true;
                }
                else if (CurrentInput == "Documents" || CurrentInput == "documents")
                {
                    this.ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.DocumentsPath);
                    PathText.Text = "Documents";
                    LayoutItems.isEnabled = true;
                }
                else if (CurrentInput == "Downloads" || CurrentInput == "downloads")
                {
                    this.ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.DownloadsPath);
                    PathText.Text = "Downloads";
                    LayoutItems.isEnabled = true;
                }
                else if (CurrentInput == "Pictures" || CurrentInput == "pictures")
                {
                    this.ItemDisplayFrame.Navigate(typeof(PhotoAlbum), App.PicturesPath);
                    PathText.Text = "Pictures";
                    LayoutItems.isEnabled = true;
                }
                else if (CurrentInput == "Music" || CurrentInput == "music")
                {
                    this.ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.MusicPath);
                    PathText.Text = "Music";
                    LayoutItems.isEnabled = true;
                }
                else if (CurrentInput == "Videos" || CurrentInput == "videos")
                {
                    this.ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.VideosPath);
                    PathText.Text = "Videos";
                    LayoutItems.isEnabled = true;
                }
                else if (CurrentInput == "OneDrive" || CurrentInput == "Onedrive" || CurrentInput == "onedrive")
                {
                    this.ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.OneDrivePath);
                    PathText.Text = "OneDrive";
                    LayoutItems.isEnabled = true;
                }
                else
                {
                    if (CurrentInput.Contains("."))
                    {
                        if (CurrentInput.Contains(".exe") || CurrentInput.Contains(".EXE"))
                        {
                            if (StorageFile.GetFileFromPathAsync(CurrentInput) != null)
                            {
                                if (this.ItemDisplayFrame.SourcePageType == typeof(GenericFileBrowser))
                                {
                                    await Interaction.LaunchExe(CurrentInput);
                                }
                                else if (this.ItemDisplayFrame.SourcePageType == typeof(PhotoAlbum))
                                {
                                    await Interaction.LaunchExe(CurrentInput);
                                }

                                VisiblePath.Text = instance.Universal.path;
                            }
                            else
                            {
                                MessageDialog dialog = new MessageDialog("The path typed was not correct. Please try again.", "Invalid Path");
                                await dialog.ShowAsync();
                            }
                        }
                        else if (StorageFolder.GetFolderFromPathAsync(CurrentInput) != null)
                        {
                            try
                            {
                                await StorageFolder.GetFolderFromPathAsync(CurrentInput);
                                this.ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), CurrentInput);
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
                            catch (System.Exception)
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
                                VisiblePath.Text = instance.Universal.path;
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
                            catch (System.Exception)
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
                            this.ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), CurrentInput);
                            LayoutItems.isEnabled = true;
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
                        catch (System.Exception)
                        {
                            MessageDialog dialog = new MessageDialog("The path typed was not correct. Please try again.", "Invalid Path");
                            await dialog.ShowAsync();
                        }

                    }

                }
            }
        }

        private void LocationsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            SidebarItem clickedItem = e.ClickedItem as SidebarItem;

            if (clickedItem.isDefaultLocation)
            {
                if (clickedItem.Text.ToString() == "Home")
                {
                    ItemDisplayFrame.Navigate(typeof(YourHome), "New tab");
                    PathText.Text = "New tab";
                    HomeItems.isEnabled = false;
                    ShareItems.isEnabled = false;
                    if (DrivesList.SelectedItem != null)
                    {
                        DrivesList.SelectedItem = null;
                        LayoutItems.isEnabled = false;
                    }
                    LayoutItems.isEnabled = false;
                }
                else if (clickedItem.Text.ToString() == "Desktop")
                {
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.DesktopPath);
                    PathText.Text = "Desktop";
                    HomeItems.isEnabled = false;
                    ShareItems.isEnabled = false;
                    if (DrivesList.SelectedItem != null)
                    {
                        DrivesList.SelectedItem = null;
                        LayoutItems.isEnabled = false;
                    }
                    LayoutItems.isEnabled = true;
                }
                else if (clickedItem.Text.ToString() == "Downloads")
                {
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.DownloadsPath);
                    PathText.Text = "Downloads";
                    HomeItems.isEnabled = false;
                    ShareItems.isEnabled = false;
                    if (DrivesList.SelectedItem != null)
                    {
                        DrivesList.SelectedItem = null;
                        LayoutItems.isEnabled = false;
                    }
                    LayoutItems.isEnabled = true;
                }
                else if (clickedItem.Text.ToString() == "Documents")
                {
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.DocumentsPath);
                    PathText.Text = "Documents";
                    HomeItems.isEnabled = false;
                    ShareItems.isEnabled = false;
                    if (DrivesList.SelectedItem != null)
                    {
                        DrivesList.SelectedItem = null;
                        LayoutItems.isEnabled = false;
                    }
                    LayoutItems.isEnabled = true;
                }
                else if (clickedItem.Text.ToString() == "Pictures")
                {
                    ItemDisplayFrame.Navigate(typeof(PhotoAlbum), App.PicturesPath);
                    PathText.Text = "Pictures";
                    HomeItems.isEnabled = false;
                    ShareItems.isEnabled = false;
                    if (DrivesList.SelectedItem != null)
                    {
                        DrivesList.SelectedItem = null;
                        LayoutItems.isEnabled = false;
                    }
                    LayoutItems.isEnabled = true;
                }
                else if (clickedItem.Text.ToString() == "Music")
                {
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.MusicPath);
                    PathText.Text = "Music";
                    HomeItems.isEnabled = false;
                    ShareItems.isEnabled = false;
                    if (DrivesList.SelectedItem != null)
                    {
                        DrivesList.SelectedItem = null;
                        LayoutItems.isEnabled = false;
                    }
                    LayoutItems.isEnabled = true;
                }
                else if (clickedItem.Text.ToString() == "Videos")
                {
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.VideosPath);
                    PathText.Text = "Videos";
                    HomeItems.isEnabled = false;
                    ShareItems.isEnabled = false;
                    if (DrivesList.SelectedItem != null)
                    {
                        DrivesList.SelectedItem = null;
                        LayoutItems.isEnabled = false;
                    }
                    LayoutItems.isEnabled = true;
                }
            }
            else
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), clickedItem.Path);
                PathText.Text = clickedItem.Text;
                HomeItems.isEnabled = false;
                ShareItems.isEnabled = false;
                if (DrivesList.SelectedItem != null)
                {
                    DrivesList.SelectedItem = null;
                    LayoutItems.isEnabled = false;
                }
                LayoutItems.isEnabled = true;
            }

        }

        private void DrivesList_ItemClick(object sender, ItemClickEventArgs e)
        {
            HomeItems.isEnabled = false;
            ShareItems.isEnabled = false;
            if (LocationsList.SelectedItem != null)
            {
                LocationsList.SelectedItem = null;
                LayoutItems.isEnabled = false;
            }

            DriveItem clickedItem = e.ClickedItem as DriveItem;

            if (clickedItem.tag.ToString() == "LocalDisk")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), @"C:\");
                PathText.Text = @"Local Disk (C:\)";
                LayoutItems.isEnabled = true;
            }
            else if (clickedItem.tag.ToString() == "OneDrive")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.OneDrivePath);
                PathText.Text = "OneDrive";
                LayoutItems.isEnabled = true;
            }
            else
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), clickedItem.tag.ToString());
                PathText.Text = clickedItem.tag.ToString();
                LayoutItems.isEnabled = true;
            }  
        }

        string NavParams = null;

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            NavParams = eventArgs.Parameter.ToString();
        }

        private void LocationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                DrivesList.SelectedItem = null;
            }
        }

        private void DrivesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                LocationsList.SelectedItem = null;
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            var instanceTabsView = rootFrame.Content as InstanceTabsView;
            instanceTabsView.AddNewTab(typeof(Settings), "Settings");
        }

        private async void AddItem_Click(object sender, RoutedEventArgs e)
        {
            if (ItemDisplayFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                App.addItemDialog.addDialogContentFrame.Navigate(typeof(AddItem), ItemDisplayFrame.Content as GenericFileBrowser, new SuppressNavigationTransitionInfo());
            }
            else if (ItemDisplayFrame.SourcePageType == typeof(PhotoAlbum))
            {
                App.addItemDialog.addDialogContentFrame.Navigate(typeof(AddItem), ItemDisplayFrame.Content as PhotoAlbum, new SuppressNavigationTransitionInfo());
            }
            await App.addItemDialog.ShowAsync();
        }

        private async void LayoutButton_Click(object sender, RoutedEventArgs e)
        {
            await App.layoutDialog.ShowAsync();
        }

        public async void ShowPropertiesButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.ItemDisplayFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                App.propertiesDialog.accessiblePropertiesFrame.Tag = App.propertiesDialog;
                App.propertiesDialog.accessiblePropertiesFrame.Navigate(typeof(Properties), (App.OccupiedInstance.ItemDisplayFrame.Content as GenericFileBrowser).AllView.SelectedItems, new SuppressNavigationTransitionInfo());
            }
            else if (this.ItemDisplayFrame.SourcePageType == typeof(PhotoAlbum))
            {
                App.propertiesDialog.accessiblePropertiesFrame.Tag = App.propertiesDialog;
                App.propertiesDialog.accessiblePropertiesFrame.Navigate(typeof(Properties), (App.OccupiedInstance.ItemDisplayFrame.Content as PhotoAlbum).FileList.SelectedItems, new SuppressNavigationTransitionInfo());
            }
            await App.propertiesDialog.ShowAsync(ContentDialogPlacement.Popup);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.OccupiedInstance == null && ItemViewModel.GetCurrentSelectedTabInstance<ProHome>().Equals(this))
            {
                App.OccupiedInstance = this;
            }

            if (NavParams == "Start" || NavParams == "New tab")
            {
                ItemDisplayFrame.Navigate(typeof(YourHome), NavParams, new SuppressNavigationTransitionInfo());
                LocationsList.SelectedIndex = 0;
            }
            else if (NavParams == "Desktop")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.DesktopPath, new SuppressNavigationTransitionInfo());
                LocationsList.SelectedIndex = 1;
            }
            else if (NavParams == "Downloads")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.DownloadsPath, new SuppressNavigationTransitionInfo());
                LocationsList.SelectedIndex = 2;
            }
            else if (NavParams == "Documents")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.DocumentsPath, new SuppressNavigationTransitionInfo());
                LocationsList.SelectedIndex = 3;
            }
            else if (NavParams == "Pictures" || NavParams == App.PicturesPath)
            {
                ItemDisplayFrame.Navigate(typeof(PhotoAlbum), App.PicturesPath, new SuppressNavigationTransitionInfo());
                LocationsList.SelectedIndex = 4;
            }
            else if (NavParams == "Music")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.MusicPath, new SuppressNavigationTransitionInfo());
                LocationsList.SelectedIndex = 5;
            }
            else if (NavParams == "Videos")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), App.VideosPath, new SuppressNavigationTransitionInfo());
                LocationsList.SelectedIndex = 6;
            }
            else
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), NavParams, new SuppressNavigationTransitionInfo());
                if (NavParams.Contains("C:", StringComparison.OrdinalIgnoreCase))
                {
                    DrivesList.SelectedIndex = 0;
                }
                else
                {
                    DrivesList.SelectedItem = null;
                }
            }

            this.Loaded -= Page_Loaded;
        }

        private void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            SearchBox.Focus(FocusState.Programmatic);
        }

        private async void NewWindowButton_Click(object sender, RoutedEventArgs e)
        {
            var filesUWPUri = new Uri("files-uwp:");
            var options = new LauncherOptions()
            {
                DisplayApplicationPicker = false
            };
            await Launcher.LaunchUriAsync(filesUWPUri);
        }

        private void ItemDisplayFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if(ItemDisplayFrame.CurrentSourcePageType == typeof(GenericFileBrowser))
            {
                // Reset DataGrid Rows that may be in "cut" command mode
                App.OccupiedInstance.instanceInteraction.dataGridRows.Clear();
                Interaction.FindChildren<DataGridRow>(App.OccupiedInstance.instanceInteraction.dataGridRows, (ItemDisplayFrame.Content as GenericFileBrowser).AllView);
                foreach (DataGridRow dataGridRow in App.OccupiedInstance.instanceInteraction.dataGridRows)
                {
                    if ((ItemDisplayFrame.Content as GenericFileBrowser).AllView.Columns[0].GetCellContent(dataGridRow).Opacity < 1)
                    {
                        (ItemDisplayFrame.Content as GenericFileBrowser).AllView.Columns[0].GetCellContent(dataGridRow).Opacity = 1;
                    }
                }
            }
        }

        private void HideFakeDialogButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteProgressFakeDialog.Visibility = Visibility.Collapsed;
        }

        private void VisiblePath_LostFocus(object sender, RoutedEventArgs e)
        {
            VisiblePath.Visibility = Visibility.Collapsed;
            ClickablePath.Visibility = Visibility.Visible;
        }

        private void ClickablePathView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PathViewInteract.SelectedIndex = -1;
        }

        private void PathViewInteract_ItemClick(object sender, ItemClickEventArgs e)
        {
            var itemTappedPath = (e.ClickedItem as PathBoxItem).Path.ToString();
            if (itemTappedPath == "Start" || itemTappedPath == "New tab") { return; }
            
            App.OccupiedInstance.ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), itemTappedPath, new SuppressNavigationTransitionInfo());
        }

        private void ManualPathEntryItem_Click(object sender, RoutedEventArgs e)
        {
            VisiblePath.Visibility = Visibility.Visible;
            ClickablePath.Visibility = Visibility.Collapsed;
            VisiblePath.Focus(FocusState.Programmatic);
            VisiblePath.SelectionStart = VisiblePath.Text.Length;
        }

        private void LocationsList_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            ListView list = (ListView)sender;
            var item = ((FrameworkElement)e.OriginalSource).DataContext as SidebarItem;
            if (!item.isDefaultLocation)
            {
                SideBarItemContextFlyout.ShowAt(list, e.GetPosition(list));
                App.rightClickedItem = item;
            }
        }
    }

    public class PathBoxItem
    {
        public string Title { get; set; }
        public string Path { get; set; }
    }
}
