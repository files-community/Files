using Files.Filesystem;
using Files.Views.Pages;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Files
{
    public sealed partial class InstanceTabsView : Page
    {
        public static TabView tabView;
        public string navArgs;
        public InstanceTabsView()
        {
            this.InitializeComponent();
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
            var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            CoreTitleBar.ExtendViewIntoTitleBar = true;
            tabView = TabStrip;
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 255, 255, 255);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 10, 10, 10);
            if (App.Current.RequestedTheme == ApplicationTheme.Dark)
            {
                titleBar.ButtonBackgroundColor = Color.FromArgb(0, 0, 0, 0);
                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 240, 240, 240);
                //titleBar.BackgroundColor = Color.FromArgb(255, 25, 25, 25);
            }
            else if (App.Current.RequestedTheme == ApplicationTheme.Light)
            {
                titleBar.ButtonBackgroundColor = Color.FromArgb(0, 255, 255, 255);
                titleBar.ButtonForegroundColor = Colors.Black;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 155, 155, 155);
            }

            if (this.RequestedTheme == ElementTheme.Dark)
            {
                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 240, 240, 240);
                //titleBar.BackgroundColor = Color.FromArgb(255, 25, 25, 25);
            }
            else if (this.RequestedTheme == ElementTheme.Light)
            {
                titleBar.ButtonForegroundColor = Colors.Black;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 155, 155, 155);
                //titleBar.BackgroundColor = Colors.Transparent;
            }

            // Check if the acrylic sidebar setting is on
            if (App.AppSettings.AcrylicSidebar == true)
            {
                this.Background = (Brush)Application.Current.Resources["BackgroundAcrylicBrush"];
            }
            else
            {
                this.Background = (Brush)Application.Current.Resources["SystemControlBackgroundChromeMediumLowBrush"];
            }

            Window.Current.SizeChanged += Current_SizeChanged;
            Current_SizeChanged(null, null);
        }

        public static TabWindowProperties WindowProperties { get; set; } = new TabWindowProperties();

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            if (Huyn.WindowDisplayInfo.GetForCurrentView().ToString() == "Maximized")
            {
                WindowProperties.TabListPadding = new Thickness(0, 0, 0, 0);
                WindowProperties.TabAddButtonMargin = new Thickness(0, 0, 0, 0);

            }
            else
            {
                WindowProperties.TabListPadding = new Thickness(0, 0, 0, 0);
                WindowProperties.TabAddButtonMargin = new Thickness(0, 0, 0, 0);

            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            navArgs = eventArgs.Parameter?.ToString();

            if (string.IsNullOrEmpty(navArgs))
            {
                AddNewTab(typeof(ModernShellPage), "New tab");
            }
            else
            {
                AddNewTab(typeof(ModernShellPage), navArgs);
            }

            Microsoft.UI.Xaml.Controls.FontIconSource icon = new Microsoft.UI.Xaml.Controls.FontIconSource();
            icon.Glyph = "\xE713";
            if ((tabView.SelectedItem as TabViewItem).Header.ToString() != ResourceController.GetTranslation("SidebarSettings/Text") && (tabView.SelectedItem as TabViewItem).IconSource != icon)
            {
                App.CurrentInstance = ItemViewModel.GetCurrentSelectedTabInstance<ModernShellPage>();
            }
        }

        public void AddNewTab(Type t, string path)
        {
            Frame frame = new Frame();
            //frame.Navigate(t, path);
            string tabLocationHeader = null;
            Microsoft.UI.Xaml.Controls.FontIconSource fontIconSource = new Microsoft.UI.Xaml.Controls.FontIconSource();
            Microsoft.UI.Xaml.Controls.IconSource tabIcon;

            if (path != null)
            {
                if (path == "Settings")
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarSettings/Text");
                    fontIconSource.Glyph = "\xE713";
                    foreach (TabViewItem item in tabView.TabItems)
                    {
                        if (item.Header.ToString() == ResourceController.GetTranslation("SidebarSettings/Text"))
                        {
                            tabView.SelectedItem = item;
                            return;
                        }
                    }
                }
                else if (path == App.AppSettings.DesktopPath)
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarDesktop");
                    fontIconSource.Glyph = "\xE8FC";
                }
                else if (path == App.AppSettings.DownloadsPath)
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarDownloads");
                    fontIconSource.Glyph = "\xE896";
                }
                else if (path == App.AppSettings.DocumentsPath)
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarDocuments");
                    fontIconSource.Glyph = "\xE8A5";
                }
                else if (path == App.AppSettings.PicturesPath)
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarPictures");
                    fontIconSource.Glyph = "\xEB9F";
                }
                else if (path == App.AppSettings.MusicPath)
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarMusic");
                    fontIconSource.Glyph = "\xEC4F";
                }
                else if (path == App.AppSettings.VideosPath)
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarVideos");
                    fontIconSource.Glyph = "\xE8B2";
                }
                else if (path == App.AppSettings.OneDrivePath)
                {
                    tabLocationHeader = "OneDrive";
                    fontIconSource.Glyph = "\xE753";
                }
                else if (path == "New tab")
                {
                    tabLocationHeader = "New tab";
                    fontIconSource.Glyph = "\xE737";
                }
                else
                {
                    tabLocationHeader = Path.GetDirectoryName(path);
                    fontIconSource.Glyph = "\xE8B7";
                }
            }

            tabIcon = fontIconSource;
            Grid gr = new Grid();
            gr.Children.Add(frame);
            gr.HorizontalAlignment = HorizontalAlignment.Stretch;
            gr.VerticalAlignment = VerticalAlignment.Stretch;
            TabViewItem tvi = new TabViewItem()
            {
                Header = tabLocationHeader,
                Content = gr,
                Width = 200,
                IconSource = tabIcon
            };
            tabView.TabItems.Add(tvi);
            TabStrip.SelectedItem = TabStrip.TabItems[TabStrip.TabItems.Count - 1];
            if (tabView.SelectedItem == tvi)
            {
                (((tabView.SelectedItem as TabViewItem).Content as Grid).Children[0] as Frame).Navigate(t, path);
            }
        }

        public async void SetSelectedTabInfo(string text, string currentPathForTabIcon = null)
        {
            string tabLocationHeader;
            Microsoft.UI.Xaml.Controls.FontIconSource fontIconSource = new Microsoft.UI.Xaml.Controls.FontIconSource();
            Microsoft.UI.Xaml.Controls.IconSource tabIcon;

            if (currentPathForTabIcon == null && text == ResourceController.GetTranslation("SidebarSettings/Text"))
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarSettings/Text");
                fontIconSource.Glyph = "\xE713";
            }
            else if (currentPathForTabIcon == null && text == "New tab")
            {
                tabLocationHeader = "New tab";
                fontIconSource.Glyph = "\xE737";
            }
            else if (currentPathForTabIcon == App.AppSettings.DesktopPath)
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarDesktop");
                fontIconSource.Glyph = "\xE8FC";
            }
            else if (currentPathForTabIcon == App.AppSettings.DownloadsPath)
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarDownloads");
                fontIconSource.Glyph = "\xE896";
            }
            else if (currentPathForTabIcon == App.AppSettings.DocumentsPath)
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarDocuments");
                fontIconSource.Glyph = "\xE8A5";
            }
            else if (currentPathForTabIcon == App.AppSettings.PicturesPath)
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarPictures");
                fontIconSource.Glyph = "\xEB9F";
            }
            else if (currentPathForTabIcon == App.AppSettings.MusicPath)
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarMusic");
                fontIconSource.Glyph = "\xEC4F";
            }
            else if (currentPathForTabIcon == App.AppSettings.VideosPath)
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarVideos");
                fontIconSource.Glyph = "\xE8B2";
            }
            else if (currentPathForTabIcon == App.AppSettings.OneDrivePath)
            {
                tabLocationHeader = "OneDrive";
                fontIconSource.Glyph = "\xE753";
            }
            else
            {
                // If path is a drive's root
                if (NormalizePath(Path.GetPathRoot(currentPathForTabIcon)) == NormalizePath(currentPathForTabIcon))
                {
                    if (NormalizePath(currentPathForTabIcon) != NormalizePath("A:") && NormalizePath(currentPathForTabIcon) != NormalizePath("B:"))
                    {
                        var remDriveNames = (await KnownFolders.RemovableDevices.GetFoldersAsync()).Select(x => x.DisplayName);

                        if (!remDriveNames.Contains(NormalizePath(currentPathForTabIcon)))
                        {
                            fontIconSource.Glyph = "\xEDA2";
                            tabLocationHeader = NormalizePath(currentPathForTabIcon);
                        }
                        else
                        {
                            fontIconSource.Glyph = "\xE88E";
                            tabLocationHeader = (await KnownFolders.RemovableDevices.GetFolderAsync(currentPathForTabIcon)).DisplayName;
                        }
                    }
                    else
                    {
                        fontIconSource.Glyph = "\xE74E";
                        tabLocationHeader = NormalizePath(currentPathForTabIcon);
                    }
                }
                else
                {
                    fontIconSource.Glyph = "\xE8B7";
                    tabLocationHeader = currentPathForTabIcon.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Split('\\', StringSplitOptions.RemoveEmptyEntries).Last();
                }

            }
            tabIcon = fontIconSource;
            (tabView.SelectedItem as TabViewItem).Header = tabLocationHeader;
            (tabView.SelectedItem as TabViewItem).IconSource = tabIcon;
        }

        public static string NormalizePath(string path)
        {
            if (path.StartsWith("\\\\"))
            {
                return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToUpperInvariant();
            }
            else
            {
                if (path.Contains('\\'))
                {
                    return Path.GetFullPath(new Uri(path).LocalPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .ToUpperInvariant();
                }
                else
                {
                    return Path.GetFullPath(path)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .ToUpperInvariant();
                }
            }
        }

        private void NavigateToNumberedTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var InvokedTabView = (args.Element as TabView);

            int tabToSelect = 0;

            switch (sender.Key)
            {
                case Windows.System.VirtualKey.Number1:
                    tabToSelect = 0;
                    break;
                case Windows.System.VirtualKey.Number2:
                    tabToSelect = 1;
                    break;
                case Windows.System.VirtualKey.Number3:
                    tabToSelect = 2;
                    break;
                case Windows.System.VirtualKey.Number4:
                    tabToSelect = 3;
                    break;
                case Windows.System.VirtualKey.Number5:
                    tabToSelect = 4;
                    break;
                case Windows.System.VirtualKey.Number6:
                    tabToSelect = 5;
                    break;
                case Windows.System.VirtualKey.Number7:
                    tabToSelect = 6;
                    break;
                case Windows.System.VirtualKey.Number8:
                    tabToSelect = 7;
                    break;
                case Windows.System.VirtualKey.Number9:
                    // Select the last tab
                    tabToSelect = InvokedTabView.TabItems.Count - 1;
                    break;
            }

            // Only select the tab if it is in the list
            if (tabToSelect < InvokedTabView.TabItems.Count)
            {
                InvokedTabView.SelectedIndex = tabToSelect;
            }
            args.Handled = true;
        }

        private void CloseSelectedTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var InvokedTabView = (args.Element as TabView);

            // Only close the selected tab if it is closeable
            if (((TabViewItem)InvokedTabView.SelectedItem).IsClosable)
            {
                if (TabStrip.TabItems.Count == 1)
                {
                    Application.Current.Exit();
                }
                else
                { 
                InvokedTabView.TabItems.Remove(InvokedTabView.SelectedItem);
                }
            }
            args.Handled = true;
        }


        private void DragArea_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SetTitleBar(sender as Grid);
        }

        public void TabStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabStrip.SelectedItem == null)
            {
                if (e.RemovedItems.Count > 0)
                {
                    var itemToReselect = e.RemovedItems[0];
                    if (TabStrip.TabItems.Contains(itemToReselect))
                    {
                        TabStrip.SelectedItem = itemToReselect;
                    }
                }
            }
            else
            {
                if ((tabView.SelectedItem as TabViewItem).Header.ToString() == ResourceController.GetTranslation("SidebarSettings/Text"))
                {
                    App.InteractionViewModel.TabsLeftMargin = new Thickness(0, 0, 0, 0);
                    App.InteractionViewModel.LeftMarginLoaded = false;
                }
                else
                {
                    if ((tabView.SelectedItem as TabViewItem).Header.ToString() == "New tab")
                    {
                        App.InteractionViewModel.IsPageTypeNotHome = false;
                    }
                    else
                    {
                        App.InteractionViewModel.IsPageTypeNotHome = true;
                    }

                    App.InteractionViewModel.TabsLeftMargin = new Thickness(200, 0, 0, 0);
                    App.InteractionViewModel.LeftMarginLoaded = true;
                }

                Microsoft.UI.Xaml.Controls.FontIconSource icon = new Microsoft.UI.Xaml.Controls.FontIconSource();
                icon.Glyph = "\xE713";
                if ((tabView.SelectedItem as TabViewItem).Header.ToString() != ResourceController.GetTranslation("SidebarSettings/Text") && (tabView.SelectedItem as TabViewItem).IconSource != icon)
                {
                    App.CurrentInstance = ItemViewModel.GetCurrentSelectedTabInstance<ModernShellPage>();
                }
            }

        }

        private void TabStrip_TabCloseRequested(Microsoft.UI.Xaml.Controls.TabView sender, Microsoft.UI.Xaml.Controls.TabViewTabCloseRequestedEventArgs args)
        {
            if (TabStrip.TabItems.Count == 1)
            {
                Application.Current.Exit();
            }
            else if (TabStrip.TabItems.Count > 1)
            {
                int tabIndexToClose = TabStrip.TabItems.IndexOf(args.Tab);
                TabStrip.TabItems.RemoveAt(tabIndexToClose);
            }
        }

        private void AddTabButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab(typeof(ModernShellPage), "New tab");
        }
    }

    public class TabWindowProperties : INotifyPropertyChanged
    {
        private Thickness tabListPadding = new Thickness(8, 8, 0, 0);
        private Thickness tabAddButtonMargin = new Thickness(0, 8, 0, 0);

        public Thickness TabListPadding
        {
            get
            {
                return tabListPadding;
            }
            set
            {
                if (tabListPadding != value)
                {
                    tabListPadding = value;
                    RaiseChangeNotification("TabListPadding");
                }
            }
        }

        public Thickness TabAddButtonMargin
        {
            get
            {
                return tabAddButtonMargin;
            }
            set
            {
                if (tabAddButtonMargin != value)
                {
                    tabAddButtonMargin = value;
                    RaiseChangeNotification("TabAddButtonMargin");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaiseChangeNotification(string v)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(v));
            }
        }

    }
}
