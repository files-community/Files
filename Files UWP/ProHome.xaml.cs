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
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Files
{
    /// <summary>
    /// Code to accompany Project Mumbai layout
    /// </summary>
    public sealed partial class ProHome : Page
    {
        public ContentDialog permissionBox;
        public ContentDialog propertiesBox;
        public ListView locationsList;
        public ListView drivesList;
        public Frame accessibleContentFrame;
        public Frame accessiblePropertiesFrame;
        public Button BackButton;
        public Button ForwardButton;
        public Button accessiblePasteButton;
        public Button RefreshButton;
        public Button AddItemButton;
        public ContentDialog AddItemBox;
        public ContentDialog NameBox;
        public TextBox inputFromRename;
        public TextBox PathBox;
        public string inputForRename;
        public static string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        public static string DocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public static string DownloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
        public static string OneDrivePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive";
        public static string PicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        public static string MusicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        public static string VideosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        public TeachingTip RibbonTeachingTip;

        public BackState BS { get; set; } = new BackState();
        public ForwardState FS { get; set; } = new ForwardState();
        public DisplayedPathText PathText { get; set; } = new DisplayedPathText();
        public PasteState PS { get; set; } = new PasteState();
        public Interacts.Home.HomeItemsState HomeItems { get; set; } = new Interacts.Home.HomeItemsState();
        public Interacts.Share.ShareItemsState ShareItems { get; set; } = new Interacts.Share.ShareItemsState();
        public Interacts.Layout.LayoutItemsState LayoutItems { get; set; } = new Interacts.Layout.LayoutItemsState();
        public Interacts.AlwaysPresentCommandsState AlwaysPresentCommands { get; set; } = new Interacts.AlwaysPresentCommandsState();

        public ProHome()
        {
            this.InitializeComponent();
            permissionBox = PermissionDialog;
            locationsList = LocationsList;
            drivesList = DrivesList;
            accessibleContentFrame = ItemDisplayFrame;
            accessiblePropertiesFrame = propertiesFrame;
            AddItemBox = AddDialog;
            NameBox = NameDialog;
            propertiesBox = PropertiesDialog;
            inputFromRename = RenameInput;
            BackButton = Back;
            ForwardButton = Forward;
            RefreshButton = Refresh;
            AddItemButton = addItemButton;
            PathBox = VisiblePath;
            PathText.Text = "Favorites";
            accessiblePasteButton = PasteButton;
            LocationsList.SelectedIndex = 0;
            RibbonTeachingTip = RibbonTip;
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            PopulatePinnedSidebarItems();
            PopulateNavViewWithExternalDrives();
            BackButton.Click += NavigationActions.Back_Click;
            ForwardButton.Click += NavigationActions.Forward_Click;
            RefreshButton.Click += NavigationActions.Refresh_Click;
            if(ribbonShadow != null)
            {
                ribbonShadow.Receivers.Add(RibbonShadowSurface);
                Ribbon.Translation += new System.Numerics.Vector3(0, 0, 4);
            }

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
            

        }



        List<string> LinesToRemoveFromFile = new List<string>();

        public async void PopulatePinnedSidebarItems()
        {
            StorageFile ListFile;
            StorageFolder cacheFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;
            try
            {
                ListFile = await cacheFolder.GetFileAsync("PinnedItems.txt");
            }
            catch(FileNotFoundException)
            {
                ListFile = await cacheFolder.CreateFileAsync("PinnedItems.txt");
            }
            
            if(ListFile != null)
            {
                var ListFileLines = await FileIO.ReadLinesAsync(ListFile);
                foreach (string s in ListFileLines)
                {
                    try
                    {
                        StorageFolder fol = await StorageFolder.GetFolderFromPathAsync(s);
                        var name = fol.DisplayName;
                        var content = name;
                        var icon = "\uE8B7";

                        FontFamily fontFamily = new FontFamily("Segoe MDL2 Assets");
                        FontIcon fontIcon = new FontIcon()
                        {
                            FontSize = 16,
                            FontFamily = fontFamily,
                            Glyph = icon
                        };

                        TextBlock text = new TextBlock()
                        {
                            Text = content,
                            FontSize = 12
                        };

                        StackPanel stackPanel = new StackPanel()
                        {
                            Spacing = 15,
                            Orientation = Orientation.Horizontal
                        };

                        stackPanel.Children.Add(fontIcon);
                        stackPanel.Children.Add(text);
                        MenuFlyout flyout = new MenuFlyout();
                        MenuFlyoutItem flyoutItem = new MenuFlyoutItem()
                        {
                            Text = "Unpin item"
                        };
                        flyoutItem.Click += FlyoutItem_Click;
                        flyout.Items.Add(flyoutItem);
                        bool isDuplicate = false;
                        foreach (ListViewItem lvi in LocationsList.Items)
                        {
                            if (lvi.Tag.ToString() == s)
                            {
                                isDuplicate = true;

                            }
                        }

                        if (!isDuplicate)
                        {
                            ListViewItem newItem = new ListViewItem();
                            newItem.Content = stackPanel;
                            newItem.Tag = s;
                            newItem.ContextFlyout = flyout;
                            newItem.IsRightTapEnabled = true;
                            newItem.RightTapped += NewItem_RightTapped;
                            LocationsList.Items.Add(newItem);
                        }
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                    catch (FileNotFoundException e)
                    {
                        Debug.WriteLine("Pinned item was deleted and will be removed from the file lines list soon: " + e.Message);
                        LinesToRemoveFromFile.Add(s);
                    }
                    catch (System.Runtime.InteropServices.COMException e)
                    {
                        Debug.WriteLine("Pinned item's drive was ejected and will be removed from the file lines list soon: " + e.Message);
                        LinesToRemoveFromFile.Add(s);
                    }
                }

                foreach (string path in LinesToRemoveFromFile)
                {
                    ListFileLines.Remove(path);
                }
                await FileIO.WriteLinesAsync(ListFile, ListFileLines);
                ListFileLines = await FileIO.ReadLinesAsync(ListFile);

                // Remove unpinned items from sidebar
                foreach (ListViewItem location in LocationsList.Items)
                {
                    if (!(location.Tag.ToString() == "Favorites" || location.Tag.ToString() == "Desktop" || location.Tag.ToString() == "Documents" || location.Tag.ToString() == "Downloads" || location.Tag.ToString() == "Pictures" || location.Tag.ToString() == "Music" || location.Tag.ToString() == "Videos"))
                    {

                        if (!ListFileLines.Contains(location.Tag.ToString()))
                        {
                            if(LocationsList.SelectedItem == location)
                            {
                                LocationsList.SelectedIndex = 0;
                                accessibleContentFrame.Navigate(typeof(YourHome));
                                PathText.Text = "Favorites";
                                LayoutItems.isEnabled = false;
                            }
                            LocationsList.Items.Remove(location);
                        }
                    }
                }
                LinesToRemoveFromFile.Clear();
            }
        }

        ListViewItem rightClickedItem;

        private void NewItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            rightClickedItem = sender as ListViewItem;
        }

        public async void PopulateNavViewWithExternalDrives()
        {
            var knownRemDevices = new ObservableCollection<string>();
            foreach (var f in await KnownFolders.RemovableDevices.GetFoldersAsync())
            {
                var path = f.Path;
                knownRemDevices.Add(path);
            }

            var driveLetters = DriveInfo.GetDrives().Select(x => x.RootDirectory.Root).ToList();

            if (!driveLetters.Any()) return;

            driveLetters.ToList().ForEach(roots =>
            {
                try
                {
                    if (roots.Name == @"C:\") return;
                    var content = string.Empty;
                    string icon;
                    if (knownRemDevices.Contains(roots.Name))
                    {
                        content = $"Removable Drive ({roots.Name})";
                        icon = "\uE88E";
                    }
                    else
                    {
                        content = $"Local Disk ({roots.Name})";
                        icon = "\uEDA2";
                    }
                    FontFamily fontFamily = new FontFamily("Segoe MDL2 Assets");
                    FontIcon fontIcon = new FontIcon()
                    {
                        FontSize = 16,
                        FontFamily = fontFamily,
                        Glyph = icon
                    };

                    TextBlock text = new TextBlock()
                    {
                        Text = content,
                        FontSize = 12
                    };

                    StackPanel stackPanel = new StackPanel()
                    {
                        Spacing = 15,
                        Orientation = Orientation.Horizontal
                    };

                    stackPanel.Children.Add(fontIcon);
                    stackPanel.Children.Add(text);
                    DrivesList.Items.Add(new ListViewItem()
                    {
                        Content = stackPanel,
                        Tag = roots.Name
                    });
                }
                catch (UnauthorizedAccessException e)
                {
                    Debug.WriteLine(e.Message);
                }
            });
        }

        private async void FlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            StorageFolder cacheFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;
            var ListFile = await cacheFolder.GetFileAsync("PinnedItems.txt");
            var ListFileLines = await FileIO.ReadLinesAsync(ListFile);
            foreach (string s in ListFileLines)
            {
                if(s == rightClickedItem.Tag.ToString())
                {
                    LinesToRemoveFromFile.Add(s);
                    PopulatePinnedSidebarItems();
                    return;
                }
            }
        }

        private void NameDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            inputForRename = inputFromRename.Text;
        }

        private void VisiblePath_TextChanged(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                var PathBox = (sender as TextBox);
                var CurrentInput = PathBox.Text;
                if (accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
                {
                    var contentInstance = (this.accessibleContentFrame.Content as GenericFileBrowser).instanceViewModel;
                    CheckPathInput<GenericFileBrowser>(contentInstance, CurrentInput);
                }
                else if (accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
                {
                    var contentInstance = (this.accessibleContentFrame.Content as PhotoAlbum).instanceViewModel;
                    CheckPathInput<PhotoAlbum>(contentInstance, CurrentInput);
                }
                else if (accessibleContentFrame.SourcePageType == typeof(YourHome))
                {
                    var contentInstance = (this.accessibleContentFrame.Content as YourHome).instanceViewModel;
                    CheckPathInput<YourHome>(contentInstance, CurrentInput);
                }

            }
        }

        public async void CheckPathInput<T>(ItemViewModel<T> instance, string CurrentInput) where T : class
        {
            if (CurrentInput != instance.Universal.path)
            {
                instance.CancelLoadAndClearFiles();
                HomeItems.isEnabled = false;
                ShareItems.isEnabled = false;

                if (CurrentInput == "Favorites" || CurrentInput == "favorites")
                {
                    this.accessibleContentFrame.Navigate(typeof(YourHome));
                    PathText.Text = "Favorites";
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
                                    await (this.accessibleContentFrame.Content as GenericFileBrowser).instanceInteraction.LaunchExe(CurrentInput);
                                }
                                else if (this.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
                                {
                                    await (this.accessibleContentFrame.Content as PhotoAlbum).instanceInteraction.LaunchExe(CurrentInput);
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
            ListViewItem clickedItem = Interaction<ProHome>.FindParent<ListViewItem>(e.ClickedItem as DependencyObject);
            ItemViewModel<GenericFileBrowser> instance = null;

            if (clickedItem.Tag.ToString() == "Favorites")
            {
                ItemDisplayFrame.Navigate(typeof(YourHome));
                PathText.Text = "Favorites";
                ItemViewModel<YourHome> homeInstance = (this.accessibleContentFrame.Content as YourHome).instanceViewModel;
                homeInstance.CancelLoadAndClearFiles();
                HomeItems.isEnabled = false;
                ShareItems.isEnabled = false;
                if (DrivesList.SelectedItem != null)
                {
                    DrivesList.SelectedItem = null;
                    LayoutItems.isEnabled = false;
                }
                LayoutItems.isEnabled = false;
            }
            else if (clickedItem.Tag.ToString() == "Desktop")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), DesktopPath);
                PathText.Text = "Desktop";
                instance = (this.accessibleContentFrame.Content as GenericFileBrowser).instanceViewModel;
                HomeItems.isEnabled = false;
                ShareItems.isEnabled = false;
                if (DrivesList.SelectedItem != null)
                {
                    DrivesList.SelectedItem = null;
                    LayoutItems.isEnabled = false;
                }
                LayoutItems.isEnabled = true;
            }
            else if (clickedItem.Tag.ToString() == "Downloads")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), DownloadsPath);
                PathText.Text = "Downloads";
                instance = (this.accessibleContentFrame.Content as GenericFileBrowser).instanceViewModel;
                HomeItems.isEnabled = false;
                ShareItems.isEnabled = false;
                if (DrivesList.SelectedItem != null)
                {
                    DrivesList.SelectedItem = null;
                    LayoutItems.isEnabled = false;
                }
                LayoutItems.isEnabled = true;
            }
            else if (clickedItem.Tag.ToString() == "Documents")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), DocumentsPath);
                PathText.Text = "Documents";
                instance = (this.accessibleContentFrame.Content as GenericFileBrowser).instanceViewModel;
                HomeItems.isEnabled = false;
                ShareItems.isEnabled = false;
                if (DrivesList.SelectedItem != null)
                {
                    DrivesList.SelectedItem = null;
                    LayoutItems.isEnabled = false;
                }
                LayoutItems.isEnabled = true;
            }
            else if (clickedItem.Tag.ToString() == "Pictures")
            {
                ItemDisplayFrame.Navigate(typeof(PhotoAlbum), PicturesPath);
                PathText.Text = "Pictures";
                ItemViewModel<PhotoAlbum> PAInstance = (this.accessibleContentFrame.Content as PhotoAlbum).instanceViewModel;
                HomeItems.isEnabled = false;
                ShareItems.isEnabled = false;
                if (DrivesList.SelectedItem != null)
                {
                    DrivesList.SelectedItem = null;
                    LayoutItems.isEnabled = false;
                }
                LayoutItems.isEnabled = true;
            }
            else if (clickedItem.Tag.ToString() == "Music")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), MusicPath);
                PathText.Text = "Music";
                instance = (this.accessibleContentFrame.Content as GenericFileBrowser).instanceViewModel;
                HomeItems.isEnabled = false;
                ShareItems.isEnabled = false;
                if (DrivesList.SelectedItem != null)
                {
                    DrivesList.SelectedItem = null;
                    LayoutItems.isEnabled = false;
                }
                LayoutItems.isEnabled = true;
            }
            else if (clickedItem.Tag.ToString() == "Videos")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), VideosPath);
                PathText.Text = "Videos";
                instance = (this.accessibleContentFrame.Content as GenericFileBrowser).instanceViewModel;
                HomeItems.isEnabled = false;
                ShareItems.isEnabled = false;
                if (DrivesList.SelectedItem != null)
                {
                    DrivesList.SelectedItem = null;
                    LayoutItems.isEnabled = false;
                }
                LayoutItems.isEnabled = true;
            }
            else
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), clickedItem.Tag);
                PathText.Text = clickedItem.Tag.ToString();
                instance = (this.accessibleContentFrame.Content as GenericFileBrowser).instanceViewModel;
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
            if (this.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                var instance = (this.accessibleContentFrame.Content as GenericFileBrowser).instanceViewModel;
                HomeItems.isEnabled = false;
                ShareItems.isEnabled = false;
                if (LocationsList.SelectedItem != null)
                {
                    LocationsList.SelectedItem = null;
                    LayoutItems.isEnabled = false;
                }

                ListViewItem clickedItem = Interaction<ProHome>.FindParent<ListViewItem>(e.ClickedItem as DependencyObject);

                if (clickedItem.Tag.ToString() == "LocalDisk")
                {
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), @"C:\");
                    PathText.Text = @"Local Disk (C:\)";
                    LayoutItems.isEnabled = true;
                }
                else if (clickedItem.Tag.ToString() == "OneDrive")
                {
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), OneDrivePath);
                    PathText.Text = "OneDrive";
                    LayoutItems.isEnabled = true;
                }
                else
                {
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), clickedItem.Tag.ToString());
                    PathText.Text = clickedItem.Tag.ToString();
                    LayoutItems.isEnabled = true;
                }
            }
            else if (this.accessibleContentFrame.SourcePageType == typeof(YourHome))
            {
                var instance = (this.accessibleContentFrame.Content as YourHome).instanceViewModel;
                HomeItems.isEnabled = false;
                ShareItems.isEnabled = false;
                if (LocationsList.SelectedItem != null)
                {
                    LocationsList.SelectedItem = null;
                    LayoutItems.isEnabled = false;
                }

                ListViewItem clickedItem = Interaction<ProHome>.FindParent<ListViewItem>(e.ClickedItem as DependencyObject);

                if (clickedItem.Tag.ToString() == "LocalDisk")
                {
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), @"C:\");
                    PathText.Text = @"Local Disk (C:\)";
                    LayoutItems.isEnabled = true;
                }
                else if (clickedItem.Tag.ToString() == "OneDrive")
                {
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), OneDrivePath);
                    PathText.Text = "OneDrive";
                    LayoutItems.isEnabled = true;
                }
                else
                {
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), clickedItem.Tag.ToString());
                    PathText.Text = clickedItem.Tag.ToString();
                    LayoutItems.isEnabled = true;
                }
            }
            else if (this.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                var instance = (this.accessibleContentFrame.Content as PhotoAlbum).instanceViewModel;
                HomeItems.isEnabled = false;
                ShareItems.isEnabled = false;
                if (LocationsList.SelectedItem != null)
                {
                    LocationsList.SelectedItem = null;
                    LayoutItems.isEnabled = false;
                }

                ListViewItem clickedItem = Interaction<ProHome>.FindParent<ListViewItem>(e.ClickedItem as DependencyObject);

                if (clickedItem.Tag.ToString() == "LocalDisk")
                {
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), @"C:\");
                    PathText.Text = @"Local Disk (C:\)";
                    LayoutItems.isEnabled = true;
                }
                else if (clickedItem.Tag.ToString() == "OneDrive")
                {
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), OneDrivePath);
                    PathText.Text = "OneDrive";
                    LayoutItems.isEnabled = true;
                }
                else
                {
                    ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), clickedItem.Tag.ToString());
                    PathText.Text = clickedItem.Tag.ToString();
                    LayoutItems.isEnabled = true;
                }
            }
                
        }

        private async void PermissionDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess"));

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
                (this.accessibleContentFrame.Content as GenericFileBrowser).instanceInteraction.CutItem_Click(null, null);
            }
            else if (this.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                (this.accessibleContentFrame.Content as PhotoAlbum).instanceInteraction.CutItem_Click(null, null);
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                (this.accessibleContentFrame.Content as GenericFileBrowser).instanceInteraction.CopyItem_ClickAsync(null, null);
            }
            else if (this.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                (this.accessibleContentFrame.Content as PhotoAlbum).instanceInteraction.CopyItem_ClickAsync(null, null);
            }
        }

        private void PasteButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                (this.accessibleContentFrame.Content as GenericFileBrowser).instanceInteraction.PasteItem_ClickAsync(null, null);
            }
            else if (this.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                (this.accessibleContentFrame.Content as PhotoAlbum).instanceInteraction.PasteItem_ClickAsync(null, null);
            }
        }

        private void CopyPathButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                (this.accessibleContentFrame.Content as GenericFileBrowser).instanceInteraction.GetPath_Click(null, null);
            }
            else if (this.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                (this.accessibleContentFrame.Content as PhotoAlbum).instanceInteraction.GetPath_Click(null, null);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                (this.accessibleContentFrame.Content as GenericFileBrowser).instanceInteraction.DeleteItem_Click(null, null);
            }
            else if (this.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                (this.accessibleContentFrame.Content as PhotoAlbum).instanceInteraction.DeleteItem_Click(null, null);
            }
        }

        private void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                (this.accessibleContentFrame.Content as GenericFileBrowser).instanceInteraction.RenameItem_Click(null, null);
            }
            else if (this.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                (this.accessibleContentFrame.Content as PhotoAlbum).instanceInteraction.RenameItem_Click(null, null);
            }
        }

        private async void AddItem_Click(object sender, RoutedEventArgs e)
        {
            if (accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                //addItemPageInstance = new AddItem(accessibleContentFrame.Content as GenericFileBrowser, null);
                AddDialogFrame.Navigate(typeof(AddItem), accessibleContentFrame.Content as GenericFileBrowser, new SuppressNavigationTransitionInfo());
            }
            else if (accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                //addItemPageInstance = new AddItem(null, accessibleContentFrame.Content as PhotoAlbum);
                AddDialogFrame.Navigate(typeof(AddItem), accessibleContentFrame.Content as PhotoAlbum, new SuppressNavigationTransitionInfo());
            }
            await AddItemBox.ShowAsync();
        }

        private void OpenWithButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                (this.accessibleContentFrame.Content as GenericFileBrowser).instanceInteraction.OpenItem_Click(null, null);
            }
            else if (this.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                (this.accessibleContentFrame.Content as PhotoAlbum).instanceInteraction.OpenItem_Click(null, null);
            }
        }

        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                (this.accessibleContentFrame.Content as GenericFileBrowser).instanceInteraction.ShareItem_Click(null, null);
            }
            else if (this.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                (this.accessibleContentFrame.Content as PhotoAlbum).instanceInteraction.ShareItem_Click(null, null);
            }
        }

        private async void LayoutButton_Click(object sender, RoutedEventArgs e)
        {
            await LayoutDialog.ShowAsync();
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {
                (this.accessibleContentFrame.Content as GenericFileBrowser).instanceInteraction.SelectAllItems();
            }
            else if (this.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                (this.accessibleContentFrame.Content as PhotoAlbum).instanceInteraction.SelectAllItems();
            }
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void NameDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            inputForRename = null;
        }
        //AppWindow propertiesWindow;
        public async void ShowPropertiesButton_Click(object sender, RoutedEventArgs e)
        {
            var instance = (this.accessibleContentFrame.Content as GenericFileBrowser).instanceViewModel;
            if (this.accessibleContentFrame.SourcePageType == typeof(GenericFileBrowser))
            {

                //if (ApiInformation.IsTypePresent("Windows.UI.WindowManagement.AppWindow"))
                //{
                //    propertiesWindow = await AppWindow.TryCreateAsync();
                //    Frame propertiesWindowContent = new Frame();
                //    propertiesWindowContent.Navigate(typeof(Properties), ((ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Content as GenericFileBrowser).data.SelectedItem as ListedItem).FilePath, new SuppressNavigationTransitionInfo());
                //    ElementCompositionPreview.SetAppWindowContent(propertiesWindow, propertiesWindowContent);
                //    propertiesWindow.Title = "Properties";
                //    await propertiesWindow.TryShowAsync();

                //}
                //else
                //{

                //}

                
            }
            else if (this.accessibleContentFrame.SourcePageType == typeof(PhotoAlbum))
            {
                propertiesFrame.Navigate(typeof(Properties), ((ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Content as PhotoAlbum).gv.SelectedItem as ListedItem).FilePath, new SuppressNavigationTransitionInfo());
            }

        }

        public void PropertiesWindow_CancelButton_Click(object sender, RoutedEventArgs e)
        {
            //propertiesWindow.RequestSize(new Windows.Foundation.Size(200, 450));
        }

        private void RibbonTip_Loaded(object sender, RoutedEventArgs e)
        {
            if (ApplicationData.Current.LocalSettings.Values["HasBeenWelcomed"] == null)
            {
                this.RibbonTeachingTip.IsOpen = true;
                ApplicationData.Current.LocalSettings.Values["HasBeenWelcomed"] = true;
            }
            else
            {
                this.RibbonTeachingTip.IsOpen = false;
            }

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            accessibleContentFrame.Navigate(typeof(YourHome), null, new SuppressNavigationTransitionInfo());
            this.Loaded -= Page_Loaded;
        }

        private void ItemDisplayFrame_Navigating(object sender, Windows.UI.Xaml.Navigation.NavigatingCancelEventArgs e)
        {

        }

        private void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            SearchBox.Focus(FocusState.Programmatic);
        }
    }
    public class NavigationActions
    {
        public async static void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if ((ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Content as GenericFileBrowser) != null)
                {
                    var ContentOwnedViewModelInstance = (ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Content as GenericFileBrowser).instanceViewModel;
                    ContentOwnedViewModelInstance.CancelLoadAndClearFiles();
                    ContentOwnedViewModelInstance.AddItemsToCollectionAsync(ContentOwnedViewModelInstance.Universal.path, (ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Content as GenericFileBrowser).GFBPageName);
                }
                else if ((ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Content as PhotoAlbum) != null)
                {
                    var ContentOwnedViewModelInstance = (ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Content as PhotoAlbum).instanceViewModel;
                    ContentOwnedViewModelInstance.CancelLoadAndClearFiles();
                    ContentOwnedViewModelInstance.AddItemsToCollectionAsync(ContentOwnedViewModelInstance.Universal.path, (ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Content as PhotoAlbum).PAPageName);
                }

            });
        }

        public static void Back_Click(object sender, RoutedEventArgs e)
        {
            Frame instanceContentFrame = ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame;
            if ((ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Content as GenericFileBrowser) != null)
            {
                var instance = (instanceContentFrame.Content as GenericFileBrowser).instanceViewModel;
                var instanceContent = (instanceContentFrame.Content as GenericFileBrowser);
                instance.CancelLoadAndClearFiles();
                if (instanceContentFrame.CanGoBack)
                {
                    var previousSourcePageType = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1].SourcePageType;
                    var Parameter = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1].Parameter;

                    if (previousSourcePageType == typeof(YourHome))
                    {

                        ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().locationsList.SelectedIndex = 0;
                        ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().PathText.Text = "Favorites";
                    }
                    else
                    {
                        var CurrentTabInstance = ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>();
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
                            CurrentTabInstance.drivesList.SelectedIndex = 1;
                            CurrentTabInstance.PathText.Text = "OneDrive";
                        }
                        else
                        {
                            if (Parameter.ToString().Contains("C:\\") || Parameter.ToString().Contains("c:\\"))
                            {
                                CurrentTabInstance.drivesList.SelectedIndex = 0;
                            }
                            else
                            {
                                foreach (ListViewItem drive in CurrentTabInstance.drivesList.Items)
                                {
                                    if (drive.Tag.ToString().Contains(Parameter.ToString().Split("\\")[0]))
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
            else if ((ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Content as PhotoAlbum) != null)
            {
                var instance = (instanceContentFrame.Content as PhotoAlbum).instanceViewModel;
                var instanceContent = (instanceContentFrame.Content as PhotoAlbum);
                instance.CancelLoadAndClearFiles();
                if (instanceContentFrame.CanGoBack)
                {
                    var previousSourcePageType = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1].SourcePageType;
                    var Parameter = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1].Parameter;

                    if (previousSourcePageType == typeof(YourHome))
                    {

                        ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().locationsList.SelectedIndex = 0;
                        ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().PathText.Text = "Favorites";
                    }
                    else
                    {
                        var CurrentTabInstance = ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>();
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
                            CurrentTabInstance.drivesList.SelectedIndex = 1;
                            CurrentTabInstance.PathText.Text = "OneDrive";
                        }
                        else
                        {
                            if (Parameter.ToString().Contains("C:\\") || Parameter.ToString().Contains("c:\\"))
                            {
                                CurrentTabInstance.drivesList.SelectedIndex = 0;
                            }
                            else
                            {
                                foreach (ListViewItem drive in CurrentTabInstance.drivesList.Items)
                                {
                                    if (drive.Tag.ToString().Contains(Parameter.ToString().Split("\\")[0]))
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
            else if ((ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Content as YourHome) != null)
            {
                var instance = (instanceContentFrame.Content as YourHome).instanceViewModel;
                var instanceContent = (instanceContentFrame.Content as YourHome);
                instance.CancelLoadAndClearFiles();

                if (instanceContentFrame.CanGoBack)
                {
                    var previousSourcePageType = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1].SourcePageType;
                    var Parameter = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1].Parameter;

                    if (previousSourcePageType == typeof(YourHome))
                    {
                        ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().locationsList.SelectedIndex = 0;
                        ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().PathText.Text = "Favorites";
                    }
                    else
                    {
                        var CurrentTabInstance = ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>();
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
                            CurrentTabInstance.drivesList.SelectedIndex = 1;
                            CurrentTabInstance.PathText.Text = "OneDrive";
                        }
                        else
                        {
                            if (Parameter.ToString().Contains("C:\\") || Parameter.ToString().Contains("c:\\"))
                            {
                                CurrentTabInstance.drivesList.SelectedIndex = 0;
                            }
                            else
                            {
                                foreach (ListViewItem drive in CurrentTabInstance.drivesList.Items)
                                {
                                    if (drive.Tag.ToString().Contains(Parameter.ToString().Split("\\")[0]))
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
            Frame instanceContentFrame = ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame;
            if ((ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Content as GenericFileBrowser) != null)
            {
                var instance = (instanceContentFrame.Content as GenericFileBrowser).instanceViewModel;
                var instanceContent = (instanceContentFrame.Content as GenericFileBrowser);
                instance.CancelLoadAndClearFiles();

                if (instanceContentFrame.CanGoForward)
                {
                    var previousSourcePageType = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].SourcePageType;
                    var Parameter = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].Parameter;

                    if (previousSourcePageType == typeof(YourHome))
                    {
                        ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().locationsList.SelectedIndex = 0;
                        ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().PathText.Text = "Favorites";
                    }
                    else
                    {
                        var CurrentTabInstance = ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>();
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
                            CurrentTabInstance.drivesList.SelectedIndex = 1;
                            CurrentTabInstance.PathText.Text = "OneDrive";
                        }
                        else
                        {
                            if (Parameter.ToString().Contains("C:\\") || Parameter.ToString().Contains("c:\\"))
                            {
                                CurrentTabInstance.drivesList.SelectedIndex = 0;
                            }
                            else
                            {
                                foreach (ListViewItem drive in CurrentTabInstance.drivesList.Items)
                                {
                                    if (drive.Tag.ToString().Contains(Parameter.ToString().Split("\\")[0]))
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
            else if ((ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Content as PhotoAlbum) != null)
            {
                var instance = (instanceContentFrame.Content as PhotoAlbum).instanceViewModel;
                var instanceContent = (instanceContentFrame.Content as PhotoAlbum);
                instance.CancelLoadAndClearFiles();

                if (instanceContentFrame.CanGoForward)
                {
                    var previousSourcePageType = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].SourcePageType;
                    var Parameter = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].Parameter;

                    if (previousSourcePageType == typeof(YourHome))
                    {
                        ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().locationsList.SelectedIndex = 0;
                        ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().PathText.Text = "Favorites";
                    }
                    else
                    {
                        var CurrentTabInstance = ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>();
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
                            CurrentTabInstance.drivesList.SelectedIndex = 1;
                            CurrentTabInstance.PathText.Text = "OneDrive";
                        }
                        else
                        {
                            if (Parameter.ToString().Contains("C:\\") || Parameter.ToString().Contains("c:\\"))
                            {
                                CurrentTabInstance.drivesList.SelectedIndex = 0;
                            }
                            else
                            {
                                foreach (ListViewItem drive in CurrentTabInstance.drivesList.Items)
                                {
                                    if (drive.Tag.ToString().Contains(Parameter.ToString().Split("\\")[0]))
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
            else if ((ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().accessibleContentFrame.Content as YourHome) != null)
            {
                var instance = (instanceContentFrame.Content as YourHome).instanceViewModel;
                var instanceContent = (instanceContentFrame.Content as YourHome);
                instance.CancelLoadAndClearFiles();

                if (instanceContentFrame.CanGoForward)
                {
                    var previousSourcePageType = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].SourcePageType;
                    var Parameter = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].Parameter;

                    if (previousSourcePageType == typeof(YourHome))
                    {
                        ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().locationsList.SelectedIndex = 0;
                        ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>().PathText.Text = "Favorites";
                    }
                    else
                    {
                        var CurrentTabInstance = ItemViewModel<ProHome>.GetCurrentSelectedTabInstance<ProHome>();
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
                            CurrentTabInstance.drivesList.SelectedIndex = 1;
                            CurrentTabInstance.PathText.Text = "OneDrive";
                        }
                        else
                        {
                            if (Parameter.ToString().Contains("C:\\") || Parameter.ToString().Contains("c:\\"))
                            {
                                CurrentTabInstance.drivesList.SelectedIndex = 0;
                            }
                            else
                            {
                                foreach (ListViewItem drive in CurrentTabInstance.drivesList.Items)
                                {
                                    if (drive.Tag.ToString().Contains(Parameter.ToString().Split("\\")[0]))
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
    }
}
