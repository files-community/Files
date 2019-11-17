using Files.Dialogs;
using Files.Filesystem;
using Files.Interacts;
using Files.Navigation;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Files
{
    public sealed partial class ProHome : Page
    {
        public ListView locationsList;
        public ListView drivesList;
        public Frame accessibleContentFrame;
        public Frame accessiblePropertiesFrame;
        public Button BackButton;
        public Button ForwardButton;
        public Button UpButton;
        public Button accessiblePasteButton;
        public Button RefreshButton;
        public Button AddItemButton;
        public TextBox PathBox;
        public static string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        public static string DocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public static string DownloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
        public static string OneDrivePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive";
        public static string PicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        public static string MusicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        public static string VideosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        public TeachingTip RibbonTeachingTip;
        public Grid deleteProgressBox;
        public ProgressBar deleteProgressBoxIndicator;
        public TextBlock deleteProgressBoxTitle;
        public TextBlock deleteProgressBoxTextInfo;
        public BackState BS { get; set; } = new BackState();
        public ForwardState FS { get; set; } = new ForwardState();
        public DisplayedPathText PathText { get; set; } = new DisplayedPathText();
        public PasteState PS { get; set; } = new PasteState();
        public Interacts.Home.HomeItemsState HomeItems { get; set; } = new Interacts.Home.HomeItemsState();
        public Interacts.Share.ShareItemsState ShareItems { get; set; } = new Interacts.Share.ShareItemsState();
        public Interacts.Layout.LayoutItemsState LayoutItems { get; set; } = new Interacts.Layout.LayoutItemsState();
        public AlwaysPresentCommandsState AlwaysPresentCommands { get; set; } = new AlwaysPresentCommandsState();

        public AddItemDialog addItemDialog = new AddItemDialog();
        public LayoutDialog layoutDialog = new LayoutDialog();
        public PropertiesDialog propertiesDialog = new PropertiesDialog();
        public ListView accessiblePathListView;
        public ObservableCollection<PathBoxItem> pathBoxItems = new ObservableCollection<PathBoxItem>();
        private ItemViewModel _instanceViewModel;
        public Interaction instanceInteraction;

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
            locationsList = LocationsList;
            drivesList = DrivesList;
            accessibleContentFrame = ItemDisplayFrame;
            BackButton = Back;
            UpButton = Up;
            ForwardButton = Forward;
            RefreshButton = Refresh;
            AddItemButton = addItemButton;
            PathBox = VisiblePath;
            PathText.Text = "New tab";
            accessiblePasteButton = PasteButton;
            LocationsList.SelectedIndex = 0;
            RibbonTeachingTip = RibbonTip;
            accessiblePathListView = PathViewInteract;
            accessiblePathListView.ItemsSource = pathBoxItems;
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            //PopulateNavViewWithExternalDrives();
            BackButton.Click += NavigationActions.Back_Click;
            ForwardButton.Click += NavigationActions.Forward_Click;
            RefreshButton.Click += NavigationActions.Refresh_Click;
            UpButton.Click += NavigationActions.Up_Click;
            UnpinItem.Click += App.FlyoutItem_Click;

            // Overwrite paths for common locations if Custom Locations setting is enabled
            if(localSettings.Values["customLocationsSetting"] != null)
            {
                if (localSettings.Values["customLocationsSetting"].Equals(true))
                {
                    DesktopPath = localSettings.Values["DesktopLocation"].ToString();
                    DownloadsPath = localSettings.Values["DownloadsLocation"].ToString();
                    DocumentsPath = localSettings.Values["DocumentsLocation"].ToString();
                    PicturesPath = localSettings.Values["PicturesLocation"].ToString();
                    MusicPath = localSettings.Values["MusicLocation"].ToString();
                    VideosPath = localSettings.Values["VideosLocation"].ToString();
                }
            }
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
                if (accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
                {
                    var contentInstance = instanceViewModel;
                    CheckPathInput(contentInstance, CurrentInput);
                }
                else if (accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
                {
                    var contentInstance = instanceViewModel;
                    CheckPathInput(contentInstance, CurrentInput);
                }
                else if (accessibleContentFrame.SourcePageType == typeof(YourHome))
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
                    this.accessibleContentFrame.Navigate(typeof(YourHome), "New tab");
                    PathText.Text = "New tab";
                    LayoutItems.isEnabled = false;
                }
                else if (CurrentInput.Equals("Start", StringComparison.OrdinalIgnoreCase))
                {
                    this.accessibleContentFrame.Navigate(typeof(YourHome), "Start");
                    PathText.Text = "Start";
                    LayoutItems.isEnabled = false;
                }
                else if (CurrentInput == "Desktop" || CurrentInput == "desktop")
                {
                    this.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DesktopPath);
                    PathText.Text = "Desktop";
                    LayoutItems.isEnabled = true;
                }
                else if (CurrentInput == "Documents" || CurrentInput == "documents")
                {
                    this.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DocumentsPath);
                    PathText.Text = "Documents";
                    LayoutItems.isEnabled = true;
                }
                else if (CurrentInput == "Downloads" || CurrentInput == "downloads")
                {
                    this.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DownloadsPath);
                    PathText.Text = "Downloads";
                    LayoutItems.isEnabled = true;
                }
                else if (CurrentInput == "Pictures" || CurrentInput == "pictures")
                {
                    this.accessibleContentFrame.Navigate(typeof(PhotoAlbum), PicturesPath);
                    PathText.Text = "Pictures";
                    LayoutItems.isEnabled = true;
                }
                else if (CurrentInput == "Music" || CurrentInput == "music")
                {
                    this.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MusicPath);
                    PathText.Text = "Music";
                    LayoutItems.isEnabled = true;
                }
                else if (CurrentInput == "Videos" || CurrentInput == "videos")
                {
                    this.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), VideosPath);
                    PathText.Text = "Videos";
                    LayoutItems.isEnabled = true;
                }
                else if (CurrentInput == "OneDrive" || CurrentInput == "Onedrive" || CurrentInput == "onedrive")
                {
                    this.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), OneDrivePath);
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
                                if (this.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
                                {
                                    await Interaction.LaunchExe(CurrentInput);
                                }
                                else if (this.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
                                {
                                    await Interaction.LaunchExe(CurrentInput);
                                }

                                PathBox.Text = instance.Universal.path;
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
                                this.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), CurrentInput);
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
                                PathBox.Text = instance.Universal.path;
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
                            this.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), CurrentInput);
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
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), DesktopPath);
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
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), DownloadsPath);
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
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), DocumentsPath);
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
                    ItemDisplayFrame.Navigate(typeof(PhotoAlbum), PicturesPath);
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
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), MusicPath);
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
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), VideosPath);
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
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), OneDrivePath);
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
                drivesList.SelectedItem = null;
            }
        }

        private void DrivesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                locationsList.SelectedItem = null;
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            var instanceTabsView = rootFrame.Content as InstanceTabsView;
            instanceTabsView.AddNewTab(typeof(Settings), "Settings");
        }

        private void CutButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                instanceInteraction.CutItem_Click(null, null);
            }
            else if (this.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                instanceInteraction.CutItem_Click(null, null);
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                instanceInteraction.CopyItem_ClickAsync(null, null);
            }
            else if (this.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                instanceInteraction.CopyItem_ClickAsync(null, null);
            }
        }

        private void PasteButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                instanceInteraction.PasteItem_ClickAsync(null, null);
            }
            else if (this.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                instanceInteraction.PasteItem_ClickAsync(null, null);
            }
        }

        private void CopyPathButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                instanceInteraction.GetPath_Click(null, null);
            }
            else if (this.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                instanceInteraction.GetPath_Click(null, null);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                instanceInteraction.DeleteItem_Click(null, null);
            }
            else if (this.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                instanceInteraction.DeleteItem_Click(null, null);
            }
        }

        private void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                instanceInteraction.RenameItem_Click(null, null);
            }
            else if (this.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                instanceInteraction.RenameItem_Click(null, null);
            }
        }

        private async void AddItem_Click(object sender, RoutedEventArgs e)
        {
            if (accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                addItemDialog.addDialogContentFrame.Navigate(typeof(AddItem), accessibleContentFrame.Content as GenericFileBrowser, new SuppressNavigationTransitionInfo());
            }
            else if (accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                addItemDialog.addDialogContentFrame.Navigate(typeof(AddItem), accessibleContentFrame.Content as PhotoAlbum, new SuppressNavigationTransitionInfo());
            }
            await addItemDialog.ShowAsync();
        }

        private void OpenWithButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                instanceInteraction.OpenItem_Click(null, null);
            }
            else if (this.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                instanceInteraction.OpenItem_Click(null, null);
            }
        }

        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                instanceInteraction.ShareItem_Click(null, null);
            }
            else if (this.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                instanceInteraction.ShareItem_Click(null, null);
            }
        }

        private async void LayoutButton_Click(object sender, RoutedEventArgs e)
        {
            await layoutDialog.ShowAsync();
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                instanceInteraction.SelectAllItems();
            }
            else if (this.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                instanceInteraction.SelectAllItems();
            }
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                instanceInteraction.ClearAllItems();
            }
            else if (this.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                instanceInteraction.ClearAllItems();
            }
        }


        public async void ShowPropertiesButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                propertiesDialog.accessiblePropertiesFrame.Tag = propertiesDialog;
                propertiesDialog.accessiblePropertiesFrame.Navigate(typeof(Properties), (App.selectedTabInstance.accessibleContentFrame.Content as GenericFileBrowser).data.SelectedItems, new SuppressNavigationTransitionInfo());
            }
            else if (this.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                propertiesDialog.accessiblePropertiesFrame.Tag = propertiesDialog;
                propertiesDialog.accessiblePropertiesFrame.Navigate(typeof(Properties), (App.selectedTabInstance.accessibleContentFrame.Content as PhotoAlbum).gv.SelectedItems, new SuppressNavigationTransitionInfo());
            }
            await propertiesDialog.ShowAsync(ContentDialogPlacement.Popup);
        }

        private void RibbonTip_Loaded(object sender, RoutedEventArgs e)
        {
            if (ApplicationData.Current.LocalSettings.Values["HasBeenWelcomed"] == null)
            {
                this.RibbonTeachingTip.IsOpen = false;
                ApplicationData.Current.LocalSettings.Values["HasBeenWelcomed"] = true;
            }
            else
            {
                this.RibbonTeachingTip.IsOpen = false;
            }

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {


            if (NavParams == "Start" || NavParams == "New tab")
            {
                ItemDisplayFrame.Navigate(typeof(YourHome), NavParams, new SuppressNavigationTransitionInfo());
                locationsList.SelectedIndex = 0;
            }
            else if (NavParams == "Desktop")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), DesktopPath, new SuppressNavigationTransitionInfo());
                locationsList.SelectedIndex = 1;
            }
            else if (NavParams == "Downloads")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), DownloadsPath, new SuppressNavigationTransitionInfo());
                locationsList.SelectedIndex = 2;
            }
            else if (NavParams == "Documents")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), DocumentsPath, new SuppressNavigationTransitionInfo());
                locationsList.SelectedIndex = 3;
            }
            else if (NavParams == "Pictures" || NavParams == PicturesPath)
            {
                ItemDisplayFrame.Navigate(typeof(PhotoAlbum), PicturesPath, new SuppressNavigationTransitionInfo());
                locationsList.SelectedIndex = 4;
            }
            else if (NavParams == "Music")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), MusicPath, new SuppressNavigationTransitionInfo());
                locationsList.SelectedIndex = 5;
            }
            else if (NavParams == "Videos")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), VideosPath, new SuppressNavigationTransitionInfo());
                locationsList.SelectedIndex = 6;
            }
            else
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), NavParams, new SuppressNavigationTransitionInfo());
                if (NavParams.Contains("C:", StringComparison.OrdinalIgnoreCase))
                {
                    drivesList.SelectedIndex = 0;
                }
                else
                {
                    drivesList.SelectedItem = null;
                }
            }
            //accessibleContentFrame.Navigate(typeof(YourHome), NavParams, new SuppressNavigationTransitionInfo());
            this.Loaded -= Page_Loaded;
        }

        private void ItemDisplayFrame_Navigating(object sender, Windows.UI.Xaml.Navigation.NavigatingCancelEventArgs e)
        {

        }

        private void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            SearchBox.Focus(FocusState.Programmatic);
        }

        // Initiate search from term
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //this.accessibleContentFrame
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

        private void ItemDisplayFrame_Navigated(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            if (accessibleContentFrame.CurrentSourcePageType.Equals(typeof(YourHome)))
            {
                UpButton.IsEnabled = false;
            }

            if(instanceViewModel == null && instanceInteraction == null)
            {
                Frame rootFrame = Window.Current.Content as Frame;
                var instanceTabsView = rootFrame.Content as InstanceTabsView;
                instanceTabsView.TabStrip_SelectionChanged(null, null);
                instanceViewModel = new ItemViewModel();
                instanceInteraction = new Interaction();
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
            
            App.selectedTabInstance.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), itemTappedPath, new SuppressNavigationTransitionInfo());
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

    public class NavigationActions
    {
        public async static void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var ContentOwnedViewModelInstance = App.selectedTabInstance.instanceViewModel;
                ContentOwnedViewModelInstance.AddItemsToCollectionAsync(ContentOwnedViewModelInstance.Universal.path);
            });
        }

        public static void Back_Click(object sender, RoutedEventArgs e)
        {
            App.selectedTabInstance.BackButton.IsEnabled = false;
            Frame instanceContentFrame = App.selectedTabInstance.accessibleContentFrame;
            App.selectedTabInstance.instanceViewModel.CancelLoadAndClearFiles();
            if ((App.selectedTabInstance.accessibleContentFrame.Content as GenericFileBrowser) != null)
            {
                var instanceContent = (instanceContentFrame.Content as GenericFileBrowser);
                if (instanceContentFrame.CanGoBack)
                {
                    var previousSourcePageType = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1].SourcePageType;
                    var Parameter = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1].Parameter;

                    if (previousSourcePageType == typeof(YourHome))
                    {
                        App.selectedTabInstance.locationsList.SelectedIndex = 0;
                        App.selectedTabInstance.PathText.Text = "New tab";
                    }
                    else
                    {
                        var CurrentTabInstance = App.selectedTabInstance;
                        if (Parameter.ToString() == ProHome.DesktopPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 1;
                            CurrentTabInstance.PathText.Text = "Desktop";
                        }
                        else if (Parameter.ToString() == ProHome.DownloadsPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 2;
                            CurrentTabInstance.PathText.Text = "Downloads";
                        }
                        else if (Parameter.ToString() == ProHome.DocumentsPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 3;
                            CurrentTabInstance.PathText.Text = "Documents";
                        }
                        else if (Parameter.ToString() == ProHome.PicturesPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 4;
                            CurrentTabInstance.PathText.Text = "Pictures";
                        }
                        else if (Parameter.ToString() == ProHome.MusicPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 5;
                            CurrentTabInstance.PathText.Text = "Music";
                        }
                        else if (Parameter.ToString() == ProHome.VideosPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 6;
                            CurrentTabInstance.PathText.Text = "Videos";
                        }
                        else if (Parameter.ToString() == ProHome.OneDrivePath)
                        {
                            CurrentTabInstance.drivesList.SelectedItem = CurrentTabInstance.drivesList.Items.Where(x => (x as DriveItem).tag == "OneDrive").First();
                            CurrentTabInstance.PathText.Text = "OneDrive";
                        }
                        else
                        {
                            if (Parameter.ToString().Contains("C:\\") || Parameter.ToString().Contains("c:\\"))
                            {
                                CurrentTabInstance.drivesList.SelectedItem = CurrentTabInstance.drivesList.Items.Where(x => (x as DriveItem).tag == "C:\\").First();
                            }
                            else
                            {
                                foreach (DriveItem drive in CurrentTabInstance.drivesList.Items)
                                {
                                    if (drive.tag.ToString().Contains(Parameter.ToString().Split("\\")[0]))
                                    {
                                        CurrentTabInstance.drivesList.SelectedItem = drive;
                                        break;
                                    }
                                }

                            }
                            CurrentTabInstance.PathText.Text = Parameter.ToString();
                        }
                    }
                    instanceContentFrame.GoBack();
                }
            }
            else if ((App.selectedTabInstance.accessibleContentFrame.Content as PhotoAlbum) != null)
            {
                var instanceContent = (instanceContentFrame.Content as PhotoAlbum);
                if (instanceContentFrame.CanGoBack)
                {
                    var previousSourcePageType = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1].SourcePageType;
                    var Parameter = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1].Parameter;

                    if (previousSourcePageType == typeof(YourHome))
                    {

                        App.selectedTabInstance.locationsList.SelectedIndex = 0;
                        App.selectedTabInstance.PathText.Text = "New tab";
                    }
                    else
                    {
                        var CurrentTabInstance = App.selectedTabInstance;
                        if (Parameter.ToString() == ProHome.DesktopPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 1;
                            CurrentTabInstance.PathText.Text = "Desktop";
                        }
                        else if (Parameter.ToString() == ProHome.DownloadsPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 2;
                            CurrentTabInstance.PathText.Text = "Downloads";
                        }
                        else if (Parameter.ToString() == ProHome.DocumentsPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 3;
                            CurrentTabInstance.PathText.Text = "Documents";
                        }
                        else if (Parameter.ToString() == ProHome.PicturesPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 4;
                            CurrentTabInstance.PathText.Text = "Pictures";
                        }
                        else if (Parameter.ToString() == ProHome.MusicPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 5;
                            CurrentTabInstance.PathText.Text = "Music";
                        }
                        else if (Parameter.ToString() == ProHome.VideosPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 6;
                            CurrentTabInstance.PathText.Text = "Videos";
                        }
                        else if (Parameter.ToString() == ProHome.OneDrivePath)
                        {
                            CurrentTabInstance.drivesList.SelectedItem = CurrentTabInstance.drivesList.Items.Where(x => (x as DriveItem).tag.ToString() == "OneDrive").First();
                            CurrentTabInstance.PathText.Text = "OneDrive";
                        }
                        else
                        {
                            if (Parameter.ToString().Contains("C:\\") || Parameter.ToString().Contains("c:\\"))
                            {
                                CurrentTabInstance.drivesList.SelectedItem = CurrentTabInstance.drivesList.Items.Where(x => (x as DriveItem).tag == "C:\\").First();
                            }
                            else
                            {
                                foreach (DriveItem drive in CurrentTabInstance.drivesList.Items)
                                {
                                    if (drive.tag.ToString().Contains(Parameter.ToString().Split("\\")[0]))
                                    {
                                        CurrentTabInstance.drivesList.SelectedItem = drive;
                                        break;
                                    }
                                }

                            }
                            CurrentTabInstance.PathText.Text = Parameter.ToString();
                        }
                    }
                    instanceContentFrame.GoBack();
                }
            }
            else if ((App.selectedTabInstance.accessibleContentFrame.Content as YourHome) != null)
            {
                var instanceContent = (instanceContentFrame.Content as YourHome);

                if (instanceContentFrame.CanGoBack)
                {
                    var previousSourcePageType = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1].SourcePageType;
                    var Parameter = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1].Parameter;

                    if (previousSourcePageType == typeof(YourHome))
                    {
                        App.selectedTabInstance.locationsList.SelectedIndex = 0;
                        App.selectedTabInstance.PathText.Text = "New tab";
                    }
                    else
                    {
                        var CurrentTabInstance = App.selectedTabInstance;
                        if (Parameter.ToString() == ProHome.DesktopPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 1;
                            CurrentTabInstance.PathText.Text = "Desktop";
                        }
                        else if (Parameter.ToString() == ProHome.DownloadsPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 2;
                            CurrentTabInstance.PathText.Text = "Downloads";
                        }
                        else if (Parameter.ToString() == ProHome.DocumentsPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 3;
                            CurrentTabInstance.PathText.Text = "Documents";
                        }
                        else if (Parameter.ToString() == ProHome.PicturesPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 4;
                            CurrentTabInstance.PathText.Text = "Pictures";
                        }
                        else if (Parameter.ToString() == ProHome.MusicPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 5;
                            CurrentTabInstance.PathText.Text = "Music";
                        }
                        else if (Parameter.ToString() == ProHome.VideosPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 6;
                            CurrentTabInstance.PathText.Text = "Videos";
                        }
                        else if (Parameter.ToString() == ProHome.OneDrivePath)
                        {
                            CurrentTabInstance.drivesList.SelectedItem = CurrentTabInstance.drivesList.Items.Where(x => (x as DriveItem).tag.ToString() == "OneDrive").First();
                            CurrentTabInstance.PathText.Text = "OneDrive";
                        }
                        else
                        {
                            if (Parameter.ToString().Contains("C:\\") || Parameter.ToString().Contains("c:\\"))
                            {
                                CurrentTabInstance.drivesList.SelectedItem = CurrentTabInstance.drivesList.Items.Where(x => (x as DriveItem).tag == "C:\\").First();
                            }
                            else
                            {
                                foreach (DriveItem drive in CurrentTabInstance.drivesList.Items)
                                {
                                    if (drive.tag.ToString().Contains(Parameter.ToString().Split("\\")[0]))
                                    {
                                        CurrentTabInstance.drivesList.SelectedItem = drive;
                                        break;
                                    }
                                }

                            }
                            CurrentTabInstance.PathText.Text = Parameter.ToString();
                        }
                    }

                    instanceContentFrame.GoBack();
                }
            }

        }

        public static void Forward_Click(object sender, RoutedEventArgs e)
        {
            App.selectedTabInstance.ForwardButton.IsEnabled = false;
            App.selectedTabInstance.instanceViewModel.CancelLoadAndClearFiles();
            Frame instanceContentFrame = App.selectedTabInstance.accessibleContentFrame;
            if ((App.selectedTabInstance.accessibleContentFrame.Content as GenericFileBrowser) != null)
            {
                var instanceContent = (instanceContentFrame.Content as GenericFileBrowser);

                if (instanceContentFrame.CanGoForward)
                {
                    var previousSourcePageType = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].SourcePageType;
                    var Parameter = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].Parameter;

                    if (previousSourcePageType == typeof(YourHome))
                    {
                        App.selectedTabInstance.locationsList.SelectedIndex = 0;
                        App.selectedTabInstance.PathText.Text = "New tab";
                    }
                    else
                    {
                        var CurrentTabInstance = App.selectedTabInstance;
                        if (Parameter.ToString() == ProHome.DesktopPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 1;
                            CurrentTabInstance.PathText.Text = "Desktop";
                        }
                        else if (Parameter.ToString() == ProHome.DownloadsPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 2;
                            CurrentTabInstance.PathText.Text = "Downloads";
                        }
                        else if (Parameter.ToString() == ProHome.DocumentsPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 3;
                            CurrentTabInstance.PathText.Text = "Documents";
                        }
                        else if (Parameter.ToString() == ProHome.PicturesPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 4;
                            CurrentTabInstance.PathText.Text = "Pictures";
                        }
                        else if (Parameter.ToString() == ProHome.MusicPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 5;
                            CurrentTabInstance.PathText.Text = "Music";
                        }
                        else if (Parameter.ToString() == ProHome.VideosPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 6;
                            CurrentTabInstance.PathText.Text = "Videos";
                        }
                        else if (Parameter.ToString() == ProHome.OneDrivePath)
                        {
                            CurrentTabInstance.drivesList.SelectedItem = CurrentTabInstance.drivesList.Items.Where(x => (x as DriveItem).tag.ToString() == "OneDrive").First();
                            CurrentTabInstance.PathText.Text = "OneDrive";
                        }
                        else
                        {
                            if (Parameter.ToString().Contains("C:\\") || Parameter.ToString().Contains("c:\\"))
                            {
                                CurrentTabInstance.drivesList.SelectedItem = CurrentTabInstance.drivesList.Items.Where(x => (x as DriveItem).tag == "C:\\").First();
                            }
                            else
                            {
                                foreach (DriveItem drive in CurrentTabInstance.drivesList.Items)
                                {
                                    if (drive.tag.ToString().Contains(Parameter.ToString().Split("\\")[0]))
                                    {
                                        CurrentTabInstance.drivesList.SelectedItem = drive;
                                        break;
                                    }
                                }

                            }
                            CurrentTabInstance.PathText.Text = Parameter.ToString();
                        }
                    }

                    instanceContentFrame.GoForward();
                }
            }
            else if ((App.selectedTabInstance.accessibleContentFrame.Content as PhotoAlbum) != null)
            {
                var instance = App.selectedTabInstance.instanceViewModel;
                var instanceContent = (instanceContentFrame.Content as PhotoAlbum);

                if (instanceContentFrame.CanGoForward)
                {
                    var previousSourcePageType = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].SourcePageType;
                    var Parameter = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].Parameter;

                    if (previousSourcePageType == typeof(YourHome))
                    {
                        App.selectedTabInstance.locationsList.SelectedIndex = 0;
                        App.selectedTabInstance.PathText.Text = "New tab";
                    }
                    else
                    {
                        var CurrentTabInstance = App.selectedTabInstance;
                        if (Parameter.ToString() == ProHome.DesktopPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 1;
                            CurrentTabInstance.PathText.Text = "Desktop";
                        }
                        else if (Parameter.ToString() == ProHome.DownloadsPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 2;
                            CurrentTabInstance.PathText.Text = "Downloads";
                        }
                        else if (Parameter.ToString() == ProHome.DocumentsPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 3;
                            CurrentTabInstance.PathText.Text = "Documents";
                        }
                        else if (Parameter.ToString() == ProHome.PicturesPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 4;
                            CurrentTabInstance.PathText.Text = "Pictures";
                        }
                        else if (Parameter.ToString() == ProHome.MusicPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 5;
                            CurrentTabInstance.PathText.Text = "Music";
                        }
                        else if (Parameter.ToString() == ProHome.VideosPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 6;
                            CurrentTabInstance.PathText.Text = "Videos";
                        }
                        else if (Parameter.ToString() == ProHome.OneDrivePath)
                        {
                            CurrentTabInstance.drivesList.SelectedItem = CurrentTabInstance.drivesList.Items.Where(x => (x as DriveItem).tag.ToString() == "OneDrive").First();
                            CurrentTabInstance.PathText.Text = "OneDrive";
                        }
                        else
                        {
                            if (Parameter.ToString().Contains("C:\\") || Parameter.ToString().Contains("c:\\"))
                            {
                                CurrentTabInstance.drivesList.SelectedItem = CurrentTabInstance.drivesList.Items.Where(x => (x as DriveItem).tag == "C:\\").First();
                            }
                            else
                            {
                                foreach (DriveItem drive in CurrentTabInstance.drivesList.Items)
                                {
                                    if (drive.tag.ToString().Contains(Parameter.ToString().Split("\\")[0]))
                                    {
                                        CurrentTabInstance.drivesList.SelectedItem = drive;
                                        break;
                                    }
                                }

                            }
                            CurrentTabInstance.PathText.Text = Parameter.ToString();
                        }
                    }

                    instanceContentFrame.GoForward();
                }
            }
            else if ((App.selectedTabInstance.accessibleContentFrame.Content as YourHome) != null)
            {
                var instanceContent = (instanceContentFrame.Content as YourHome);

                if (instanceContentFrame.CanGoForward)
                {
                    var previousSourcePageType = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].SourcePageType;
                    var Parameter = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].Parameter;

                    if (previousSourcePageType == typeof(YourHome))
                    {
                        App.selectedTabInstance.locationsList.SelectedIndex = 0;
                        App.selectedTabInstance.PathText.Text = "New tab";
                    }
                    else
                    {
                        var CurrentTabInstance = App.selectedTabInstance;
                        if (Parameter.ToString() == ProHome.DesktopPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 1;
                            CurrentTabInstance.PathText.Text = "Desktop";
                        }
                        else if (Parameter.ToString() == ProHome.DownloadsPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 2;
                            CurrentTabInstance.PathText.Text = "Downloads";
                        }
                        else if (Parameter.ToString() == ProHome.DocumentsPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 3;
                            CurrentTabInstance.PathText.Text = "Documents";
                        }
                        else if (Parameter.ToString() == ProHome.PicturesPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 4;
                            CurrentTabInstance.PathText.Text = "Pictures";
                        }
                        else if (Parameter.ToString() == ProHome.MusicPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 5;
                            CurrentTabInstance.PathText.Text = "Music";
                        }
                        else if (Parameter.ToString() == ProHome.VideosPath)
                        {
                            CurrentTabInstance.locationsList.SelectedIndex = 6;
                            CurrentTabInstance.PathText.Text = "Videos";
                        }
                        else if (Parameter.ToString() == ProHome.OneDrivePath)
                        {
                            CurrentTabInstance.drivesList.SelectedItem = CurrentTabInstance.drivesList.Items.Where(x => (x as DriveItem).tag.ToString() == "OneDrive").First();
                            CurrentTabInstance.PathText.Text = "OneDrive";
                        }
                        else
                        {
                            if (Parameter.ToString().Contains("C:\\") || Parameter.ToString().Contains("c:\\"))
                            {
                                CurrentTabInstance.drivesList.SelectedItem = CurrentTabInstance.drivesList.Items.Where(x => (x as DriveItem).tag == "C:\\").First();
                            }
                            else
                            {
                                foreach (DriveItem drive in CurrentTabInstance.drivesList.Items)
                                {
                                    if (drive.tag.ToString().Contains(Parameter.ToString().Split("\\")[0]))
                                    {
                                        CurrentTabInstance.drivesList.SelectedItem = drive;
                                        break;
                                    }
                                }

                            }
                            CurrentTabInstance.PathText.Text = Parameter.ToString();
                        }
                    }

                    instanceContentFrame.GoForward();
                }
            }
        }

        public static void Up_Click(object sender, RoutedEventArgs e)
        {
            App.selectedTabInstance.UpButton.IsEnabled = false;
            Frame instanceContentFrame = App.selectedTabInstance.accessibleContentFrame;
            App.selectedTabInstance.instanceViewModel.CancelLoadAndClearFiles();
            if ((instanceContentFrame.Content as GenericFileBrowser) != null)
            {
                var instance = App.selectedTabInstance.instanceViewModel;
                string parentDirectoryOfPath = null;
                // Check that there isn't a slash at the end
                if((instance.Universal.path.Count() - 1) - instance.Universal.path.LastIndexOf("\\") > 0)
                {
                    parentDirectoryOfPath = instance.Universal.path.Remove(instance.Universal.path.LastIndexOf("\\"));
                }
                else  // Slash found at end
                {
                    var currentPathWithoutEndingSlash = instance.Universal.path.Remove(instance.Universal.path.LastIndexOf("\\"));
                    parentDirectoryOfPath = currentPathWithoutEndingSlash.Remove(currentPathWithoutEndingSlash.LastIndexOf("\\"));
                }

                var CurrentTabInstance = App.selectedTabInstance;
                if (parentDirectoryOfPath == ProHome.DesktopPath)
                {
                    CurrentTabInstance.locationsList.SelectedIndex = 1;
                    CurrentTabInstance.PathText.Text = "Desktop";
                }
                else if (parentDirectoryOfPath == ProHome.DownloadsPath)
                {
                    CurrentTabInstance.locationsList.SelectedIndex = 2;
                    CurrentTabInstance.PathText.Text = "Downloads";
                }
                else if (parentDirectoryOfPath == ProHome.DocumentsPath)
                {
                    CurrentTabInstance.locationsList.SelectedIndex = 3;
                    CurrentTabInstance.PathText.Text = "Documents";
                }
                else if (parentDirectoryOfPath == ProHome.PicturesPath)
                {
                    CurrentTabInstance.locationsList.SelectedIndex = 4;
                    CurrentTabInstance.PathText.Text = "Pictures";
                }
                else if (parentDirectoryOfPath == ProHome.MusicPath)
                {
                    CurrentTabInstance.locationsList.SelectedIndex = 5;
                    CurrentTabInstance.PathText.Text = "Music";
                }
                else if (parentDirectoryOfPath == ProHome.VideosPath)
                {
                    CurrentTabInstance.locationsList.SelectedIndex = 6;
                    CurrentTabInstance.PathText.Text = "Videos";
                }
                else if (parentDirectoryOfPath == ProHome.OneDrivePath)
                {
                    CurrentTabInstance.drivesList.SelectedItem = CurrentTabInstance.drivesList.Items.Where(x => (x as DriveItem).tag.ToString() == "OneDrive").First();
                    CurrentTabInstance.PathText.Text = "OneDrive";
                }
                else
                {
                    if (parentDirectoryOfPath.Contains("C:\\") || parentDirectoryOfPath.Contains("c:\\"))
                    {
                        CurrentTabInstance.drivesList.SelectedItem = CurrentTabInstance.drivesList.Items.Where(x => (x as DriveItem).tag == "C:\\").First();
                    }
                    else
                    {
                        foreach (DriveItem drive in CurrentTabInstance.drivesList.Items)
                        {
                            if (drive.tag.ToString().Contains(parentDirectoryOfPath.Split("\\")[0]))
                            {
                                CurrentTabInstance.drivesList.SelectedItem = drive;
                                break;
                            }
                        }

                    }
                    CurrentTabInstance.PathText.Text = parentDirectoryOfPath + "\\";
                    instanceContentFrame.Navigate(typeof(GenericFileBrowser), parentDirectoryOfPath + "\\", new SuppressNavigationTransitionInfo());
                    return;
                }
                instanceContentFrame.Navigate(typeof(GenericFileBrowser), parentDirectoryOfPath, new SuppressNavigationTransitionInfo());

            }
            else if ((instanceContentFrame.Content as PhotoAlbum) != null)
            {
                var instance = App.selectedTabInstance.instanceViewModel;
                string parentDirectoryOfPath = null;
                // Check that there isn't a slash at the end
                if ((instance.Universal.path.Count() - 1) - instance.Universal.path.LastIndexOf("\\") > 0)
                {
                    parentDirectoryOfPath = instance.Universal.path.Remove(instance.Universal.path.LastIndexOf("\\"));
                }
                else  // Slash found at end
                {
                    var currentPathWithoutEndingSlash = instance.Universal.path.Remove(instance.Universal.path.LastIndexOf("\\"));
                    parentDirectoryOfPath = currentPathWithoutEndingSlash.Remove(currentPathWithoutEndingSlash.LastIndexOf("\\"));
                }

                var CurrentTabInstance = App.selectedTabInstance;
                if (parentDirectoryOfPath == ProHome.DesktopPath)
                {
                    CurrentTabInstance.locationsList.SelectedIndex = 1;
                    CurrentTabInstance.PathText.Text = "Desktop";
                }
                else if (parentDirectoryOfPath == ProHome.DownloadsPath)
                {
                    CurrentTabInstance.locationsList.SelectedIndex = 2;
                    CurrentTabInstance.PathText.Text = "Downloads";
                }
                else if (parentDirectoryOfPath == ProHome.DocumentsPath)
                {
                    CurrentTabInstance.locationsList.SelectedIndex = 3;
                    CurrentTabInstance.PathText.Text = "Documents";
                }
                else if (parentDirectoryOfPath == ProHome.PicturesPath)
                {
                    CurrentTabInstance.locationsList.SelectedIndex = 4;
                    CurrentTabInstance.PathText.Text = "Pictures";
                }
                else if (parentDirectoryOfPath == ProHome.MusicPath)
                {
                    CurrentTabInstance.locationsList.SelectedIndex = 5;
                    CurrentTabInstance.PathText.Text = "Music";
                }
                else if (parentDirectoryOfPath == ProHome.VideosPath)
                {
                    CurrentTabInstance.locationsList.SelectedIndex = 6;
                    CurrentTabInstance.PathText.Text = "Videos";
                }
                else if (parentDirectoryOfPath == ProHome.OneDrivePath)
                {
                    CurrentTabInstance.drivesList.SelectedItem = CurrentTabInstance.drivesList.Items.Where(x => (x as DriveItem).tag.ToString() == "OneDrive").First();
                    CurrentTabInstance.PathText.Text = "OneDrive";
                }
                else
                {
                    if (parentDirectoryOfPath.Contains("C:\\") || parentDirectoryOfPath.Contains("c:\\"))
                    {
                        CurrentTabInstance.drivesList.SelectedItem = CurrentTabInstance.drivesList.Items.Where(x => (x as DriveItem).tag == "C:\\").First();
                    }
                    else
                    {
                        foreach (DriveItem drive in CurrentTabInstance.drivesList.Items)
                        {
                            if (drive.tag.ToString().Contains(parentDirectoryOfPath.Split("\\")[0]))
                            {
                                CurrentTabInstance.drivesList.SelectedItem = drive;
                                break;
                            }
                        }

                    }
                    CurrentTabInstance.PathText.Text = parentDirectoryOfPath + "\\";
                    instanceContentFrame.Navigate(typeof(PhotoAlbum), parentDirectoryOfPath + "\\", new SuppressNavigationTransitionInfo());
                    return;
                }
                instanceContentFrame.Navigate(typeof(PhotoAlbum), parentDirectoryOfPath, new SuppressNavigationTransitionInfo());
            }
        }
    }

    public class PathBoxItem
    {
        public string Title { get; set; }
        public string Path { get; set; }
    }
}
