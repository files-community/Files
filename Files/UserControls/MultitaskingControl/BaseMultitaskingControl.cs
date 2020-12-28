using Files.Common;
using Files.Views;
using Microsoft.Toolkit.Uwp.Extensions;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using static Files.Helpers.PathNormalization;

namespace Files.UserControls.MultitaskingControl
{
    public class BaseMultitaskingControl : UserControl, IMultitaskingControl
    {
        protected ITabItemContent CurrentSelectedAppInstance;

        public const string TabDropHandledIdentifier = "FilesTabViewItemDropHandled";

        public const string TabPathIdentifier = "FilesTabViewItemPath";

        public event IMultitaskingControl.CurrentInstanceChangedEventHandler CurrentInstanceChanged;

        public void SelectionChanged() => TabStrip_SelectionChanged(null, null);

        public BaseMultitaskingControl()
        {
            Loaded += MultitaskingControl_Loaded;
        }

        public ObservableCollection<TabItem> Items => MainPage.AppInstances;

        public List<ITabItem> RecentlyClosedTabs { get; private set; } = new List<ITabItem>();

        public bool RestoredRecentlyClosedTab { get; set; } = true; // True here is the default value

        private void MultitaskingControl_CurrentInstanceChanged(object sender, CurrentInstanceChangedEventArgs e)
        {
            foreach (ITabItemContent instance in e.PageInstances)
            {
                if (instance != null)
                {
                    instance.IsCurrentInstance = instance == e.CurrentInstance;
                }
            }
        }

        private void SetSelectedTabInfoForSearchResults()
        {
            var selectedTabItem = MainPage.AppInstances[App.InteractionViewModel.TabStripSelectedIndex];
            selectedTabItem.AllowStorageItemDrop = false;
            string tabLocationHeader = "SearchTabHeaderText".GetLocalized();
            Microsoft.UI.Xaml.Controls.FontIconSource fontIconSource = new Microsoft.UI.Xaml.Controls.FontIconSource();
            fontIconSource.Glyph = "\xEB51";
            fontIconSource.FontFamily = App.Current.Resources["FluentUIGlyphs"] as FontFamily;

            selectedTabItem.Header = tabLocationHeader;
            selectedTabItem.IconSource = fontIconSource;
        }

        private async Task SetSelectedTabInfoAsync(string tabHeader, string currentPath = null)
        {
            var selectedTabItem = MainPage.AppInstances[App.InteractionViewModel.TabStripSelectedIndex];
            //selectedTabItem.AllowStorageItemDrop = CurrentSelectedAppInstance.InstanceViewModel.IsPageTypeNotHome;

            //MainPage.AppInstances[App.InteractionViewModel.TabStripSelectedIndex].Path = currentPath; //TODO

            string tabLocationHeader;
            Microsoft.UI.Xaml.Controls.FontIconSource fontIconSource = new Microsoft.UI.Xaml.Controls.FontIconSource();
            Microsoft.UI.Xaml.Controls.IconSource tabIcon;
            fontIconSource.FontFamily = App.Current.Resources["FluentUIGlyphs"] as FontFamily;

            if (currentPath == null && tabHeader == "SidebarSettings/Text".GetLocalized())
            {
                tabLocationHeader = "SidebarSettings/Text".GetLocalized();
                fontIconSource.Glyph = "\xeb5d";
            }
            else if (currentPath == null && tabHeader == "NewTab".GetLocalized())
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
            else if (App.AppSettings.OneDrivePath != null && currentPath.Equals(App.AppSettings.OneDrivePath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = "OneDrive";
                fontIconSource.Glyph = "\xe9b7";
            }
            else if (App.AppSettings.OneDriveCommercialPath != null && currentPath.Equals(App.AppSettings.OneDriveCommercialPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = "OneDrive Commercial";
                fontIconSource.Glyph = "\xe9b7";
            }
            else
            {
                // If path is a drive's root
                if (NormalizePath(Path.GetPathRoot(currentPath)) == NormalizePath(currentPath))
                {
                    if (NormalizePath(currentPath) != NormalizePath("A:") && NormalizePath(currentPath) != NormalizePath("B:"))
                    {
                        var remDriveNames = (await KnownFolders.RemovableDevices.GetFoldersAsync()).Select(x => x.DisplayName);
                        var matchingDriveName = remDriveNames.FirstOrDefault(x => NormalizePath(currentPath).Contains(x.ToUpperInvariant()));

                        if (matchingDriveName == null)
                        {
                            fontIconSource.Glyph = "\xeb8b";
                            tabLocationHeader = NormalizePath(currentPath);
                        }
                        else
                        {
                            fontIconSource.Glyph = "\xec0a";
                            tabLocationHeader = matchingDriveName;
                        }
                    }
                    else
                    {
                        fontIconSource.Glyph = "\xeb4a";
                        tabLocationHeader = NormalizePath(currentPath);
                    }
                }
                else
                {
                    fontIconSource.Glyph = "\xea55";
                    tabLocationHeader = currentPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Split('\\', StringSplitOptions.RemoveEmptyEntries).Last();
                }
            }
            tabIcon = fontIconSource;
            selectedTabItem.Header = tabLocationHeader;
            selectedTabItem.IconSource = tabIcon;
        }

        protected void TabStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (App.InteractionViewModel.TabStripSelectedIndex >= 0 && App.InteractionViewModel.TabStripSelectedIndex < Items.Count)
            {
                CurrentSelectedAppInstance = GetCurrentSelectedTabInstance();

                if (CurrentSelectedAppInstance != null)
                {
                    CurrentInstanceChanged?.Invoke(this, new CurrentInstanceChangedEventArgs()
                    {
                        CurrentInstance = CurrentSelectedAppInstance,
                        PageInstances = GetAllTabInstances()
                    });
                }
            }
        }

        protected void TabStrip_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            RemoveTab(args.Item as TabItem);
        }

        protected async void TabView_AddTabButtonClick(TabView sender, object args)
        {
            await MainPage.AddNewTabAsync();
        }

        public void MultitaskingControl_Loaded(object sender, RoutedEventArgs e)
        {
            CurrentInstanceChanged += MultitaskingControl_CurrentInstanceChanged;
        }

        public async void UpdateSelectedTab(string tabHeader, string workingDirectoryPath, bool searchResultsTab)
        {
            SelectionChanged();
            if (searchResultsTab)
            {
                SetSelectedTabInfoForSearchResults();
            }
            else
            {
                await SetSelectedTabInfoAsync(tabHeader, workingDirectoryPath);
            }
        }

        public ITabItemContent GetCurrentSelectedTabInstance()
        {
            return MainPage.AppInstances[App.InteractionViewModel.TabStripSelectedIndex].Control?.TabItemContent;
        }

        public List<ITabItemContent> GetAllTabInstances()
        {
            return MainPage.AppInstances.Select(x => x.Control?.TabItemContent).ToList();
        }

        public void RemoveTab(TabItem tabItem)
        {
            if (Items.Count == 1)
            {
                App.CloseApp();
            }
            else if (Items.Count > 1)
            {
                Items.Remove(tabItem);
                tabItem?.Unload(); // Dispose and save tab arguments
                RecentlyClosedTabs.Add((ITabItem)tabItem);
                RestoredRecentlyClosedTab = false;
            }
        }
    }
}