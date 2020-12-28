using Files.Common;
using Files.Controllers;
using Files.Filesystem;
using Files.Helpers;
using Files.UserControls.MultitaskingControl;
using Files.ViewModels;
using Microsoft.Toolkit.Uwp.Extensions;
using Newtonsoft.Json;
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
        }

        protected override async void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            if (eventArgs.NavigationMode != NavigationMode.Back)
            {
                App.AppSettings = new SettingsViewModel();
                App.InteractionViewModel = new InteractionViewModel();
                App.SidebarPinnedController = new SidebarPinnedController();

                Helpers.ThemeHelper.Initialize();

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
                                AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
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
                                    AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
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
                        AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
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
                AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
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

        public static void AddNewTabByParam(Type type, object tabViewItemArgs, int atIndex = -1)
        {
            TabItem tabItem = new TabItem()
            {
                Description = null
            };
            tabItem.Control.NavigationArguments = new TabItemArguments()
            {
                InitialPageType = type,
                NavigationArg = tabViewItemArgs
            };
            AppInstances.Insert(atIndex == -1 ? AppInstances.Count : atIndex, tabItem);
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
                else if (path.Equals(App.AppSettings.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    tabLocationHeader = localSettings.Values.Get("RecycleBin_Title", "Recycle Bin");
                    fontIconSource.FontFamily = Application.Current.Resources["RecycleBinIcons"] as FontFamily;
                    fontIconSource.Glyph = "\xEF87";
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
                IconSource = fontIconSource,
                Description = null
            };
            tabItem.Control.NavigationArguments = new TabItemArguments()
            {
                InitialPageType = type,
                NavigationArg = path
            };
            AppInstances.Insert(atIndex == -1 ? AppInstances.Count : atIndex, tabItem);
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
            var shift = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift);
            if (!shift)
            {
                await AddNewTabByPathAsync(typeof(PaneHolderPage), "NewTab".GetLocalized());
            }
            else // ctrl + shif + t, restore recently closed tab
            {
                if (!MultitaskingControl.RestoredRecentlyClosedTab && MultitaskingControl.Items.Count > 0)
                {
                    var tabArgs = MultitaskingControl.RecentlyClosedTabs.Last().TabItemArguments;
                    AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
                    MultitaskingControl.RestoredRecentlyClosedTab = true;
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
    }
}