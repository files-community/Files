using Files.Common;
using Files.Views.Pages;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Storage;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.DataTransfer;

using static Files.Helpers.PathNormalization;
using Files.UserControls.MultiTaskingControl;
using Files.Views;

namespace Files.UserControls
{
    public sealed partial class VerticalTabView : UserControl, IMultitaskingControl
    {
        public ObservableCollection<TabItem> Items => MainPage.AppInstances;
        public void SelectionChanged() => TabStrip_SelectionChanged(null, null);

        public VerticalTabView()
        {
            this.InitializeComponent();
        }

        public async void SetSelectedTabInfo(string text, string currentPathForTabIcon = null)
        {
            var selectedTabItem = (MainPage.AppInstances[App.InteractionViewModel.TabStripSelectedIndex] as TabItem);
            selectedTabItem.AllowStorageItemDrop = App.CurrentInstance.InstanceViewModel.IsPageTypeNotHome;

            string tabLocationHeader;
            Microsoft.UI.Xaml.Controls.FontIconSource fontIconSource = new Microsoft.UI.Xaml.Controls.FontIconSource();
            Microsoft.UI.Xaml.Controls.IconSource tabIcon;

            if (currentPathForTabIcon == null && text == ResourceController.GetTranslation("SidebarSettings/Text"))
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarSettings/Text");
                fontIconSource.Glyph = "\xE713";
            }
            else if (currentPathForTabIcon == null && text == ResourceController.GetTranslation("NewTab"))
            {
                tabLocationHeader = ResourceController.GetTranslation("NewTab");
                fontIconSource.Glyph = "\xE737";
            }
            else if (currentPathForTabIcon.Equals(App.AppSettings.DesktopPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarDesktop");
                fontIconSource.Glyph = "\xE8FC";
            }
            else if (currentPathForTabIcon.Equals(App.AppSettings.DownloadsPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarDownloads");
                fontIconSource.Glyph = "\xE896";
            }
            else if (currentPathForTabIcon.Equals(App.AppSettings.DocumentsPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarDocuments");
                fontIconSource.Glyph = "\xE8A5";
            }
            else if (currentPathForTabIcon.Equals(App.AppSettings.PicturesPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarPictures");
                fontIconSource.Glyph = "\xEB9F";
            }
            else if (currentPathForTabIcon.Equals(App.AppSettings.MusicPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarMusic");
                fontIconSource.Glyph = "\xEC4F";
            }
            else if (currentPathForTabIcon.Equals(App.AppSettings.VideosPath, StringComparison.OrdinalIgnoreCase))
            {
                tabLocationHeader = ResourceController.GetTranslation("SidebarVideos");
                fontIconSource.Glyph = "\xE8B2";
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
                fontIconSource.Glyph = "\xE753";
            }
            else
            {
                // If path is a drive's root
                if (Helpers.PathNormalization.NormalizePath(Path.GetPathRoot(currentPathForTabIcon)) == NormalizePath(currentPathForTabIcon))
                {
                    if (NormalizePath(currentPathForTabIcon) != NormalizePath("A:") && NormalizePath(currentPathForTabIcon) != NormalizePath("B:"))
                    {
                        var remDriveNames = (await KnownFolders.RemovableDevices.GetFoldersAsync()).Select(x => x.DisplayName);
                        var matchingDriveName = remDriveNames.FirstOrDefault(x => NormalizePath(currentPathForTabIcon).Contains(x.ToUpperInvariant()));

                        if (matchingDriveName == null)
                        {
                            fontIconSource.Glyph = "\xEDA2";
                            tabLocationHeader = NormalizePath(currentPathForTabIcon);
                        }
                        else
                        {
                            fontIconSource.Glyph = "\xE88E";
                            tabLocationHeader = matchingDriveName;
                        }
                    }
                    else
                    {
                        fontIconSource.Glyph = "\xE74E";
                        tabLocationHeader = NormalizePath(currentPathForTabIcon);
                    }
                }
                else
                {
                    fontIconSource.Glyph = "\xE8B7";
                    tabLocationHeader = currentPathForTabIcon.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Split('\\', StringSplitOptions.RemoveEmptyEntries).Last();
                }
            }
            tabIcon = fontIconSource;
            selectedTabItem.Header = tabLocationHeader;
            selectedTabItem.IconSource = tabIcon;
        }

        private void NavigateToNumberedTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var InvokedTabView = (args.Element as TabView);

            int tabToSelect = 0;

            switch (sender.Key)
            {
                case VirtualKey.Number1:
                    tabToSelect = 0;
                    break;

                case VirtualKey.Number2:
                    tabToSelect = 1;
                    break;

                case VirtualKey.Number3:
                    tabToSelect = 2;
                    break;

                case VirtualKey.Number4:
                    tabToSelect = 3;
                    break;

                case VirtualKey.Number5:
                    tabToSelect = 4;
                    break;

                case VirtualKey.Number6:
                    tabToSelect = 5;
                    break;

                case VirtualKey.Number7:
                    tabToSelect = 6;
                    break;

                case VirtualKey.Number8:
                    tabToSelect = 7;
                    break;

                case VirtualKey.Number9:
                    // Select the last tab
                    tabToSelect = InvokedTabView.TabItems.Count - 1;
                    break;
            }

            // Only select the tab if it is in the list
            if (tabToSelect < InvokedTabView.TabItems.Count)
            {
                InvokedTabView.SelectedIndex = tabToSelect;
            }
            args.Handled = true;
        }

        private async void CloseSelectedTabKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var InvokedTabView = (args.Element as TabView);

            if (Items.Count == 1)
            {
                await ApplicationView.GetForCurrentView().TryConsolidateAsync();
            }
            else
            {
                Items.Remove(InvokedTabView.SelectedItem as TabItem);
            }
            args.Handled = true;
        }

        public void TabStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (App.InteractionViewModel.TabStripSelectedIndex >= 0 && App.InteractionViewModel.TabStripSelectedIndex < Items.Count)
            {
                Microsoft.UI.Xaml.Controls.FontIconSource icon = new Microsoft.UI.Xaml.Controls.FontIconSource();
                icon.Glyph = "\xE713";
                if ((Items[App.InteractionViewModel.TabStripSelectedIndex] as TabItem).Header.ToString() != ResourceController.GetTranslation("SidebarSettings/Text") 
                    && (Items[App.InteractionViewModel.TabStripSelectedIndex] as TabItem).IconSource != icon)
                {
                    App.CurrentInstance = GetCurrentSelectedTabInstance<ModernShellPage>();
                }

                if ((Items[App.InteractionViewModel.TabStripSelectedIndex] as TabItem).Header.ToString() == ResourceController.GetTranslation("SidebarSettings/Text"))
                {
                    App.InteractionViewModel.TabsLeftMargin = new Thickness(0, 0, 0, 0);
                    App.InteractionViewModel.LeftMarginLoaded = false;
                }
                else
                {
                    if (App.CurrentInstance != null)
                    {
                        if ((Items[App.InteractionViewModel.TabStripSelectedIndex] as TabItem).Header.ToString() == ResourceController.GetTranslation("NewTab"))
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

                    App.InteractionViewModel.TabsLeftMargin = new Thickness(200, 0, 0, 0);
                    App.InteractionViewModel.LeftMarginLoaded = true;
                }
            }
        }

        private async void TabStrip_TabCloseRequested(Microsoft.UI.Xaml.Controls.TabView sender, Microsoft.UI.Xaml.Controls.TabViewTabCloseRequestedEventArgs args)
        {
            if (Items.Count == 1)
            {
                await ApplicationView.GetForCurrentView().TryConsolidateAsync();
            }
            else if (Items.Count > 1)
            {
                int tabIndexToClose = Items.IndexOf(args.Item as TabItem);
                Items.RemoveAt(tabIndexToClose);
                //App.InteractionViewModel.TabStripSelectedIndex = verticalTabView.SelectedIndex;
            }
        }

        public static T GetCurrentSelectedTabInstance<T>()
        {
            var selectedTabContent = (MainPage.AppInstances[App.InteractionViewModel.TabStripSelectedIndex] as TabItem).Content as Grid;
            foreach (UIElement uiElement in selectedTabContent.Children)
            {
                if (uiElement.GetType() == typeof(Frame))
                {
                    return (T)((uiElement as Frame).Content);
                }
            }
            return default;
        }

        private void verticalTabView_AddTabButtonClick(TabView sender, object args)
        {
            MainPage.AddNewTab(typeof(ModernShellPage), ResourceController.GetTranslation("NewTab"));
        }

        private void verticalTabView_TabItemsChanged(TabView sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
        {
            switch (args.CollectionChange)
            {
                case Windows.Foundation.Collections.CollectionChange.ItemRemoved:
                    App.InteractionViewModel.TabStripSelectedIndex = Items.IndexOf(verticalTabView.SelectedItem as TabItem);
                    break;
                case Windows.Foundation.Collections.CollectionChange.ItemInserted:
                    App.InteractionViewModel.TabStripSelectedIndex = Items.IndexOf(verticalTabView.SelectedItem as TabItem);
                    break;
            }
        }

        private async void TabViewItem_Drop(object sender, DragEventArgs e)
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

                foreach (IStorageItem item in await e.DataView.GetStorageItemsAsync())
                {
                    if (item.IsOfType(StorageItemTypes.Folder))
                    {
                        await App.CurrentInstance.InteractionOperations.CloneDirectoryAsync(((StorageFolder)item), await StorageFolder.GetFolderFromPathAsync(tabViewItemWorkingDir), item.Name, true);
                    }
                    else
                    {
                        await ((StorageFile)item).CopyAsync(await StorageFolder.GetFolderFromPathAsync(tabViewItemWorkingDir), item.Name, NameCollisionOption.GenerateUniqueName);
                    }
                }
            }
            else
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
        }

        private void TabViewItem_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.AvailableFormats.Contains(StandardDataFormats.StorageItems))
                e.AcceptedOperation = DataPackageOperation.Copy;
            else
                e.AcceptedOperation = DataPackageOperation.None;
        }
    }
}
