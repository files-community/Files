using Files.Interacts;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Files
{
    /// <summary>
    /// This is not finished yet. This is the work that was started on having multiple Tabs
    /// </summary>
    public sealed partial class ProHome : Page
    {
        ObservableCollection<Tab> tabList = new ObservableCollection<Tab>();
        public static ContentDialog permissionBox;
        public static ListView locationsList;
        public static ListView drivesList;
        public static Frame accessibleContentFrame;
        public static Button BackButton;
        public static Button ForwardButton;
        public static Button RefreshButton;
        public static Button AddItemButton;
        public static ContentDialog AddItemBox;
        public static ContentDialog NameBox;
        public static TextBox inputFromRename;
        public static string inputForRename;
        public static ObservableCollection<Tab> TabList { get; set; } = new ObservableCollection<Tab>();
        public static string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        public static string DocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public static string DownloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
        public static string OneDrivePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive";
        public static string PicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        public static string MusicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        public static string VideosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        public ProHome()
        {
            this.InitializeComponent();
            // TODO: Migrate preferred view size to page hosting tabs (when needed)
            ApplicationView.PreferredLaunchViewSize = new Size(1080, 630);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            CoreTitleBar.ExtendViewIntoTitleBar = false;
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 255, 255, 255);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 10, 10, 10);
            permissionBox = PermissionDialog;
            locationsList = LocationsList;
            drivesList = DrivesList;
            accessibleContentFrame = ItemDisplayFrame;
            AddItemBox = AddDialog;
            NameBox = NameDialog;
            inputFromRename = RenameInput;
            BackButton = Back;
            ForwardButton = Forward;
            RefreshButton = Refresh;
            AddItemButton = AddItem;
            LocationsList.SelectedIndex = 0;
            accessibleContentFrame.Navigate(typeof(YourHome));
            if (App.Current.RequestedTheme == ApplicationTheme.Dark)
            {
                titleBar.ButtonBackgroundColor = Color.FromArgb(255, 0, 0, 0);
                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 240, 240, 240);
            }
            else if (App.Current.RequestedTheme == ApplicationTheme.Light)
            {
                titleBar.ButtonBackgroundColor = Color.FromArgb(255, 255, 255, 255);
                titleBar.ButtonForegroundColor = Colors.Black;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 240, 240, 240);
            }

            if (this.RequestedTheme == ElementTheme.Dark)
            {
                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 240, 240, 240);
            }
            else if (this.RequestedTheme == ElementTheme.Light)
            {
                titleBar.ButtonForegroundColor = Colors.Black;
            }
            TabList.Clear();
            TabList.Add(new Tab() { TabName = "Home", TabContent = "local:MainPage" });
            PathBarTip.IsOpen = false;
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

        private void TabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedTab = e.AddedItems as TabViewItem;

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
                    if (CurrentInput == "Home" || CurrentInput == "home")
                    {
                        ProHome.accessibleContentFrame.Navigate(typeof(YourHome));
                        App.PathText.Text = "This PC";
                        App.LayoutItems.isEnabled = false;
                    }
                    else if (CurrentInput == "Desktop" || CurrentInput == "desktop")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DesktopPath);
                        App.PathText.Text = "Desktop";
                        App.LayoutItems.isEnabled = true;

                    }
                    else if (CurrentInput == "Documents" || CurrentInput == "documents")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DocumentsPath);
                        App.PathText.Text = "Documents";
                        App.LayoutItems.isEnabled = true;


                    }
                    else if (CurrentInput == "Downloads" || CurrentInput == "downloads")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DownloadsPath);
                        App.PathText.Text = "Downloads";
                        App.LayoutItems.isEnabled = true;


                    }
                    else if (CurrentInput == "Pictures" || CurrentInput == "pictures")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        ProHome.accessibleContentFrame.Navigate(typeof(PhotoAlbum), PicturesPath);
                        App.PathText.Text = "Pictures";
                        App.LayoutItems.isEnabled = true;

                    }
                    else if (CurrentInput == "Music" || CurrentInput == "music")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MusicPath);
                        App.PathText.Text = "Music";
                        App.LayoutItems.isEnabled = true;


                    }
                    else if (CurrentInput == "Videos" || CurrentInput == "videos")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), VideosPath);
                        App.PathText.Text = "Videos";
                        App.LayoutItems.isEnabled = true;


                    }
                    else if (CurrentInput == "OneDrive" || CurrentInput == "Onedrive" || CurrentInput == "onedrive")
                    {
                        App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                        ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), OneDrivePath);
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
                                await StorageFolder.GetFolderFromPathAsync(CurrentInput);
                                App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                                ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), CurrentInput);
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
                            }
                        }
                        else
                        {
                            try
                            {
                                await StorageFolder.GetFolderFromPathAsync(CurrentInput);
                                App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                                ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), CurrentInput);
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

                        }

                    }
                }
            }
        }

        private void LocationsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            App.HomeItems.isEnabled = false;
            App.ShareItems.isEnabled = false;
            if (DrivesList.SelectedItem != null)
            {
                DrivesList.SelectedItem = null;
                App.LayoutItems.isEnabled = false;
            }
            var clickedItem = Interaction.FindParent<ListViewItem>(e.ClickedItem as DependencyObject);
            if(clickedItem.Tag.ToString() == "ThisPC")
            {
                ItemDisplayFrame.Navigate(typeof(YourHome));
                App.PathText.Text = "This PC";
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
    }

    public class Tab
    {
        public string TabName { get; set; }
        public string TabContent { get; set; }
    }
}
