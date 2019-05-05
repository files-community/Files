using Files.Filesystem;
using Files.Interacts;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
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
    /// Project Mumbai - Pre-release Dense UI Design
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
        public string inputForRename;
        public static string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        public static string DocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public static string DownloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
        public static string OneDrivePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive";
        public static string PicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        public static string MusicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        public static string VideosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        public TeachingTip RibbonTeachingTip;
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
            accessiblePasteButton = PasteButton;
            LocationsList.SelectedIndex = 0;
            RibbonTeachingTip = RibbonTip;
            PopulateNavViewWithExternalDrives();
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

        private void NameDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            inputForRename = inputFromRename.Text;
        }

        private async void VisiblePath_TextChanged(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                var PathBox = (sender as TextBox);
                var CurrentInput = PathBox.Text;
                if (CurrentInput != App.ViewModel.Universal.path)
                {
                    App.ViewModel.CancelLoadAndClearFiles();
                    App.HomeItems.isEnabled = false;
                    App.ShareItems.isEnabled = false;
                    if (CurrentInput == "Favorites" || CurrentInput == "favorites")
                    {
                        this.accessibleContentFrame.Navigate(typeof(YourHome));
                        App.PathText.Text = "Favorites";
                        App.LayoutItems.isEnabled = false;
                    }
                    else if (CurrentInput == "Desktop" || CurrentInput == "desktop")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        this.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DesktopPath);
                        App.PathText.Text = "Desktop";
                        App.LayoutItems.isEnabled = true;

                    }
                    else if (CurrentInput == "Documents" || CurrentInput == "documents")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        this.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DocumentsPath);
                        App.PathText.Text = "Documents";
                        App.LayoutItems.isEnabled = true;


                    }
                    else if (CurrentInput == "Downloads" || CurrentInput == "downloads")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        this.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DownloadsPath);
                        App.PathText.Text = "Downloads";
                        App.LayoutItems.isEnabled = true;


                    }
                    else if (CurrentInput == "Pictures" || CurrentInput == "pictures")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        this.accessibleContentFrame.Navigate(typeof(PhotoAlbum), PicturesPath);
                        App.PathText.Text = "Pictures";
                        App.LayoutItems.isEnabled = true;

                    }
                    else if (CurrentInput == "Music" || CurrentInput == "music")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        this.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MusicPath);
                        App.PathText.Text = "Music";
                        App.LayoutItems.isEnabled = true;


                    }
                    else if (CurrentInput == "Videos" || CurrentInput == "videos")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        this.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), VideosPath);
                        App.PathText.Text = "Videos";
                        App.LayoutItems.isEnabled = true;


                    }
                    else if (CurrentInput == "OneDrive" || CurrentInput == "Onedrive" || CurrentInput == "onedrive")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        this.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), OneDrivePath);
                        App.PathText.Text = "OneDrive";
                        App.LayoutItems.isEnabled = true;


                    }
                    else
                    {
                        if (CurrentInput.Contains("."))
                        {
                            if (CurrentInput.Contains(".exe") || CurrentInput.Contains(".EXE"))
                            {
                                if (StorageFile.GetFileFromPathAsync(CurrentInput) != null)
                                {
                                    await Interaction.LaunchExe(CurrentInput);
                                    PathBox.Text = App.ViewModel.Universal.path;
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
                                    App.ViewModel.TextState.isVisible = Visibility.Collapsed;
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
                                    PathBox.Text = App.ViewModel.Universal.path;
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
                                App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                                this.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), CurrentInput);
                                App.LayoutItems.isEnabled = true;
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
        }

        private void LocationsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            App.ViewModel.CancelLoadAndClearFiles();
            App.HomeItems.isEnabled = false;
            App.ShareItems.isEnabled = false;
            if (DrivesList.SelectedItem != null)
            {
                DrivesList.SelectedItem = null;
                App.LayoutItems.isEnabled = false;
            }
            var clickedItem = Interaction.FindParent<ListViewItem>(e.ClickedItem as DependencyObject);
            if(clickedItem.Tag.ToString() == "Favorites")
            {
                ItemDisplayFrame.Navigate(typeof(YourHome));
                App.PathText.Text = "Favorites";
                App.LayoutItems.isEnabled = false;
            }
            else if(clickedItem.Tag.ToString() == "Desktop")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), DesktopPath);
                App.PathText.Text = "Desktop";
                App.LayoutItems.isEnabled = true;
            }
            else if(clickedItem.Tag.ToString() == "Downloads")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), DownloadsPath);
                App.PathText.Text = "Downloads";
                App.LayoutItems.isEnabled = true;
            }
            else if(clickedItem.Tag.ToString() == "Documents")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), DocumentsPath);
                App.PathText.Text = "Documents";
                App.LayoutItems.isEnabled = true;
            }
            else if(clickedItem.Tag.ToString() == "Pictures")
            {
                ItemDisplayFrame.Navigate(typeof(PhotoAlbum), PicturesPath);
                App.PathText.Text = "Pictures";
                App.LayoutItems.isEnabled = true;
            }
            else if(clickedItem.Tag.ToString() == "Music")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), MusicPath);
                App.PathText.Text = "Music";
                App.LayoutItems.isEnabled = true;
            }
            else if(clickedItem.Tag.ToString() == "Videos")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), VideosPath);
                App.PathText.Text = "Videos";
                App.LayoutItems.isEnabled = true;
            }

        }

        private void DrivesList_ItemClick(object sender, ItemClickEventArgs e)
        {
            App.ViewModel.CancelLoadAndClearFiles();
            App.HomeItems.isEnabled = false;
            App.ShareItems.isEnabled = false;
            if (LocationsList.SelectedItem != null)
            {
                LocationsList.SelectedItem = null;
                App.LayoutItems.isEnabled = false;
            }
            var clickedItem = Interaction.FindParent<ListViewItem>(e.ClickedItem as DependencyObject);

            if(clickedItem.Tag.ToString() == "LocalDisk")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), @"C:\");
                App.PathText.Text = @"Local Disk (C:\)";
                App.LayoutItems.isEnabled = true;
            }
            else if(clickedItem.Tag.ToString() == "OneDrive")
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), OneDrivePath);
                App.PathText.Text = "OneDrive";
                App.LayoutItems.isEnabled = true;
            }
            else
            {
                ItemDisplayFrame.Navigate(typeof(GenericFileBrowser), clickedItem.Tag.ToString());
                App.PathText.Text = clickedItem.Tag.ToString();
                App.LayoutItems.isEnabled = true;
            }
        }

        private async void PermissionDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess"));

        }
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            App.ViewModel.CancelLoadAndClearFiles();
            if (accessibleContentFrame.CanGoBack)
            {
                var SourcePageType = accessibleContentFrame.BackStack[accessibleContentFrame.BackStack.Count - 1].SourcePageType;
                var Parameter = accessibleContentFrame.BackStack[accessibleContentFrame.BackStack.Count - 1].Parameter;

                if (SourcePageType == typeof(YourHome))
                {
                    locationsList.SelectedIndex = 0;
                    App.PathText.Text = "Favorites";
                }
                else
                {
                    if (Parameter.ToString() == DesktopPath)
                    {
                        locationsList.SelectedIndex = 1;
                        App.PathText.Text = "Desktop";
                    }
                    else if (Parameter.ToString() == DownloadsPath)
                    {
                        locationsList.SelectedIndex = 2;
                        App.PathText.Text = "Downloads";
                    }
                    else if (Parameter.ToString() == DocumentsPath)
                    {
                        locationsList.SelectedIndex = 3;
                        App.PathText.Text = "Documents";
                    }
                    else if (Parameter.ToString() == PicturesPath)
                    {
                        locationsList.SelectedIndex = 4;
                        App.PathText.Text = "Pictures";
                    }
                    else if (Parameter.ToString() == MusicPath)
                    {
                        locationsList.SelectedIndex = 5;
                        App.PathText.Text = "Music";
                    }
                    else if (Parameter.ToString() == VideosPath)
                    {
                        locationsList.SelectedIndex = 6;
                        App.PathText.Text = "Videos";
                    }
                    else if (Parameter.ToString() == OneDrivePath)
                    {
                        drivesList.SelectedIndex = 1;
                        App.PathText.Text = "OneDrive";
                    }
                    else
                    {
                        if (Parameter.ToString().Contains("C:\\") || Parameter.ToString().Contains("c:\\"))
                        {
                            drivesList.SelectedIndex = 0;
                        }
                        else
                        {
                            foreach (ListViewItem drive in drivesList.Items)
                            {
                                if (drive.Tag.ToString().Contains(Parameter.ToString().Split("\\")[0]))
                                {
                                    drivesList.SelectedItem = drive;
                                    break;
                                }
                            }
                            
                        }
                        App.PathText.Text = Parameter.ToString();
                    }
                }
                accessibleContentFrame.GoBack();
            }
        }

        private void Forward_Click(object sender, RoutedEventArgs e)
        {
            App.ViewModel.CancelLoadAndClearFiles();
            if (accessibleContentFrame.CanGoForward)
            {
                var SourcePageType = accessibleContentFrame.ForwardStack[accessibleContentFrame.ForwardStack.Count - 1].SourcePageType;
                var Parameter = accessibleContentFrame.ForwardStack[accessibleContentFrame.ForwardStack.Count - 1].Parameter;

                if (SourcePageType == typeof(YourHome))
                {
                    locationsList.SelectedIndex = 0;
                    App.PathText.Text = "Favorites";
                }
                else
                {
                    if (Parameter.ToString() == DesktopPath)
                    {
                        locationsList.SelectedIndex = 1;
                        App.PathText.Text = "Desktop";
                    }
                    else if (Parameter.ToString() == DownloadsPath)
                    {
                        locationsList.SelectedIndex = 2;
                        App.PathText.Text = "Downloads";
                    }
                    else if (Parameter.ToString() == DocumentsPath)
                    {
                        locationsList.SelectedIndex = 3;
                        App.PathText.Text = "Documents";
                    }
                    else if (Parameter.ToString() == PicturesPath)
                    {
                        locationsList.SelectedIndex = 4;
                        App.PathText.Text = "Pictures";
                    }
                    else if (Parameter.ToString() == MusicPath)
                    {
                        locationsList.SelectedIndex = 5;
                        App.PathText.Text = "Music";
                    }
                    else if (Parameter.ToString() == VideosPath)
                    {
                        locationsList.SelectedIndex = 6;
                        App.PathText.Text = "Videos";
                    }
                    else if (Parameter.ToString() == OneDrivePath)
                    {
                        drivesList.SelectedIndex = 1;
                        App.PathText.Text = "OneDrive";
                    }
                    else
                    {
                        if (Parameter.ToString().Contains("C:\\") || Parameter.ToString().Contains("c:\\"))
                        {
                            drivesList.SelectedIndex = 0;
                        }
                        else
                        {
                            foreach (ListViewItem drive in drivesList.Items)
                            {
                                if (drive.Tag.ToString().Contains(Parameter.ToString().Split("\\")[0]))
                                {
                                    drivesList.SelectedItem = drive;
                                    break;
                                }
                            }

                        }
                        App.PathText.Text = Parameter.ToString();
                    }
                }

                accessibleContentFrame.GoForward();
            }
        }

        private void LocationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.AddedItems.Count > 0)
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
            rootFrame.Navigate(typeof(Settings));
        }

        private void CutButton_Click(object sender, RoutedEventArgs e)
        {
            Interaction.CutItem_Click(null, null);
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            Interaction.CopyItem_ClickAsync(null, null);
        }

        private void PasteButton_Click(object sender, RoutedEventArgs e)
        {
            Interaction.PasteItem_ClickAsync(null, null);
        }

        private void CopyPathButton_Click(object sender, RoutedEventArgs e)
        {
            Interaction.GetPath_Click(null, null);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Interaction.DeleteItem_Click(null, null);
        }

        private void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            Interaction.RenameItem_Click(null, null);
        }

        private async void AddItem_Click(object sender, RoutedEventArgs e)
        {
            AddItem.addItemsChoices.SelectedItem = null;
            await this.AddItemBox.ShowAsync();
        }

        private void OpenWithButton_Click(object sender, RoutedEventArgs e)
        {
            Interaction.OpenItem_Click(null, null);
        }

        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            Interaction.ShareItem_Click(null, null);
        }

        private async void LayoutButton_Click(object sender, RoutedEventArgs e)
        {
            await LayoutDialog.ShowAsync();
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            Interaction.SelectAllItems();
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AddDialog_Loaded(object sender, RoutedEventArgs e)
        {
            AddDialogFrame.Navigate(typeof(AddItem), new SuppressNavigationTransitionInfo());
        }

        private void NameDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            inputForRename = null;
        }

        public async void ShowPropertiesButton_Click(object sender, RoutedEventArgs e)
        {

            if (Interaction.page.Name == "GenericItemView")
            {
                propertiesFrame.Navigate(typeof(Properties), (GenericFileBrowser.data.SelectedItem as ListedItem).FilePath, new SuppressNavigationTransitionInfo());
            }
            else if (Interaction.page.Name == "PhotoAlbumViewer")
            {
                propertiesFrame.Navigate(typeof(Properties), (PhotoAlbum.gv.SelectedItem as ListedItem).FilePath, new SuppressNavigationTransitionInfo());
            }
            await PropertiesDialog.ShowAsync();

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
            accessibleContentFrame.Navigate(typeof(YourHome), new SuppressNavigationTransitionInfo());
            this.Loaded -= Page_Loaded;
        }
    }
}
