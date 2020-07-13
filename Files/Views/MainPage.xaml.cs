using Files.Common;
using Files.Controllers;
using Files.Controls;
using Files.Filesystem;
using Files.UserControls;
using Files.View_Models;
using Files.Views.Pages;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources.Core;
using Windows.Services.Maps;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

using static Files.Helpers.PathNormalization;


namespace Files.Views
{
    /// <summary>
    /// The root page of Files
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private TabItem _SelectedTabItem;
        public TabItem SelectedTabItem 
        {
            get
            {
                return _SelectedTabItem;
            }
            set
            {
                _SelectedTabItem = value;
                NotifyPropertyChanged("SelectedTabItem");
            }
        }

        public SettingsViewModel AppSettings => App.AppSettings;
        public static ObservableCollection<TabItem> AppInstances = new ObservableCollection<TabItem>();
        public static ObservableCollection<INavigationControlItem> sideBarItems = new ObservableCollection<INavigationControlItem>();

        public MainPage()
        {
            this.InitializeComponent();
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
            var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            CoreTitleBar.ExtendViewIntoTitleBar = true;

            var flowDirectionSetting = ResourceContext.GetForCurrentView().QualifierValues["LayoutDirection"];

            if (flowDirectionSetting == "RTL")
            {
                FlowDirection = FlowDirection.RightToLeft;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            var navArgs = eventArgs.Parameter?.ToString();
            if (eventArgs.NavigationMode != NavigationMode.Back)
            {
                App.AppSettings = new SettingsViewModel();
                App.InteractionViewModel = new InteractionViewModel();
                App.SidebarPinnedController = new SidebarPinnedController();

                Helpers.ThemeHelper.Initialize();
                Clipboard.ContentChanged += Clipboard_ContentChanged;
                Clipboard_ContentChanged(null, null);

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

                // Initial setting of SelectedTabItem
                Frame rootFrame = Window.Current.Content as Frame;
                var mainView = rootFrame.Content as MainPage;
                mainView.SelectedTabItem = AppInstances[App.InteractionViewModel.TabStripSelectedIndex];
            }
        }

        public static async void AddNewTab(Type t, string path)
        {
            string tabLocationHeader = null;
            Microsoft.UI.Xaml.Controls.FontIconSource fontIconSource = new Microsoft.UI.Xaml.Controls.FontIconSource();

            if (path != null)
            {
                if (path == "Settings")
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarSettings/Text");
                    fontIconSource.Glyph = "\xE713";
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

            TabItem tvi = new TabItem()
            {
                Header = tabLocationHeader,
                Content = new Grid()
                {
                    Children =
                    {
                        new Frame()
                        {
                            CacheSize = 0,
                            Tag = new TabItemContent()
                            {
                                InitialPageType = t,
                                NavigationArg = path
                            }
                        }
                    },
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                },
                IconSource = fontIconSource,
                Description = null
            };
            MainPage.AppInstances.Add(tvi);

            var tabViewItemFrame = (tvi.Content as Grid).Children[0] as Frame;
            tabViewItemFrame.Loaded += TabViewItemFrame_Loaded;
        }

        private static void TabViewItemFrame_Loaded(object sender, RoutedEventArgs e)
        {
            var frame = sender as Frame;
            if (frame.CurrentSourcePageType != typeof(ModernShellPage))
            {
                frame.Navigate((frame.Tag as TabItemContent).InitialPageType, (frame.Tag as TabItemContent).NavigationArg);
                frame.Loaded -= TabViewItemFrame_Loaded;
            }
        }

        private void DragArea_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SetTitleBar(sender as Grid);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static void Clipboard_ContentChanged(object sender, object e)
        {
            try
            {
                if (App.CurrentInstance != null)
                {
                    DataPackageView packageView = Clipboard.GetContent();
                    if (packageView.Contains(StandardDataFormats.StorageItems)
                        && App.CurrentInstance.CurrentPageType != typeof(YourHome)
                        && !App.CurrentInstance.FilesystemViewModel.WorkingDirectory.StartsWith(App.AppSettings.RecycleBinPath))
                    {
                        App.InteractionViewModel.IsPasteEnabled = true;
                    }
                    else
                    {
                        App.InteractionViewModel.IsPasteEnabled = false;
                    }
                }
                else
                {
                    App.InteractionViewModel.IsPasteEnabled = false;
                }
            }
            catch (Exception)
            {
                App.InteractionViewModel.IsPasteEnabled = false;
            }
        }
    }
}
