using Files.Controllers;
using Files.Controls;
using Files.Helpers;
using Files.Filesystem;
using Files.UserControls.MultitaskingControl;
using Files.View_Models;
using Files.Views.Pages;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
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
        public static IMultitaskingControl MultitaskingControl { get; set; }

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
        public static ObservableCollection<INavigationControlItem> SideBarItems = new ObservableCollection<INavigationControlItem>();

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
                    ((((removedTab as TabItem).Content as Grid).Children[0] as Frame).Content as IShellPage)?.Dispose();
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

                if (string.IsNullOrEmpty(navArgs))
                {
                    try
                    {
                        if (App.AppSettings.ResumeAfterRestart)
                        {
                            App.AppSettings.ResumeAfterRestart = false;

                            foreach (string path in App.AppSettings.LastSessionPages)
                            {
                                await AddNewTabByPathAsync(typeof(ModernShellPage), path);
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
                                    await AddNewTabByPathAsync(typeof(ModernShellPage), path);
                                }
                            }
                            else
                            {
                                AddNewTabAsync();
                            }
                        }
                        else if (App.AppSettings.ContinueLastSessionOnStartUp)
                        {
                            if (App.AppSettings.LastSessionPages != null)
                            {
                                foreach (string path in App.AppSettings.LastSessionPages)
                                {
                                    await AddNewTabByPathAsync(typeof(ModernShellPage), path);
                                }
                                App.AppSettings.LastSessionPages = new string[] { "NewTab".GetLocalized() };
                            }
                            else
                            {
                                AddNewTabAsync();
                            }
                        }
                        else
                        {
                            AddNewTabAsync();
                        }
                    }
                    catch (Exception)
                    {
                        AddNewTabAsync();
                    }
                }
                else if (string.IsNullOrEmpty(navArgs))
                {
                    AddNewTabAsync();
                }
                else
                {
                    await AddNewTabByPathAsync(typeof(ModernShellPage), navArgs);
                }

                // Check for required updates
                try
                {
                    AppUpdater updater = new AppUpdater();
                    await updater.CheckForUpdatesAsync();
                }
                catch (Exception)
                {
                    // App is not installed from the store or checking for updates failed
                }
                

                // Initial setting of SelectedTabItem
                Frame rootFrame = Window.Current.Content as Frame;
                var mainView = rootFrame.Content as MainPage;
                mainView.SelectedTabItem = AppInstances[App.InteractionViewModel.TabStripSelectedIndex];
            }
        }

        public static async Task AddNewTabAsync()
        {
            await AddNewTabByPathAsync(typeof(ModernShellPage), "NewTab".GetLocalized());
        }

        public static async void AddNewTabAtIndex(object sender, RoutedEventArgs e)
        {
            await MainPage.AddNewTabByPathAsync(typeof(ModernShellPage), "NewTab".GetLocalized());
        }

        public static async void DuplicateTabAtIndex(object sender, RoutedEventArgs e)
        {
            var tabItem = ((FrameworkElement)sender).DataContext as TabItem;
            var index = MainPage.AppInstances.IndexOf(tabItem);

            if (MainPage.AppInstances[index].Path != null)
            {
                await MainPage.AddNewTabByPathAsync(typeof(ModernShellPage), MainPage.AppInstances[index].Path);
            }
            else
            {
                await MainPage.AddNewTabByPathAsync(typeof(ModernShellPage), "NewTab".GetLocalized());
            }
        }

        public static async void MoveTabToNewWindow(object sender, RoutedEventArgs e)
        {
            var tabItem = ((FrameworkElement)sender).DataContext as TabItem;
            var index = MainPage.AppInstances.IndexOf(tabItem);
            var path = MainPage.AppInstances[index].Path;

            MainPage.MultitaskingControl.Items.RemoveAt(index);

            if (path != null)
            {
                var folderUri = new Uri("files-uwp:" + "?folder=" + path);
                await Launcher.LaunchUriAsync(folderUri);
            }
            else
            {
                var folderUri = new Uri("files-uwp:" + "?folder=" + "NewTab".GetLocalized());
                await Launcher.LaunchUriAsync(folderUri);
            }
        }

        public static async Task AddNewTabByPathAsync(Type type, string path, int atIndex = -1)
        {
            string tabLocationHeader = string.Empty;
            Microsoft.UI.Xaml.Controls.FontIconSource fontIconSource = new Microsoft.UI.Xaml.Controls.FontIconSource();
            fontIconSource.FontFamily = App.Current.Resources["FluentUIGlyphs"] as FontFamily;

            if (string.IsNullOrEmpty(path))
            {
                path = "NewTab".GetLocalized();
            }

            if (path != null)
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
                            {
                                fontIconSource.Glyph = "\xeb4a"; // Floppy Disk icon
                            }
                            else
                            {
                                fontIconSource.Glyph = "\xeb8b"; // Hard Disk icon
                            }

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

                    path = "NewTab".GetLocalized();
                    tabLocationHeader = "NewTab".GetLocalized();
                    fontIconSource.Glyph = "\xe90c";
                }
            }

            TabItem tabItem = new TabItem()
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
                                InitialPageType = type,
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
            AppInstances.Insert(atIndex == -1 ? AppInstances.Count : atIndex, tabItem);
            var tabViewItemFrame = (tabItem.Content as Grid).Children[0] as Frame;
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
            await AddNewTabByPathAsync(typeof(ModernShellPage), "NewTab".GetLocalized());
            args.Handled = true;
        }

        private void HorizontalMultitaskingControl_Loaded(object sender, RoutedEventArgs e)
        {
            MultitaskingControl = HorizontalMultitaskingControl;
        }
    }
}