using Files.Commands;
using Files.Common;
using Files.Interacts;
using Files.UserControls.MultiTaskingControl;
using Files.View_Models;
using Files.Views;
using Files.Views.Pages;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using static Files.Helpers.PathNormalization;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class HorizontalMultitaskingControl : UserControl, IMultitaskingControl
    {
        private const string TabDropHandledIdentifier = "FilesTabViewItemDropHandled";

        public const string TabPathIdentifier = "FilesTabViewItemPath";

        public HorizontalMultitaskingControl()
        {
            this.InitializeComponent();
        }

        public SettingsViewModel AppSettings => App.AppSettings;

        public void SelectionChanged() => TabStrip_SelectionChanged(null, null);

        public ObservableCollection<TabItem> Items => MainPage.AppInstances;

        public async Task SetSelectedTabInfo(string text, string currentPathForTabIcon)
        {
            var selectedTabItem = MainPage.AppInstances[App.InteractionViewModel.TabStripSelectedIndex];
            selectedTabItem.AllowStorageItemDrop = App.CurrentInstance.InstanceViewModel.IsPageTypeNotHome;

            MainPage.AppInstances[App.InteractionViewModel.TabStripSelectedIndex].Path = currentPathForTabIcon;

            string tabLocationHeader;
            Microsoft.UI.Xaml.Controls.FontIconSource fontIconSource = new Microsoft.UI.Xaml.Controls.FontIconSource();
            Microsoft.UI.Xaml.Controls.IconSource tabIcon;
            fontIconSource.FontFamily = App.Current.Resources["FluentUIGlyphs"] as FontFamily;

            if (currentPathForTabIcon == null && text == ResourceController.GetTranslation("SidebarSettings/Text"))
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarSettings/Text");
                fontIconSource.Glyph = "\xeb5d";
            }
            else if (currentPathForTabIcon == null && text == ResourceController.GetTranslation("NewTab"))
            {
                tabLocationHeader = ResourceController.GetTranslation("NewTab");
                fontIconSource.Glyph = "\xe90c";
            }
            else if (currentPathForTabIcon.Equals(App.AppSettings.DesktopPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarDesktop");
                fontIconSource.Glyph = "\xe9f1";
            }
            else if (currentPathForTabIcon.Equals(App.AppSettings.DownloadsPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarDownloads");
                fontIconSource.Glyph = "\xe91c";
            }
            else if (currentPathForTabIcon.Equals(App.AppSettings.DocumentsPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarDocuments");
                fontIconSource.Glyph = "\xEA11";
            }
            else if (currentPathForTabIcon.Equals(App.AppSettings.PicturesPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarPictures");
                fontIconSource.Glyph = "\xEA83";
            }
            else if (currentPathForTabIcon.Equals(App.AppSettings.MusicPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarMusic");
                fontIconSource.Glyph = "\xead4";
            }
            else if (currentPathForTabIcon.Equals(App.AppSettings.VideosPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarVideos");
                fontIconSource.Glyph = "\xec0d";
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
                fontIconSource.Glyph = "\xe9b7";
            }
            else
            {
                // If path is a drive's root
                if (NormalizePath(Path.GetPathRoot(currentPathForTabIcon)) == NormalizePath(currentPathForTabIcon))
                {
                    if (NormalizePath(currentPathForTabIcon) != NormalizePath("A:") && NormalizePath(currentPathForTabIcon) != NormalizePath("B:"))
                    {
                        var remDriveNames = (await KnownFolders.RemovableDevices.GetFoldersAsync()).Select(x => x.DisplayName);
                        var matchingDriveName = remDriveNames.FirstOrDefault(x => NormalizePath(currentPathForTabIcon).Contains(x.ToUpperInvariant()));

                        if (matchingDriveName == null)
                        {
                            fontIconSource.Glyph = "\xeb8b";
                            tabLocationHeader = NormalizePath(currentPathForTabIcon);
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
                        tabLocationHeader = NormalizePath(currentPathForTabIcon);
                    }
                }
                else
                {
                    fontIconSource.Glyph = "\xea55";
                    tabLocationHeader = currentPathForTabIcon.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Split('\\', StringSplitOptions.RemoveEmptyEntries).Last();
                }
            }
            tabIcon = fontIconSource;
            selectedTabItem.Header = tabLocationHeader;
            selectedTabItem.IconSource = tabIcon;
        }

        public void TabStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (App.InteractionViewModel.TabStripSelectedIndex >= 0 && App.InteractionViewModel.TabStripSelectedIndex < Items.Count)
            {
                Microsoft.UI.Xaml.Controls.FontIconSource icon = new Microsoft.UI.Xaml.Controls.FontIconSource();
                icon.Glyph = "\xE713";
                if (Items[App.InteractionViewModel.TabStripSelectedIndex].Header.ToString() != ResourceController.GetTranslation("SidebarSettings/Text")
                    && Items[App.InteractionViewModel.TabStripSelectedIndex].IconSource != icon)
                {
                    App.CurrentInstance = GetCurrentSelectedTabInstance<ModernShellPage>();
                }

                if (Items[App.InteractionViewModel.TabStripSelectedIndex].Header.ToString() == ResourceController.GetTranslation("SidebarSettings/Text"))
                {
                    App.InteractionViewModel.TabsLeftMargin = new Thickness(0, 0, 0, 0);
                    App.InteractionViewModel.LeftMarginLoaded = false;
                }
                else
                {
                    if (App.CurrentInstance != null)
                    {
                        if (Items[App.InteractionViewModel.TabStripSelectedIndex].Header.ToString() == ResourceController.GetTranslation("NewTab"))
                        {
                            App.CurrentInstance.InstanceViewModel.IsPageTypeNotHome = false;
                        }
                        else
                        {
                            App.CurrentInstance.InstanceViewModel.IsPageTypeNotHome = true;
                        }

                        App.CurrentInstance.InstanceViewModel.IsPageTypeRecycleBin =
                            App.CurrentInstance?.FilesystemViewModel?.WorkingDirectory?.StartsWith(App.AppSettings.RecycleBinPath) ?? false;
                        App.CurrentInstance.InstanceViewModel.IsPageTypeMtpDevice =
                            App.CurrentInstance?.FilesystemViewModel?.WorkingDirectory?.StartsWith("\\\\?\\") ?? false;
                    }

                    App.InteractionViewModel.TabsLeftMargin = new Thickness(0, 0, 0, 0);
                    App.InteractionViewModel.LeftMarginLoaded = true;
                }
            }
        }

        private void TabStrip_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            RemoveTab(args.Item as TabItem);
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

        private void DragArea_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SetTitleBar(sender as Grid);
        }

        private async void TabView_AddTabButtonClick(TabView sender, object args)
        {
            await MainPage.AddNewTab(typeof(ModernShellPage), ResourceController.GetTranslation("NewTab"));
        }

        private void HorizontalTabView_TabItemsChanged(TabView sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
        {
            switch (args.CollectionChange)
            {
                case Windows.Foundation.Collections.CollectionChange.ItemRemoved:
                    App.InteractionViewModel.TabStripSelectedIndex = Items.IndexOf(HorizontalTabView.SelectedItem as TabItem);
                    break;

                case Windows.Foundation.Collections.CollectionChange.ItemInserted:
                    App.InteractionViewModel.TabStripSelectedIndex = Items.IndexOf(HorizontalTabView.SelectedItem as TabItem);
                    break;
            }
        }

        private void TabViewItem_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.AvailableFormats.Contains(StandardDataFormats.StorageItems))
            {
                // TODO: Add Simpler way to find TabItem working directory
                string tabViewItemWorkingDir = ((((
                    (sender as TabViewItem)
                    .DataContext as TabItem)
                    .Content as Grid).Children[0] as Frame)
                    .Content as IShellPage)
                    .FilesystemViewModel
                    .WorkingDirectory;

                ItemOperations.PasteItemWithStatus(e.DataView, tabViewItemWorkingDir, DataPackageOperation.Move);
            }
            else
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
        }

        private void TabViewItem_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.AvailableFormats.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Move;
            }
            else
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
        }

        private void TabStrip_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
        {
            var tabViewItemPath = ((((args.Item as TabItem).Content as Grid).Children[0] as Frame).Tag as TabItemContent).NavigationArg;
            args.Data.Properties.Add(TabPathIdentifier, tabViewItemPath);
            args.Data.RequestedOperation = DataPackageOperation.Move;
        }

        private void TabStrip_TabStripDragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.ContainsKey(TabPathIdentifier))
            {
                e.AcceptedOperation = DataPackageOperation.Move;
                e.DragUIOverride.Caption = ResourceController.GetTranslation("TabStripDragAndDropUIOverrideCaption");
                e.DragUIOverride.IsCaptionVisible = true;
                e.DragUIOverride.IsGlyphVisible = false;
            }
        }

        private async void TabStrip_TabStripDrop(object sender, DragEventArgs e)
        {
            if (!(sender is TabView tabStrip))
            {
                return;
            }

            if (!e.DataView.Properties.TryGetValue(TabPathIdentifier, out object tabViewItemPathObj) || !(tabViewItemPathObj is string tabViewItemPath))
            {
                return;
            }

            var index = -1;

            for (int i = 0; i < tabStrip.TabItems.Count; i++)
            {
                var item = tabStrip.ContainerFromIndex(i) as TabViewItem;

                if (e.GetPosition(item).Y - item.ActualHeight < 0)
                {
                    index = i;
                    break;
                }
            }

            ApplicationData.Current.LocalSettings.Values[TabDropHandledIdentifier] = true;
            await MainPage.AddNewTab(typeof(ModernShellPage), tabViewItemPath, index);
        }

        private void TabStrip_TabDragCompleted(TabView sender, TabViewTabDragCompletedEventArgs args)
        {
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(TabDropHandledIdentifier) &&
                (bool)ApplicationData.Current.LocalSettings.Values[TabDropHandledIdentifier])
            {
                RemoveTab(args.Item as TabItem);
            }

            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(TabDropHandledIdentifier))
            {
                ApplicationData.Current.LocalSettings.Values.Remove(TabDropHandledIdentifier);
            }
        }

        private async void TabStrip_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
        {
            if (sender.TabItems.Count == 1)
            {
                return;
            }

            var indexOfTabViewItem = sender.TabItems.IndexOf(args.Tab);
            var tabViewItemPath = ((((args.Item as TabItem).Content as Grid).Children[0] as Frame).Tag as TabItemContent).NavigationArg;
            var selectedTabViewItemIndex = sender.SelectedIndex;
            RemoveTab(args.Item as TabItem);
            if (!await Interaction.OpenPathInNewWindow(tabViewItemPath))
            {
                sender.TabItems.Insert(indexOfTabViewItem, args.Tab);
                sender.SelectedIndex = selectedTabViewItemIndex;
            }
        }

        private void RemoveTab(TabItem tabItem)
        {
            if (Items.Count == 1)
            {
                MainPage.AppInstances.Remove(tabItem);
                App.CloseApp();
            }
            else if (Items.Count > 1)
            {
                Items.Remove(tabItem);
            }
        }
    }
}