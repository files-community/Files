using Files.Common;
using Files.Controls;
using Files.DataModels;
using Files.Filesystem;
using Files.View_Models;
using Files.Views.Pages;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.System;
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
        public SettingsViewModel AppSettings => App.AppSettings;

        public InstanceTabsView()
        {
            this.InitializeComponent();
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
            var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            CoreTitleBar.ExtendViewIntoTitleBar = true;
            tabView = TabStrip;

            App.AppSettings = new SettingsViewModel();
            App.InteractionViewModel = new InteractionViewModel();

            // Turn on Navigation Cache
            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            Window.Current.SizeChanged += Current_SizeChanged;
            Current_SizeChanged(null, null);

            Helpers.ThemeHelper.Initialize();
        }

        public static TabWindowProperties WindowProperties { get; set; } = new TabWindowProperties();

        public static async Task StartTerminateAsync()
        {
            IList<AppDiagnosticInfo> infos = await AppDiagnosticInfo.RequestInfoForAppAsync();
            IList<AppResourceGroupInfo> resourceInfos = infos[0].GetResourceGroups();
            var pid = Windows.System.Diagnostics.ProcessDiagnosticInfo.GetForCurrentProcess().ProcessId;
            await resourceInfos.Single(r => r.GetProcessDiagnosticInfos()[0].ProcessId == pid).StartTerminateAsync();
            //Application.Current.Exit();
        }

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

            if (TabStrip.TabItems.Count >= 1)
            {
                return;
            }

            if (string.IsNullOrEmpty(navArgs) && App.AppSettings.OpenASpecificPageOnStartup)
            {
                try
                {
                    AddNewTab(typeof(ModernShellPage), App.AppSettings.OpenASpecificPageOnStartupPath);
                }
                catch (Exception)
                {
                    AddNewTab(typeof(ModernShellPage), ResourceController.GetTranslation("NewTab"));
                }
            }
            else if (string.IsNullOrEmpty(navArgs))
            {
                AddNewTab(typeof(ModernShellPage), ResourceController.GetTranslation("NewTab"));
            }
            else
            {
                AddNewTab(typeof(ModernShellPage), navArgs);
            }
        }

        public async void AddNewTab(Type t, string path)
        {
            Frame frame = new Frame();
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
                else if (path.Equals(App.AppSettings.DesktopPath, StringComparison.OrdinalIgnoreCase))
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarDesktop");
                    fontIconSource.Glyph = "\xE8FC";
                }
                else if (path.Equals(App.AppSettings.DownloadsPath, StringComparison.OrdinalIgnoreCase))
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarDownloads");
                    fontIconSource.Glyph = "\xE896";
                }
                else if (path.Equals(App.AppSettings.DocumentsPath, StringComparison.OrdinalIgnoreCase))
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarDocuments");
                    fontIconSource.Glyph = "\xE8A5";
                }
                else if (path.Equals(App.AppSettings.PicturesPath, StringComparison.OrdinalIgnoreCase))
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarPictures");
                    fontIconSource.Glyph = "\xEB9F";
                }
                else if (path.Equals(App.AppSettings.MusicPath, StringComparison.OrdinalIgnoreCase))
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarMusic");
                    fontIconSource.Glyph = "\xEC4F";
                }
                else if (path.Equals(App.AppSettings.VideosPath, StringComparison.OrdinalIgnoreCase))
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarVideos");
                    fontIconSource.Glyph = "\xE8B2";
                }
                else if (path.Equals(App.AppSettings.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    tabLocationHeader = localSettings.Values.Get("RecycleBin_Title", "Recycle Bin");
                    fontIconSource.Glyph = "\xE74D";
                }
                else if (App.AppSettings.OneDrivePath != null && path.Equals(App.AppSettings.OneDrivePath, StringComparison.OrdinalIgnoreCase))
                {
                    tabLocationHeader = "OneDrive";
                    fontIconSource.Glyph = "\xE753";
                }
                else if (path == ResourceController.GetTranslation("NewTab"))
                {
                    tabLocationHeader = ResourceController.GetTranslation("NewTab");
                    fontIconSource.Glyph = "\xE737";
                }
                else
                {
                    var isRoot = Path.GetPathRoot(path) == path;

                    if (Path.IsPathRooted(path) || isRoot) // Or is a directory or a root (drive)
                    {
                        var normalizedPath = NormalizePath(path);

                        var dirName = Path.GetDirectoryName(normalizedPath);
                        if (dirName != null)
                        {
                            tabLocationHeader = dirName;
                            fontIconSource.Glyph = "\xE8B7";
                        }
                        else
                        {
                            // Pick the best icon for this tab
                            var remDriveNames = (await KnownFolders.RemovableDevices.GetFoldersAsync()).Select(x => x.DisplayName);

                            if (!remDriveNames.Contains(normalizedPath))
                            {
                                if (path != "A:" && path != "B:") // Check if it's using (generally) floppy-reserved letters.
                                    fontIconSource.Glyph = "\xE74E"; // Floppy Disk icon
                                else
                                    fontIconSource.Glyph = "\xEDA2"; // Hard Disk icon

                                tabLocationHeader = normalizedPath;
                            }
                            else
                            {
                                fontIconSource.Glyph = "\xE88E";
                                tabLocationHeader = (await KnownFolders.RemovableDevices.GetFolderAsync(path)).DisplayName;
                            }
                        }
                    }
                    else
                    {
                        // Invalid path, open new tab instead (explorer opens Documents when it fails)
                        Debug.WriteLine($"Invalid path \"{path}\" in InstanceTabsView.xaml.cs\\AddNewTab");

                        path = ResourceController.GetTranslation("NewTab");
                        tabLocationHeader = ResourceController.GetTranslation("NewTab");
                        fontIconSource.Glyph = "\xE737";
                    }
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
                IconSource = tabIcon,
                Transitions = null,
                Style = rootGrid.Resources["TabItemStyle"] as Style,
                ContentTransitions = null
            };
            tabView.TabItems.Add(tvi);
            TabStrip.SelectedIndex = TabStrip.TabItems.Count - 1;

            var tabViewItemFrame = (tvi.Content as Grid).Children[0] as Frame;
            tabViewItemFrame.Loaded += delegate
            {
                if (tabViewItemFrame.CurrentSourcePageType != typeof(ModernShellPage))
                {
                    tabViewItemFrame.Navigate(t, path);
                }
            };
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
            else if (currentPathForTabIcon == null && text == ResourceController.GetTranslation("NewTab"))
            {
                tabLocationHeader = ResourceController.GetTranslation("NewTab");
                fontIconSource.Glyph = "\xE737";
            }
            else if (currentPathForTabIcon.Equals(App.AppSettings.DesktopPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarDesktop");
                fontIconSource.Glyph = "\xE8FC";
            }
            else if (currentPathForTabIcon.Equals(App.AppSettings.DownloadsPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarDownloads");
                fontIconSource.Glyph = "\xE896";
            }
            else if (currentPathForTabIcon.Equals(App.AppSettings.DocumentsPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarDocuments");
                fontIconSource.Glyph = "\xE8A5";
            }
            else if (currentPathForTabIcon.Equals(App.AppSettings.PicturesPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarPictures");
                fontIconSource.Glyph = "\xEB9F";
            }
            else if (currentPathForTabIcon.Equals(App.AppSettings.MusicPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarMusic");
                fontIconSource.Glyph = "\xEC4F";
            }
            else if (currentPathForTabIcon.Equals(App.AppSettings.VideosPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarVideos");
                fontIconSource.Glyph = "\xE8B2";
            }
            else if (currentPathForTabIcon.Equals(App.AppSettings.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                tabLocationHeader = localSettings.Values.Get("RecycleBin_Title", "Recycle Bin");
                fontIconSource.FontFamily = Application.Current.Resources["RecycleBinIcons"] as FontFamily;
                fontIconSource.Glyph = "\xEF87";
            }
            else if (App.AppSettings.OneDrivePath != null && currentPathForTabIcon.Equals(App.AppSettings.OneDrivePath, StringComparison.OrdinalIgnoreCase))
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
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }
            if (path.StartsWith("\\\\"))
            {
                return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToUpperInvariant();
            }
            else
            {
                if (!path.EndsWith(Path.DirectorySeparatorChar))
                {
                    path += Path.DirectorySeparatorChar;
                }

                return Path.GetFullPath(new Uri(path).LocalPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .ToUpperInvariant();
            }
        }

        private void NavigateToNumberedTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var InvokedTabView = (args.Element as TabView);

            int tabToSelect = 0;

            switch (sender.Key)
            {
                case VirtualKey.Number1:
                    tabToSelect = 0;
                    break;

                case VirtualKey.Number2:
                    tabToSelect = 1;
                    break;

                case VirtualKey.Number3:
                    tabToSelect = 2;
                    break;

                case VirtualKey.Number4:
                    tabToSelect = 3;
                    break;

                case VirtualKey.Number5:
                    tabToSelect = 4;
                    break;

                case VirtualKey.Number6:
                    tabToSelect = 5;
                    break;

                case VirtualKey.Number7:
                    tabToSelect = 6;
                    break;

                case VirtualKey.Number8:
                    tabToSelect = 7;
                    break;

                case VirtualKey.Number9:
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

        private async void CloseSelectedTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var InvokedTabView = (args.Element as TabView);

            // Only close the selected tab if it is closeable
            if (((TabViewItem)InvokedTabView.SelectedItem).IsClosable)
            {
                if (TabStrip.TabItems.Count == 1)
                {
                    await InstanceTabsView.StartTerminateAsync();
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
                if (e.RemovedItems.Count > 0 && e.AddedItems.Count == 0)
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
                //App.InteractionViewModel.TabStripSelectedIndex = TabStrip.SelectedIndex;
                if ((tabView.SelectedItem as TabViewItem).Header.ToString() == ResourceController.GetTranslation("SidebarSettings/Text"))
                {
                    App.InteractionViewModel.TabsLeftMargin = new Thickness(0, 0, 0, 0);
                    App.InteractionViewModel.LeftMarginLoaded = false;
                }
                else
                {
                    if (App.CurrentInstance != null)
                    {
                        if ((tabView.SelectedItem as TabViewItem).Header.ToString() == ResourceController.GetTranslation("NewTab"))
                        {
                            App.CurrentInstance.InstanceViewModel.IsPageTypeNotHome = false;
                        }
                        else
                        {
                            App.CurrentInstance.InstanceViewModel.IsPageTypeNotHome = true;
                        }
                        if ((tabView.SelectedItem as TabViewItem).Header.ToString() ==
                            ApplicationData.Current.LocalSettings.Values.Get("RecycleBin_Title", "Recycle Bin"))
                        {
                            App.CurrentInstance.InstanceViewModel.IsPageTypeNotRecycleBin = false;
                        }
                        else
                        {
                            App.CurrentInstance.InstanceViewModel.IsPageTypeNotRecycleBin = true;
                        }
                    }

                    App.InteractionViewModel.TabsLeftMargin = new Thickness(200, 0, 0, 0);
                    App.InteractionViewModel.LeftMarginLoaded = true;
                }
            }
        }

        private async void TabStrip_TabCloseRequested(Microsoft.UI.Xaml.Controls.TabView sender, Microsoft.UI.Xaml.Controls.TabViewTabCloseRequestedEventArgs args)
        {
            if (TabStrip.TabItems.Count == 1)
            {
                await InstanceTabsView.StartTerminateAsync();
            }
            else if (TabStrip.TabItems.Count > 1)
            {
                int tabIndexToClose = TabStrip.TabItems.IndexOf(args.Tab);
                TabStrip.TabItems.RemoveAt(tabIndexToClose);
            }
        }        

        private void AddTabButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab(typeof(ModernShellPage), ResourceController.GetTranslation("NewTab"));
        }

        public static T GetCurrentSelectedTabInstance<T>()
        {
            Frame rootFrame = Window.Current.Content as Frame;
            var instanceTabsView = rootFrame.Content as InstanceTabsView;
            var selectedTabContent = ((instanceTabsView.TabStrip.SelectedItem as TabViewItem).Content as Grid);
            foreach (UIElement uiElement in selectedTabContent.Children)
            {
                if (uiElement.GetType() == typeof(Frame))
                {
                    return (T)((uiElement as Frame).Content);
                }
            }
            return default;
        }

        private void TabStrip_Loaded(object sender, RoutedEventArgs e)
        {
            TabStrip.SelectedIndex = App.InteractionViewModel.TabStripSelectedIndex;
        }

        private void TabStrip_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("TEST UNLOADED");
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