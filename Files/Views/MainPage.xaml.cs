using Files.Common;
using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using Files.UserControls.MultitaskingControl;
using Files.ViewModels;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources.Core;
using Windows.Storage;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
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

        private TabItem selectedTabItem;

        public TabItem SelectedTabItem
        {
            get
            {
                return selectedTabItem;
            }
            set
            {
                selectedTabItem = value;
                NotifyPropertyChanged(nameof(SelectedTabItem));
            }
        }

        public static ObservableCollection<TabItem> AppInstances = new ObservableCollection<TabItem>();
        public static BulkConcurrentObservableCollection<INavigationControlItem> SideBarItems = new BulkConcurrentObservableCollection<INavigationControlItem>();
        public static SemaphoreSlim SideBarItemsSemaphore = new SemaphoreSlim(1, 1);

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
        }

        protected override async void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            if (eventArgs.NavigationMode != NavigationMode.Back)
            {
                //Initialize the static theme helper to capture a reference to this window
                //to handle theme changes without restarting the app
                ThemeHelper.Initialize();

                if (eventArgs.Parameter == null || (eventArgs.Parameter is string eventStr && string.IsNullOrEmpty(eventStr)))
                {
                    try
                    {
                        if (App.AppSettings.ResumeAfterRestart)
                        {
                            App.AppSettings.ResumeAfterRestart = false;

                            foreach (string tabArgsString in App.AppSettings.LastSessionPages)
                            {
                                var tabArgs = TabItemArguments.Deserialize(tabArgsString);
                                await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
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
                                    await AddNewTabByPathAsync(typeof(PaneHolderPage), path);
                                }
                            }
                            else
                            {
                                await AddNewTabAsync();
                            }
                        }
                        else if (App.AppSettings.ContinueLastSessionOnStartUp)
                        {
                            if (App.AppSettings.LastSessionPages != null)
                            {
                                foreach (string tabArgsString in App.AppSettings.LastSessionPages)
                                {
                                    var tabArgs = TabItemArguments.Deserialize(tabArgsString);
                                    await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
                                }
                                var defaultArg = new TabItemArguments() { InitialPageType = typeof(PaneHolderPage), NavigationArg = "NewTab".GetLocalized() };
                                App.AppSettings.LastSessionPages = new string[] { defaultArg.Serialize() };
                            }
                            else
                            {
                                await AddNewTabAsync();
                            }
                        }
                        else
                        {
                            await AddNewTabAsync();
                        }
                    }
                    catch (Exception)
                    {
                        await AddNewTabAsync();
                    }
                }
                else
                {
                    if (eventArgs.Parameter is string navArgs)
                    {
                        await AddNewTabByPathAsync(typeof(PaneHolderPage), navArgs);
                    }
                    else if (eventArgs.Parameter is TabItemArguments tabArgs)
                    {
                        await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
                    }
                }

                // Check for required updates
                AppUpdater updater = new AppUpdater();
                updater.CheckForUpdatesAsync();

                // Initial setting of SelectedTabItem
                Frame rootFrame = Window.Current.Content as Frame;
                var mainView = rootFrame.Content as MainPage;
                mainView.SelectedTabItem = AppInstances[App.InteractionViewModel.TabStripSelectedIndex];
            }
        }

        public static async Task AddNewTabAsync()
        {
            await AddNewTabByPathAsync(typeof(PaneHolderPage), "NewTab".GetLocalized());
        }

        public static async void AddNewTabAtIndex(object sender, RoutedEventArgs e)
        {
            await AddNewTabByPathAsync(typeof(PaneHolderPage), "NewTab".GetLocalized());
        }

        public static async void DuplicateTabAtIndex(object sender, RoutedEventArgs e)
        {
            var tabItem = ((FrameworkElement)sender).DataContext as TabItem;
            var index = AppInstances.IndexOf(tabItem);

            if (AppInstances[index].TabItemArguments != null)
            {
                var tabArgs = AppInstances[index].TabItemArguments;
                await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg, index + 1);
            }
            else
            {
                await AddNewTabByPathAsync(typeof(PaneHolderPage), "NewTab".GetLocalized());
            }
        }

        public static async void MoveTabToNewWindow(object sender, RoutedEventArgs e)
        {
            var tabItem = ((FrameworkElement)sender).DataContext as TabItem;
            var index = AppInstances.IndexOf(tabItem);
            var tabItemArguments = AppInstances[index].TabItemArguments;

            MultitaskingControl.Items.RemoveAt(index);

            if (tabItemArguments != null)
            {
                await Interacts.Interaction.OpenTabInNewWindowAsync(tabItemArguments.Serialize());
            }
            else
            {
                await Interacts.Interaction.OpenPathInNewWindowAsync("NewTab".GetLocalized());
            }
        }

        public static async Task AddNewTabByParam(Type type, object tabViewItemArgs, int atIndex = -1)
        {
            Microsoft.UI.Xaml.Controls.FontIconSource fontIconSource = new Microsoft.UI.Xaml.Controls.FontIconSource();
            fontIconSource.FontFamily = App.Current.Resources["FluentUIGlyphs"] as FontFamily;

            TabItem tabItem = new TabItem()
            {
                Header = null,
                IconSource = fontIconSource,
                Description = null
            };
            tabItem.Control.NavigationArguments = new TabItemArguments()
            {
                InitialPageType = type,
                NavigationArg = tabViewItemArgs
            };
            tabItem.Control.ContentChanged += Control_ContentChanged;
            await UpdateTabInfo(tabItem, tabViewItemArgs);
            AppInstances.Insert(atIndex == -1 ? AppInstances.Count : atIndex, tabItem);
        }

        public static async Task AddNewTabByPathAsync(Type type, string path, int atIndex = -1)
        {
            Microsoft.UI.Xaml.Controls.FontIconSource fontIconSource = new Microsoft.UI.Xaml.Controls.FontIconSource();
            fontIconSource.FontFamily = App.Current.Resources["FluentUIGlyphs"] as FontFamily;

            if (string.IsNullOrEmpty(path))
            {
                path = "NewTab".GetLocalized();
            }

            TabItem tabItem = new TabItem()
            {
                Header = null,
                IconSource = fontIconSource,
                Description = null
            };
            tabItem.Control.NavigationArguments = new TabItemArguments()
            {
                InitialPageType = type,
                NavigationArg = path
            };
            tabItem.Control.ContentChanged += Control_ContentChanged;
            await UpdateTabInfo(tabItem, path);
            AppInstances.Insert(atIndex == -1 ? AppInstances.Count : atIndex, tabItem);
        }

        private static async Task<(string tabLocationHeader, Microsoft.UI.Xaml.Controls.IconSource tabIcon)> GetSelectedTabInfoAsync(string currentPath)
        {
            string tabLocationHeader;
            Microsoft.UI.Xaml.Controls.FontIconSource fontIconSource = new Microsoft.UI.Xaml.Controls.FontIconSource();
            fontIconSource.FontFamily = App.Current.Resources["FluentUIGlyphs"] as FontFamily;

            if (currentPath == null || currentPath == "SidebarSettings/Text".GetLocalized())
            {
                tabLocationHeader = "SidebarSettings/Text".GetLocalized();
                fontIconSource.Glyph = "\xeb5d";
            }
            else if (currentPath == null || currentPath == "NewTab".GetLocalized() || currentPath == "Home")
            {
                tabLocationHeader = "NewTab".GetLocalized();
                fontIconSource.Glyph = "\xe90c";
            }
            else if (currentPath.Equals(App.AppSettings.DesktopPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = "SidebarDesktop".GetLocalized();
                fontIconSource.Glyph = "\xe9f1";
            }
            else if (currentPath.Equals(App.AppSettings.DownloadsPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = "SidebarDownloads".GetLocalized();
                fontIconSource.Glyph = "\xe91c";
            }
            else if (currentPath.Equals(App.AppSettings.DocumentsPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = "SidebarDocuments".GetLocalized();
                fontIconSource.Glyph = "\xEA11";
            }
            else if (currentPath.Equals(App.AppSettings.PicturesPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = "SidebarPictures".GetLocalized();
                fontIconSource.Glyph = "\xEA83";
            }
            else if (currentPath.Equals(App.AppSettings.MusicPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = "SidebarMusic".GetLocalized();
                fontIconSource.Glyph = "\xead4";
            }
            else if (currentPath.Equals(App.AppSettings.VideosPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = "SidebarVideos".GetLocalized();
                fontIconSource.Glyph = "\xec0d";
            }
            else if (currentPath.Equals(App.AppSettings.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                tabLocationHeader = localSettings.Values.Get("RecycleBin_Title", "Recycle Bin");
                fontIconSource.FontFamily = Application.Current.Resources["RecycleBinIcons"] as FontFamily;
                fontIconSource.Glyph = "\xEF87";
            }
            else if (currentPath.Equals(App.AppSettings.NetworkFolderPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = "SidebarNetworkDrives".GetLocalized();
                fontIconSource.Glyph = "\ueac2";
            }
            else
            {
                var matchingCloudDrive = App.CloudDrivesManager.Drives.FirstOrDefault(x => NormalizePath(currentPath).Equals(NormalizePath(x.Path), StringComparison.OrdinalIgnoreCase));
                if (matchingCloudDrive != null)
                {
                    fontIconSource.Glyph = "\xe9b7";
                    tabLocationHeader = matchingCloudDrive.Text;
                }
                else if (NormalizePath(GetPathRoot(currentPath)) == NormalizePath(currentPath)) // If path is a drive's root
                {
                    var matchingNetDrive = App.NetworkDrivesManager.Drives.FirstOrDefault(x => NormalizePath(currentPath).Contains(NormalizePath(x.Path), StringComparison.OrdinalIgnoreCase));
                    if (matchingNetDrive != null)
                    {
                        fontIconSource.Glyph = "\ueac2";
                        tabLocationHeader = matchingNetDrive.Text;
                    }
                    else
                    {
                        try
                        {
                            List<DriveInfo> drives = DriveInfo.GetDrives().ToList();
                            DriveInfo matchingDrive = drives.FirstOrDefault(x => NormalizePath(currentPath).Contains(NormalizePath(x.Name)));

                            if (matchingDrive != null)
                            {
                                // Go through types and set the icon according to type
                                string type = GetDriveTypeIcon(matchingDrive);
                                if (!string.IsNullOrWhiteSpace(type))
                                {
                                    fontIconSource.Glyph = type;
                                }
                                else
                                {
                                    fontIconSource.Glyph = "\xeb8b"; //Drive icon
                                }
                            }
                            else
                            {
                                fontIconSource.Glyph = "\xeb4a"; //Floppy icon
                            }
                        }
                        catch (Exception)
                        {
                            fontIconSource.Glyph = "\xeb8b"; //Fallback
                        }

                        tabLocationHeader = NormalizePath(currentPath);
                    }
                }
                else
                {
                    fontIconSource.Glyph = "\xea55"; //Folder icon
                    tabLocationHeader = currentPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Split('\\', StringSplitOptions.RemoveEmptyEntries).Last();

                    FilesystemResult<StorageFolderWithPath> rootItem = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(currentPath));
                    if (rootItem)
                    {
                        StorageFolder currentFolder = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(currentPath, rootItem));
                        if (currentFolder != null && !string.IsNullOrEmpty(currentFolder.DisplayName))
                        {
                            tabLocationHeader = currentFolder.DisplayName;
                        }
                    }
                }
            }

            return (tabLocationHeader, fontIconSource);
        }

        private static async void Control_ContentChanged(object sender, TabItemArguments e)
        {
            var matchingTabItem = MainPage.AppInstances.SingleOrDefault(x => x.Control == sender);
            if (matchingTabItem == null)
            {
                return;
            }
            await UpdateTabInfo(matchingTabItem, e.NavigationArg);
        }

        private static async Task UpdateTabInfo(TabItem tabItem, object navigationArg)
        {
            tabItem.AllowStorageItemDrop = true;
            if (navigationArg is PaneNavigationArguments paneArgs)
            {
                if (!string.IsNullOrEmpty(paneArgs.LeftPaneNavPathParam) && !string.IsNullOrEmpty(paneArgs.RightPaneNavPathParam))
                {
                    var leftTabInfo = await GetSelectedTabInfoAsync(paneArgs.LeftPaneNavPathParam);
                    var rightTabInfo = await GetSelectedTabInfoAsync(paneArgs.RightPaneNavPathParam);
                    tabItem.Header = $"{leftTabInfo.tabLocationHeader} | {rightTabInfo.tabLocationHeader}";
                    tabItem.IconSource = leftTabInfo.tabIcon;
                }
                else
                {
                    (tabItem.Header, tabItem.IconSource) = await GetSelectedTabInfoAsync(paneArgs.LeftPaneNavPathParam);
                }
            }
            else if (navigationArg is string pathArgs)
            {
                (tabItem.Header, tabItem.IconSource) = await GetSelectedTabInfoAsync(pathArgs);
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

        private void CloseSelectedTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (App.InteractionViewModel.TabStripSelectedIndex >= AppInstances.Count)
            {
                var tabItem = AppInstances[AppInstances.Count - 1];
                MultitaskingControl?.RemoveTab(tabItem);
            }
            else
            {
                var tabItem = AppInstances[App.InteractionViewModel.TabStripSelectedIndex];
                MultitaskingControl?.RemoveTab(tabItem);
            }
            args.Handled = true;
        }

        private bool isRestoringClosedTab = false; // Avoid reopening two tabs

        private async void AddNewInstanceAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var shift = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift);
            if (!shift)
            {
                await AddNewTabByPathAsync(typeof(PaneHolderPage), "NewTab".GetLocalized());
            }
            else // ctrl + shift + t, restore recently closed tab
            {
                if (!isRestoringClosedTab && MultitaskingControl.RecentlyClosedTabs.Any())
                {
                    isRestoringClosedTab = true;
                    var lastTab = MultitaskingControl.RecentlyClosedTabs.Last();
                    MultitaskingControl.RecentlyClosedTabs.Remove(lastTab);
                    await AddNewTabByParam(lastTab.TabItemArguments.InitialPageType, lastTab.TabItemArguments.NavigationArg);
                    isRestoringClosedTab = false;
                }
            }
            args.Handled = true;
        }

        private async void OpenNewWindowAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var filesUWPUri = new Uri("files-uwp:");
            await Launcher.LaunchUriAsync(filesUWPUri);
        }

        private void HorizontalMultitaskingControl_Loaded(object sender, RoutedEventArgs e)
        {
            MultitaskingControl = HorizontalMultitaskingControl;
        }

        private static string GetDriveTypeIcon(DriveInfo drive)
        {
            string type;

            switch (drive.DriveType)
            {
                case System.IO.DriveType.CDRom:
                    type = "\xec39";
                    break;

                case System.IO.DriveType.Fixed:
                    type = "\xeb8b";
                    break;

                case System.IO.DriveType.Network:
                    type = "\xeac2";
                    break;

                case System.IO.DriveType.NoRootDirectory:
                    type = "\xea5a";
                    break;

                case System.IO.DriveType.Ram:
                    type = "\xe9f2";
                    break;

                case System.IO.DriveType.Removable:
                    type = "\xec0a";
                    break;

                case System.IO.DriveType.Unknown:
                    if (NormalizePath(drive.Name) != NormalizePath("A:") && NormalizePath(drive.Name) != NormalizePath("B:"))
                    {
                        type = "\xeb8b";
                    }
                    else
                    {
                        type = "\xeb4a";    //Floppy icon
                    }
                    break;

                default:
                    type = "\xeb8b";    //Drive icon
                    break;
            }

            return type;
        }
    }
}