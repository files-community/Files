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
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources.Core;
using Windows.Storage;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using static Files.Helpers.PathNormalization;

namespace Files.Views
{
    /// <summary>
    /// The root page of Files
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public SettingsViewModel AppSettings => App.AppSettings;

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
                NotifyPropertyChanged(nameof(SelectedTabItem));
            }
        }

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
            AllowDrop = true;
            AppInstances.CollectionChanged += AppInstances_CollectionChanged;
        }

        private void AppInstances_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var removedTab in e.OldItems)
                {
                    // Cleanup resources for the closed tab
                    ((((removedTab as TabItem).Content as Grid).Children[0] as Frame).Content as IShellPage)?.FilesystemViewModel?.Dispose();
                }
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs eventArgs)
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

                if (string.IsNullOrEmpty(navArgs))
                {
                    try
                    {
                        if (App.AppSettings.ResumeAfterRestart)
                        {
                            App.AppSettings.ResumeAfterRestart = false;

                            foreach (string path in App.AppSettings.LastSessionPages)
                            {
                                await AddNewTab(typeof(ModernShellPage), path);
                            }

                            if (!App.AppSettings.ContinueLastSessionOnStartUp)
                            {
                                App.AppSettings.LastSessionPages = null;
                            }
                        }
                        else if (App.AppSettings.OpenASpecificPageOnStartup)
                        {
                            if (App.AppSettings.PagesOnStartupList != null)
                            {
                                foreach (string path in App.AppSettings.PagesOnStartupList)
                                {
                                    await AddNewTab(typeof(ModernShellPage), path);
                                }
                            }
                            else
                            {
                                await AddNewTab(typeof(ModernShellPage), ResourceController.GetTranslation("NewTab"));
                            }
                        }
                        else if (App.AppSettings.ContinueLastSessionOnStartUp)
                        {
                            if (App.AppSettings.LastSessionPages != null)
                            {
                                foreach (string path in App.AppSettings.LastSessionPages)
                                {
                                    await AddNewTab(typeof(ModernShellPage), path);
                                }
                                App.AppSettings.LastSessionPages = new string[] { ResourceController.GetTranslation("NewTab") };
                            }
                            else
                            {
                                await AddNewTab(typeof(ModernShellPage), ResourceController.GetTranslation("NewTab"));
                            }
                        }
                        else
                        {
                            await AddNewTab(typeof(ModernShellPage), ResourceController.GetTranslation("NewTab"));
                        }
                    }
                    catch (Exception)
                    {
                        await AddNewTab(typeof(ModernShellPage), ResourceController.GetTranslation("NewTab"));
                    }
                }
                else if (string.IsNullOrEmpty(navArgs))
                {
                    await AddNewTab(typeof(ModernShellPage), ResourceController.GetTranslation("NewTab"));
                }
                else
                {
                    await AddNewTab(typeof(ModernShellPage), navArgs);
                }

                // Initial setting of SelectedTabItem
                Frame rootFrame = Window.Current.Content as Frame;
                var mainView = rootFrame.Content as MainPage;
                mainView.SelectedTabItem = AppInstances[App.InteractionViewModel.TabStripSelectedIndex];
            }
        }

        public static async Task AddNewTab(Type t, string path, int atIndex = -1)
        {
            string tabLocationHeader = null;
            Microsoft.UI.Xaml.Controls.FontIconSource fontIconSource = new Microsoft.UI.Xaml.Controls.FontIconSource();
            fontIconSource.FontFamily = App.Current.Resources["FluentUIGlyphs"] as FontFamily;

            if (path != null)
            {
                if (path == "Settings")
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarSettings/Text");
                    fontIconSource.Glyph = "\xeb5d";
                }
                else if (path.Equals(App.AppSettings.DesktopPath, StringComparison.OrdinalIgnoreCase))
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarDesktop");
                    fontIconSource.Glyph = "\xe9f1";
                }
                else if (path.Equals(App.AppSettings.DownloadsPath, StringComparison.OrdinalIgnoreCase))
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarDownloads");
                    fontIconSource.Glyph = "\xe91c";
                }
                else if (path.Equals(App.AppSettings.DocumentsPath, StringComparison.OrdinalIgnoreCase))
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarDocuments");
                    fontIconSource.Glyph = "\xEA11";
                }
                else if (path.Equals(App.AppSettings.PicturesPath, StringComparison.OrdinalIgnoreCase))
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarPictures");
                    fontIconSource.Glyph = "\xEA83";
                }
                else if (path.Equals(App.AppSettings.MusicPath, StringComparison.OrdinalIgnoreCase))
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarMusic");
                    fontIconSource.Glyph = "\xead4";
                }
                else if (path.Equals(App.AppSettings.VideosPath, StringComparison.OrdinalIgnoreCase))
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarVideos");
                    fontIconSource.Glyph = "\xec0d";
                }
                else if (path.Equals(App.AppSettings.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    tabLocationHeader = localSettings.Values.Get("RecycleBin_Title", "Recycle Bin");
                    fontIconSource.FontFamily = Application.Current.Resources["RecycleBinIcons"] as FontFamily;
                    fontIconSource.Glyph = "\xEF87";
                }
                else if (App.AppSettings.OneDrivePath != null && path.Equals(App.AppSettings.OneDrivePath, StringComparison.OrdinalIgnoreCase))
                {
                    tabLocationHeader = "OneDrive";
                    fontIconSource.Glyph = "\xe9b7";
                }
                else if (path == ResourceController.GetTranslation("NewTab"))
                {
                    tabLocationHeader = ResourceController.GetTranslation("NewTab");
                    fontIconSource.Glyph = "\xe90c";
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
                            tabLocationHeader = Path.GetFileName(path);
                            fontIconSource.Glyph = "\xea55";
                        }
                        else
                        {
                            // Pick the best icon for this tab
                            var remDriveNames = (await KnownFolders.RemovableDevices.GetFoldersAsync()).Select(x => x.DisplayName);

                            if (!remDriveNames.Contains(normalizedPath))
                            {
                                if (path != "A:" && path != "B:") // Check if it's using (generally) floppy-reserved letters.
                                    fontIconSource.Glyph = "\xeb4a"; // Floppy Disk icon
                                else
                                    fontIconSource.Glyph = "\xeb8b"; // Hard Disk icon

                                tabLocationHeader = normalizedPath;
                            }
                            else
                            {
                                fontIconSource.Glyph = "\xec0a";
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
                        fontIconSource.Glyph = "\xe90c";
                    }
                }
            }

            TabItem tvi = new TabItem()
            {
                Header = tabLocationHeader,
                Path = path,
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
            MainPage.AppInstances.Insert(atIndex == -1 ? AppInstances.Count : atIndex, tvi);
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

        private void NavigateToNumberedTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            int indexToSelect = 0;

            switch (sender.Key)
            {
                case VirtualKey.Number1:
                    indexToSelect = 0;
                    break;

                case VirtualKey.Number2:
                    indexToSelect = 1;
                    break;

                case VirtualKey.Number3:
                    indexToSelect = 2;
                    break;

                case VirtualKey.Number4:
                    indexToSelect = 3;
                    break;

                case VirtualKey.Number5:
                    indexToSelect = 4;
                    break;

                case VirtualKey.Number6:
                    indexToSelect = 5;
                    break;

                case VirtualKey.Number7:
                    indexToSelect = 6;
                    break;

                case VirtualKey.Number8:
                    indexToSelect = 7;
                    break;

                case VirtualKey.Number9:
                    // Select the last tab
                    indexToSelect = AppInstances.Count - 1;
                    break;
            }

            // Only select the tab if it is in the list
            if (indexToSelect < AppInstances.Count)
            {
                App.InteractionViewModel.TabStripSelectedIndex = indexToSelect;
            }
            args.Handled = true;
        }

        private async void CloseSelectedTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (AppInstances.Count == 1)
            {
                await ApplicationView.GetForCurrentView().TryConsolidateAsync();
            }
            else
            {
                if (App.InteractionViewModel.TabStripSelectedIndex >= AppInstances.Count)
                {
                    AppInstances.RemoveAt(AppInstances.Count - 1);
                }
                else
                {
                    AppInstances.RemoveAt(App.InteractionViewModel.TabStripSelectedIndex);
                }
            }
            args.Handled = true;
        }

        private async void AddNewInstanceAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            await AddNewTab(typeof(ModernShellPage), ResourceController.GetTranslation("NewTab"));
            args.Handled = true;
        }

        private void HorizontalMultitaskingControl_Loaded(object sender, RoutedEventArgs e)
        {
            App.MultitaskingControl = HorizontalMultitaskingControl;
        }
    }
}