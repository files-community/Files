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
        protected IShellPage CurrentSelectedAppInstance;

        public const string TabDropHandledIdentifier = "FilesTabViewItemDropHandled";

        public const string TabPathIdentifier = "FilesTabViewItemPath";

        public event IMultitaskingControl.CurrentInstanceChangedEventHandler CurrentInstanceChanged;

        public void SelectionChanged() => TabStrip_SelectionChanged(null, null);

        public BaseMultitaskingControl()
        {
            Loaded += MultitaskingControl_Loaded;
        }

        public ObservableCollection<TabItem> Items => MainPage.AppInstances;

        private void MultitaskingControl_CurrentInstanceChanged(object sender, CurrentInstanceChangedEventArgs e)
        {
            foreach (IShellPage instance in e.ShellPageInstances)
            {
                if (instance != null)
                {
                    instance.IsCurrentInstance = instance == e.CurrentInstance;
                }
            }
        }

        private async Task SetSelectedTabInfoAsync(string tabHeader, string currentPath = null)
        {
            var selectedTabItem = MainPage.AppInstances[App.InteractionViewModel.TabStripSelectedIndex];
            selectedTabItem.AllowStorageItemDrop = CurrentSelectedAppInstance.InstanceViewModel.IsPageTypeNotHome;

            MainPage.AppInstances[App.InteractionViewModel.TabStripSelectedIndex].Path = currentPath;

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
                CurrentSelectedAppInstance = GetCurrentSelectedTabInstance<IShellPage>();

                if (CurrentSelectedAppInstance != null)
                {
                    CurrentInstanceChanged?.Invoke(this, new CurrentInstanceChangedEventArgs() { CurrentInstance = CurrentSelectedAppInstance, ShellPageInstances = GetAllTabInstances<IShellPage>() });
                }
            }
        }

        protected void TabStrip_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            RemoveTab(args.Item as TabItem);
        }

        protected void TabView_AddTabButtonClick(TabView sender, object args)
        {
            MainPage.AddNewTab();
        }

        public void MultitaskingControl_Loaded(object sender, RoutedEventArgs e)
        {
            CurrentInstanceChanged += MultitaskingControl_CurrentInstanceChanged;
        }

        public async void UpdateSelectedTab(string tabHeader, string workingDirectoryPath)
        {
            SelectionChanged();
            await SetSelectedTabInfoAsync(tabHeader, workingDirectoryPath);
        }

        public static T GetCurrentSelectedTabInstance<T>()
        {
            var selectedTabContent = MainPage.AppInstances[App.InteractionViewModel.TabStripSelectedIndex].Content as Grid;
            foreach (UIElement uiElement in selectedTabContent.Children)
            {
                if (uiElement.GetType() == typeof(Frame))
                {
                    return (T)(uiElement as Frame).Content;
                }
            }
            return default;
        }

        public List<T> GetAllTabInstances<T>()
        {
            var instances = new List<T>();
            foreach (TabItem ti in MainPage.AppInstances)
            {
                instances.Add((T)((ti.Content as Grid).Children.First(element => element.GetType() == typeof(Frame)) as Frame).Content);
            }
            return instances;
        }

        public void RemoveTab(TabItem tabItem, bool closeApp = true)
        {
            if (Items.Count == 1)
            {
                Items.Remove(tabItem);
                if (closeApp)
                {
                    App.CloseApp();
                }
                else
                {
                    MainPage.AddNewTab();
                }
            }
            else if (Items.Count > 1)
            {
                Items.Remove(tabItem);
            }
        }
    }
}
