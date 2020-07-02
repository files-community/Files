using Files.Views.Pages;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Files.Common;
using Windows.System;
using Windows.UI.ViewManagement;
using System.Collections.ObjectModel;

using static Files.Helpers.PathNormalization;


// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class VerticalTabView : UserControl
    {
        public static ObservableCollection<TabItem> Items = new ObservableCollection<TabItem>(); 

        public VerticalTabView()
        {
            this.InitializeComponent();
        }

        public static async void AddNewTab(Type t, string path)
        {
            Frame frame = new Frame();
            string tabLocationHeader = null;
            Microsoft.UI.Xaml.Controls.FontIconSource fontIconSource = new Microsoft.UI.Xaml.Controls.FontIconSource();
            Microsoft.UI.Xaml.Controls.IconSource tabIcon;

            if (path != null)
            {
                if (path == "Settings")
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarSettings/Text");
                    fontIconSource.Glyph = "\xE713";
                    //foreach (TabItem item in Items)
                    //{
                    //    if (item.Header.ToString() == ResourceController.GetTranslation("SidebarSettings/Text"))
                    //    {
                    //        App.InteractionViewModel.TabStripSelectedIndex = Items.IndexOf(item);
                    //        return;
                    //    }
                    //}
                }
                else if (path.Equals(App.AppSettings.DesktopPath, StringComparison.OrdinalIgnoreCase))
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarDesktop");
                    fontIconSource.Glyph = "\xE8FC";
                }
                else if (path.Equals(App.AppSettings.DownloadsPath, StringComparison.OrdinalIgnoreCase))
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarDownloads");
                    fontIconSource.Glyph = "\xE896";
                }
                else if (path.Equals(App.AppSettings.DocumentsPath, StringComparison.OrdinalIgnoreCase))
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarDocuments");
                    fontIconSource.Glyph = "\xE8A5";
                }
                else if (path.Equals(App.AppSettings.PicturesPath, StringComparison.OrdinalIgnoreCase))
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarPictures");
                    fontIconSource.Glyph = "\xEB9F";
                }
                else if (path.Equals(App.AppSettings.MusicPath, StringComparison.OrdinalIgnoreCase))
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarMusic");
                    fontIconSource.Glyph = "\xEC4F";
                }
                else if (path.Equals(App.AppSettings.VideosPath, StringComparison.OrdinalIgnoreCase))
                {
                    tabLocationHeader = ResourceController.GetTranslation("SidebarVideos");
                    fontIconSource.Glyph = "\xE8B2";
                }
                else if (path.Equals(App.AppSettings.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
                {
                    var localSettings = ApplicationData.Current.LocalSettings;
                    tabLocationHeader = localSettings.Values.Get("RecycleBin_Title", "Recycle Bin");
                    fontIconSource.Glyph = "\xE74D";
                }
                else if (App.AppSettings.OneDrivePath != null && path.Equals(App.AppSettings.OneDrivePath, StringComparison.OrdinalIgnoreCase))
                {
                    tabLocationHeader = "OneDrive";
                    fontIconSource.Glyph = "\xE753";
                }
                else if (path == ResourceController.GetTranslation("NewTab"))
                {
                    tabLocationHeader = ResourceController.GetTranslation("NewTab");
                    fontIconSource.Glyph = "\xE737";
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
                            tabLocationHeader = dirName;
                            fontIconSource.Glyph = "\xE8B7";
                        }
                        else
                        {
                            // Pick the best icon for this tab
                            var remDriveNames = (await KnownFolders.RemovableDevices.GetFoldersAsync()).Select(x => x.DisplayName);

                            if (!remDriveNames.Contains(normalizedPath))
                            {
                                if (path != "A:" && path != "B:") // Check if it's using (generally) floppy-reserved letters.
                                    fontIconSource.Glyph = "\xE74E"; // Floppy Disk icon
                                else
                                    fontIconSource.Glyph = "\xEDA2"; // Hard Disk icon

                                tabLocationHeader = normalizedPath;
                            }
                            else
                            {
                                fontIconSource.Glyph = "\xE88E";
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
                        fontIconSource.Glyph = "\xE737";
                    }
                }
            }

            tabIcon = fontIconSource;
            Grid gr = new Grid();
            gr.Children.Add(frame);
            gr.HorizontalAlignment = HorizontalAlignment.Stretch;
            gr.VerticalAlignment = VerticalAlignment.Stretch;
            TabItem tvi = new TabItem()
            {
                Header = tabLocationHeader,
                Content = gr,
                IconSource = tabIcon,
                Description = null
            };
            Items.Add(tvi);
            //App.InteractionViewModel.TabStripSelectedIndex = Items.Count - 1;

            var tabViewItemFrame = (tvi.Content as Grid).Children[0] as Frame;
            tabViewItemFrame.Loaded += delegate
            {
                if (tabViewItemFrame.CurrentSourcePageType != typeof(ModernShellPage))
                {
                    tabViewItemFrame.Navigate(t, path);
                }
            };
        }

        public static async void SetSelectedTabInfo(string text, string currentPathForTabIcon = null)
        {
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
            (Items[App.InteractionViewModel.TabStripSelectedIndex] as TabItem).Header = tabLocationHeader;
            (Items[App.InteractionViewModel.TabStripSelectedIndex] as TabItem).IconSource = tabIcon;
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
            //{
            //    if (e.RemovedItems.Count > 0 && e.AddedItems.Count == 0)
            //    {
            //        var itemToReselect = e.RemovedItems[0] as TabItem;
            //        if (Items.Contains(itemToReselect))
            //        {
            //            App.InteractionViewModel.TabStripSelectedIndex = Items.IndexOf(itemToReselect);
            //        }
            //    }
            //}
            //else
            {
                Microsoft.UI.Xaml.Controls.FontIconSource icon = new Microsoft.UI.Xaml.Controls.FontIconSource();
                icon.Glyph = "\xE713";
                if ((Items[App.InteractionViewModel.TabStripSelectedIndex] as TabItem).Header.ToString() != ResourceController.GetTranslation("SidebarSettings/Text") && (Items[App.InteractionViewModel.TabStripSelectedIndex] as TabItem).IconSource != icon)
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
            var selectedTabContent = (Items[App.InteractionViewModel.TabStripSelectedIndex] as TabItem).Content as Grid;
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
            AddNewTab(typeof(ModernShellPage), ResourceController.GetTranslation("NewTab"));
        }
    }
}
