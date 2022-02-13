﻿using Files.Shared.Extensions;
using Files.Filesystem;
using Files.Filesystem.StorageItems;
using Files.Helpers;
using Files.Services;
using Files.UserControls.MultitaskingControl;
using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
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
using Files.Backend.ViewModels.Shell.Tabs;

#nullable enable

namespace Files.ViewModels
{
    public class MainPageViewModel : ObservableObject
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();










        public MultitaskingControl MultitaskingControl { get; set; }
        private int SelectedTabItemIndex => MultitaskingControl.ViewModel.Tabs.IndexOf(MultitaskingControl.ViewModel.SelectedItem);

        [Obsolete("AppInstances will be removed soon", false)]
        public static ObservableCollection<TabItemViewModel> AppInstances { get; private set; } = new ObservableCollection<TabItemViewModel>();

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
                    indexToSelect = AppInstances.Count - 1;
                    break;

                case VirtualKey.Tab:
                    bool shift = e.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift);
                    int index = SelectedTabItemIndex;
                    if (!shift) // ctrl + tab, select next tab
                    {
                        if ((index + 1) < AppInstances.Count)
                        {
                            indexToSelect = index + 1;
                        }
                        else
                        {
                            indexToSelect = 0;
                        }
                    }
                    else // ctrl + shift + tab, select previous tab
                    {
                        if ((index - 1) >= 0)
                        {
                            indexToSelect = index - 1;
                        }
                        else
                        {
                            indexToSelect = AppInstances.Count - 1;
                        }
                    }

                    break;
            }

            // Only select the tab if it is in the list
            if (indexToSelect < AppInstances.Count)
            {
                MultitaskingControl.ViewModel.SelectedItem = MultitaskingControl.ViewModel.Tabs[indexToSelect];
            }
            e.Handled = true;
        }

        private async void OpenNewWindowAccelerator(KeyboardAcceleratorInvokedEventArgs e)
        {
            e.Handled = true;
            Uri filesUWPUri = new Uri("files-uwp:");
            await Launcher.LaunchUriAsync(filesUWPUri);
        }

        private void CloseSelectedTabKeyboardAccelerator(KeyboardAcceleratorInvokedEventArgs e)
        {
            if (SelectedTabItemIndex >= AppInstances.Count)
            {
                TabItemViewModel tabItem = AppInstances[AppInstances.Count - 1];
                MultitaskingControl?.ViewModel.CloseTab(tabItem);
            }
            else
            {
                TabItemViewModel tabItem = AppInstances[SelectedTabItemIndex];
                MultitaskingControl?.ViewModel.CloseTab(tabItem);
            }
            e.Handled = true;
        }

        private async void AddNewInstanceAccelerator(KeyboardAcceleratorInvokedEventArgs e)
        {
            MultitaskingControl?.ViewModel.AddTab();
            e.Handled = true;
        }

        private void ReopenClosedTabAccelerator(KeyboardAcceleratorInvokedEventArgs e)
        {
            MultitaskingControl.ViewModel.ReopenClosedTab(null, null);
            e.Handled = true;
        }

        public async void UpdateInstanceProperties(object navigationArg)
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
            if (AppInstances.Count > 1)
            {
                windowTitle = $"{windowTitle} ({AppInstances.Count})";
            }
            if (navigationArg == SelectedTabItem?.TabItemArguments?.NavigationArg)
            {
                ApplicationView.GetForCurrentView().Title = windowTitle;
            }
        }

        private void SetLoadingIndicatorForTabs(bool isLoading)
        {
            MultitaskingControl.ViewModel.Tabs.FirstOrDefault(x => x.TabShell == PaneHolder).IsLoading = isLoading;
        }

        public static async Task UpdateTabInfo(TabItemViewModel tabItem, object navigationArg)
        {
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
                var libName = System.IO.Path.GetFileNameWithoutExtension(library.Path);
                switch (libName)
                {
                    case "Documents":
                        tabLocationHeader = $"Sidebar{libName}".GetLocalized(); // Show localized name
                        break;

                    case "Pictures":
                        tabLocationHeader = $"Sidebar{libName}".GetLocalized(); // Show localized name
                        break;

                    case "Music":
                        tabLocationHeader = $"Sidebar{libName}".GetLocalized(); // Show localized name
                        break;

                    case "Videos":
                        tabLocationHeader = $"Sidebar{libName}".GetLocalized(); // Show localized name
                        break;

                    default:
                        tabLocationHeader = library.Text; // Show original name
                        break;
                }
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
                                await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
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
                                    await AddNewTabByPathAsync(typeof(PaneHolderPage), path);
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
                                    await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
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
                        await AddNewTabByPathAsync(typeof(PaneHolderPage), navArgs);
                    }
                    else if (e.Parameter is PaneNavigationArguments paneArgs)
                    {
                        await AddNewTabByParam(typeof(PaneHolderPage), paneArgs);
                    }
                    else if (e.Parameter is TabItemArguments tabArgs)
                    {
                        await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg);
                    }
                }
            }
        }

        

        // TODO: Remove this and use TabItemViewModel.SetDisplayInformation() somewhere
        public static async void Control_ContentChanged(object sender, TabItemArguments e)
        {
            TabItem matchingTabItem = control.SingleOrDefault(x => x.Control == sender);
            if (matchingTabItem == null)
            {
                return;
            }
            await UpdateTabInfo(matchingTabItem, e.NavigationArg);
        }
    }
}
