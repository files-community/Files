using Files.Shared.Extensions;
using Files.Filesystem;
using Files.Filesystem.StorageItems;
using Files.Helpers;
using Files.Backend.Services.Settings;
using Files.UserControls.MultitaskingControl;
using Files.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Files.ViewModels
{
    public class MainPageViewModel : ObservableObject
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        private IMultitaskingControl _multitaskingControl;
        public IMultitaskingControl MultitaskingControl
        {
            get => _multitaskingControl;
            set => SetProperty<IMultitaskingControl>(ref _multitaskingControl, value);
        }

        public List<IMultitaskingControl> MultitaskingControls { get; } = new List<IMultitaskingControl>();

        public ObservableCollection<TabItem> TabInstances { get; private set; } = new ObservableCollection<TabItem>();

        public ICommand NavigateToNumberedTabKeyboardAcceleratorCommand { get; private set; }

        public ICommand OpenNewWindowAcceleratorCommand { get; private set; }

        public ICommand CloseSelectedTabKeyboardAcceleratorCommand { get; private set; }

        public ICommand AddNewInstanceAcceleratorCommand { get; private set; }

        public ICommand ReopenClosedTabAcceleratorCommand { get; private set; }

        public MainPageViewModel()
        {
            // Create commands
            NavigateToNumberedTabKeyboardAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(NavigateToNumberedTabKeyboardAccelerator);
            OpenNewWindowAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(OpenNewWindowAccelerator);
            CloseSelectedTabKeyboardAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(CloseSelectedTabKeyboardAccelerator);
            AddNewInstanceAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(AddNewInstanceAccelerator);
            ReopenClosedTabAcceleratorCommand = new RelayCommand<KeyboardAcceleratorInvokedEventArgs>(ReopenClosedTabAccelerator);
        }

        private void NavigateToNumberedTabKeyboardAccelerator(KeyboardAcceleratorInvokedEventArgs e)
        {
            int indexToSelect = 0;

            switch (e.KeyboardAccelerator.Key)
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
                    indexToSelect = TabInstances.Count - 1;
                    break;

                case VirtualKey.Tab:
                    bool shift = e.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift);

                    if (!shift) // ctrl + tab, select next tab
                    {
                        if ((App.MainViewModel.TabStripSelectedIndex + 1) < TabInstances.Count)
                        {
                            indexToSelect = App.MainViewModel.TabStripSelectedIndex + 1;
                        }
                        else
                        {
                            indexToSelect = 0;
                        }
                    }
                    else // ctrl + shift + tab, select previous tab
                    {
                        if ((App.MainViewModel.TabStripSelectedIndex - 1) >= 0)
                        {
                            indexToSelect = App.MainViewModel.TabStripSelectedIndex - 1;
                        }
                        else
                        {
                            indexToSelect = TabInstances.Count - 1;
                        }
                    }

                    break;
            }

            // Only select the tab if it is in the list
            if (indexToSelect < TabInstances.Count)
            {
                App.MainViewModel.TabStripSelectedIndex = indexToSelect;
            }
            e.Handled = true;
        }

        private void OpenNewWindowAccelerator(KeyboardAcceleratorInvokedEventArgs e)
        {
            e.Handled = true;
            NavigationHelpers.LaunchNewWindow();
        }

        private void CloseSelectedTabKeyboardAccelerator(KeyboardAcceleratorInvokedEventArgs e)
        {
            if (App.MainViewModel.TabStripSelectedIndex >= TabInstances.Count)
            {
                TabItem tabItem = TabInstances[TabInstances.Count - 1];
                MultitaskingControl?.CloseTab(tabItem);
            }
            else
            {
                TabItem tabItem = TabInstances[App.MainViewModel.TabStripSelectedIndex];
                MultitaskingControl?.CloseTab(tabItem);
            }
            e.Handled = true;
        }

        private async void AddNewInstanceAccelerator(KeyboardAcceleratorInvokedEventArgs e)
        {
            await AddNewTabAsync();
            e.Handled = true;
        }

        private void ReopenClosedTabAccelerator(KeyboardAcceleratorInvokedEventArgs e)
        {
            ((BaseMultitaskingControl)MultitaskingControl).ReopenClosedTab(null, null);
            e.Handled = true;
        }

        public async Task<string> UpdateInstancePropertiesAsync(object navigationArg)
        {
            string windowTitle = string.Empty;
            if (navigationArg is PaneNavigationArguments paneArgs)
            {
                if (!string.IsNullOrEmpty(paneArgs.LeftPaneNavPathParam) && !string.IsNullOrEmpty(paneArgs.RightPaneNavPathParam))
                {
                    var leftTabInfo = await GetSelectedTabInfoAsync(paneArgs.LeftPaneNavPathParam);
                    var rightTabInfo = await GetSelectedTabInfoAsync(paneArgs.RightPaneNavPathParam);
                    windowTitle = $"{leftTabInfo.tabLocationHeader} | {rightTabInfo.tabLocationHeader}";
                }
                else
                {
                    (windowTitle, _) = await GetSelectedTabInfoAsync(paneArgs.LeftPaneNavPathParam);
                }
            }
            else if (navigationArg is string pathArgs)
            {
                (windowTitle, _) = await GetSelectedTabInfoAsync(pathArgs);
            }
            if (TabInstances.Count > 1)
            {
                windowTitle = $"{windowTitle} ({TabInstances.Count})";
            }
            return windowTitle;
        }

        public static async Task UpdateTabInfo(TabItem tabItem, object navigationArg)
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

        public static async Task<(string tabLocationHeader, Microsoft.UI.Xaml.Controls.IconSource tabIcon)> GetSelectedTabInfoAsync(string currentPath)
        {
            string tabLocationHeader;
            var iconSource = new Microsoft.UI.Xaml.Controls.ImageIconSource();

            if (string.IsNullOrEmpty(currentPath) || currentPath == "Home".GetLocalized())
            {
                tabLocationHeader = "Home".GetLocalized();
                iconSource.ImageSource = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/FluentIcons/Home.png"));
            }
            else if (currentPath.Equals(CommonPaths.DesktopPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = "Desktop".GetLocalized();
            }
            else if (currentPath.Equals(CommonPaths.DownloadsPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = "Downloads".GetLocalized();
            }
            else if (currentPath.Equals(CommonPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                tabLocationHeader = localSettings.Values.Get("RecycleBin_Title", "Recycle Bin");
            }
            else if (currentPath.Equals(CommonPaths.NetworkFolderPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = "SidebarNetworkDrives".GetLocalized();
            }
            else if (App.LibraryManager.TryGetLibrary(currentPath, out LibraryLocationItem library))
            {
                var libName = System.IO.Path.GetFileNameWithoutExtension(library.Path).GetLocalized();
                // If localized string is empty use the library name.
                tabLocationHeader = string.IsNullOrEmpty(libName) ? library.Text : libName;
            }
            else
            {
                var matchingCloudDrive = App.CloudDrivesManager.Drives.FirstOrDefault(x => PathNormalization.NormalizePath(currentPath).Equals(PathNormalization.NormalizePath(x.Path), StringComparison.OrdinalIgnoreCase));
                if (matchingCloudDrive != null)
                {
                    tabLocationHeader = matchingCloudDrive.Text;
                }
                else if (PathNormalization.NormalizePath(PathNormalization.GetPathRoot(currentPath)) == PathNormalization.NormalizePath(currentPath)) // If path is a drive's root
                {
                    var matchingNetDrive = App.NetworkDrivesManager.Drives.FirstOrDefault(x => PathNormalization.NormalizePath(currentPath).Contains(PathNormalization.NormalizePath(x.Path), StringComparison.OrdinalIgnoreCase));
                    if (matchingNetDrive != null)
                    {
                        tabLocationHeader = matchingNetDrive.Text;
                    }
                    else
                    {
                        tabLocationHeader = PathNormalization.NormalizePath(currentPath);
                    }
                }
                else
                {
                    tabLocationHeader = currentPath.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar).Split('\\', StringSplitOptions.RemoveEmptyEntries).Last();

                    FilesystemResult<StorageFolderWithPath> rootItem = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(currentPath));
                    if (rootItem)
                    {
                        BaseStorageFolder currentFolder = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(currentPath, rootItem));
                        if (currentFolder != null && !string.IsNullOrEmpty(currentFolder.DisplayName))
                        {
                            tabLocationHeader = currentFolder.DisplayName;
                        }
                    }
                }
            }

            if (iconSource.ImageSource == null)
            {
                var iconData = await FileThumbnailHelper.LoadIconFromPathAsync(currentPath, 24u, Windows.Storage.FileProperties.ThumbnailMode.ListView);
                if (iconData != null)
                {
                    iconSource.ImageSource = await iconData.ToBitmapAsync();
                }
            }

            return (tabLocationHeader, iconSource);
        }

        public async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.NavigationMode != NavigationMode.Back)
            {
                

                if (e.Parameter == null || (e.Parameter is string eventStr && string.IsNullOrEmpty(eventStr)))
                {
                    try
                    {
                        // add last session tabs to closed tabs stack if those tabs are not about to be opened
                        if (!App.AppSettings.ResumeAfterRestart && !UserSettingsService.PreferencesSettingsService.ContinueLastSessionOnStartUp && UserSettingsService.PreferencesSettingsService.LastSessionTabList != null)
                        {
                            var items = new TabItemArguments[UserSettingsService.PreferencesSettingsService.LastSessionTabList.Count];
                            for (int i = 0; i < items.Length; i++)
                            {
                                var tabArgs = TabItemArguments.Deserialize(UserSettingsService.PreferencesSettingsService.LastSessionTabList[i]);
                                items[i] = tabArgs;
                            }
                            BaseMultitaskingControl.RecentlyClosedTabs.Add(items);
                        }

                        if (App.AppSettings.ResumeAfterRestart)
                        {
                            App.AppSettings.ResumeAfterRestart = false;

                            foreach (string tabArgsString in UserSettingsService.PreferencesSettingsService.LastSessionTabList)
                            {
                                var tabArgs = TabItemArguments.Deserialize(tabArgsString);
                                await MultitaskingControl.AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg.ToString());
                            }

                            if (!UserSettingsService.PreferencesSettingsService.ContinueLastSessionOnStartUp)
                            {
                                UserSettingsService.PreferencesSettingsService.LastSessionTabList = null;
                            }
                        }
                        else if (UserSettingsService.PreferencesSettingsService.OpenSpecificPageOnStartup)
                        {
                            if (UserSettingsService.PreferencesSettingsService.TabsOnStartupList != null)
                            {
                                foreach (string path in UserSettingsService.PreferencesSettingsService.TabsOnStartupList)
                                {
                                    await MultitaskingControl.AddNewTabByPathAsync(typeof(PaneHolderPage), path);
                                }
                            }
                            else
                            {
                                await AddNewTabAsync();
                            }
                        }
                        else if (UserSettingsService.PreferencesSettingsService.ContinueLastSessionOnStartUp)
                        {
                            if (UserSettingsService.PreferencesSettingsService.LastSessionTabList != null)
                            {
                                foreach (string tabArgsString in UserSettingsService.PreferencesSettingsService.LastSessionTabList)
                                {
                                    var tabArgs = TabItemArguments.Deserialize(tabArgsString);
                                    await MultitaskingControl.AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg.ToString());
                                }
                                var defaultArg = new TabItemArguments() { InitialPageType = typeof(PaneHolderPage), NavigationArg = "Home".GetLocalized() };
                                UserSettingsService.PreferencesSettingsService.LastSessionTabList = new List<string> { defaultArg.Serialize() };
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
                    if (e.Parameter is string navArgs)
                    {
                        await MultitaskingControl.AddNewTabByPathAsync(typeof(PaneHolderPage), navArgs);
                    }
                    else if (e.Parameter is PaneNavigationArguments paneArgs)
                    {
                        await MultitaskingControl.AddNewTabByParam(typeof(PaneHolderPage), paneArgs.ToString());
                    }
                    else if (e.Parameter is TabItemArguments tabArgs)
                    {
                        await MultitaskingControl.AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg.ToString());
                    }
                }
            }
        }

        public async Task AddNewTabAsync()
        {
            await MultitaskingControl.AddNewTabByPathAsync(typeof(PaneHolderPage), "Home".GetLocalized());
        }

        public async void AddNewTab()
        {
            await AddNewTabAsync();
        }
    }
}
