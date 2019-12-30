using Files.Filesystem;
using Files.Interacts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


namespace Files
{
    /// <summary>
    /// The base class which every layout page must derive from
    /// </summary>
    public class BaseLayout : Page
    {
        public bool IsQuickLookEnabled { get; set; } = false;

        public ItemViewModel AssociatedViewModel = null;
        public Interaction AssociatedInteractions = null;
        public bool isRenamingItem = false;
        public List<ListedItem> SelectedItems 
        { 
            get
            {
                if (App.OccupiedInstance.ItemDisplayFrame.CurrentSourcePageType == typeof(GenericFileBrowser))
                {
                    return (App.OccupiedInstance.ItemDisplayFrame.Content as GenericFileBrowser).AllView.SelectedItems.Cast<ListedItem>().ToList();
                }
                else if (App.OccupiedInstance.ItemDisplayFrame.CurrentSourcePageType == typeof(PhotoAlbum))
                {
                    return (App.OccupiedInstance.ItemDisplayFrame.Content as PhotoAlbum).FileList.SelectedItems.Cast<ListedItem>().ToList();
                }
                else
                {
                    return new List<ListedItem>();
                }
            }
        }
        public ListedItem SelectedItem
        {
            get
            {
                if (App.OccupiedInstance.ItemDisplayFrame.CurrentSourcePageType == typeof(GenericFileBrowser))
                {
                    return (App.OccupiedInstance.ItemDisplayFrame.Content as GenericFileBrowser).AllView.SelectedItem as ListedItem;
                }
                else if (App.OccupiedInstance.ItemDisplayFrame.CurrentSourcePageType == typeof(PhotoAlbum))
                {
                    return (App.OccupiedInstance.ItemDisplayFrame.Content as PhotoAlbum).FileList.SelectedItem as ListedItem;
                }
                else
                {
                    return null;
                }
            }
        }

        public BaseLayout()
        {
            this.Loaded += Page_Loaded;
            Page_Loaded(null, null);

            // QuickLook Integration
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            var isQuickLookIntegrationEnabled = localSettings.Values["quicklook_enabled"];

            if (isQuickLookIntegrationEnabled != null && isQuickLookIntegrationEnabled.Equals(true))
            {
                IsQuickLookEnabled = true;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            // Add item jumping handler
            Window.Current.CoreWindow.CharacterReceived += Page_CharacterReceived;
            var parameters = (string)eventArgs.Parameter;
            if (App.FormFactor == Enums.FormFactorMode.Regular)
            {
                Frame rootFrame = Window.Current.Content as Frame;
                InstanceTabsView instanceTabsView = rootFrame.Content as InstanceTabsView;
                instanceTabsView.TabStrip_SelectionChanged(null, null);
            }
            App.OccupiedInstance.RibbonArea.Refresh.IsEnabled = true;
            App.OccupiedInstance.AlwaysPresentCommands.isEnabled = true;
            AssociatedViewModel.EmptyTextState.isVisible = Visibility.Collapsed;
            App.OccupiedInstance.instanceViewModel.Universal.path = parameters;
            
            if (App.OccupiedInstance.instanceViewModel.Universal.path == Path.GetPathRoot(App.OccupiedInstance.instanceViewModel.Universal.path))
            {
                App.OccupiedInstance.RibbonArea.Up.IsEnabled = false;
            }
            else
            {
                App.OccupiedInstance.RibbonArea.Up.IsEnabled = true;
            }

            App.OccupiedInstance.instanceViewModel.AddItemsToCollectionAsync(App.OccupiedInstance.instanceViewModel.Universal.path);
            App.Clipboard_ContentChanged(null, null);

            if (parameters.Equals(App.DesktopPath))
            {
                App.OccupiedInstance.PathText.Text = "Desktop";
            }
            else if (parameters.Equals(App.DocumentsPath))
            {
                App.OccupiedInstance.PathText.Text = "Documents";
            }
            else if (parameters.Equals(App.DownloadsPath))
            {
                App.OccupiedInstance.PathText.Text = "Downloads";
            }
            else if (parameters.Equals(App.PicturesPath))
            {
                App.OccupiedInstance.PathText.Text = "Pictures";
            }
            else if (parameters.Equals(App.MusicPath))
            {
                App.OccupiedInstance.PathText.Text = "Music";
            }
            else if (parameters.Equals(App.OneDrivePath))
            {
                App.OccupiedInstance.PathText.Text = "OneDrive";
            }
            else if (parameters.Equals(App.VideosPath))
            {
                App.OccupiedInstance.PathText.Text = "Videos";
            }
            else
            {
                if (parameters.Equals(@"C:\") || parameters.Equals(@"c:\"))
                {
                    App.OccupiedInstance.PathText.Text = @"Local Disk (C:\)";
                }
                else
                {
                    App.OccupiedInstance.PathText.Text = parameters;
                }
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            // Remove item jumping handler
            Window.Current.CoreWindow.CharacterReceived -= Page_CharacterReceived;
            if (App.OccupiedInstance.instanceViewModel._fileQueryResult != null)
            {
                App.OccupiedInstance.instanceViewModel._fileQueryResult.ContentsChanged -= App.OccupiedInstance.instanceViewModel.FileContentsChanged;
            }
        }

        private void UnloadMenuFlyoutItemByName(string nameToUnload)
        {
            Windows.UI.Xaml.Markup.XamlMarkupHelper.UnloadObject(this.FindName(nameToUnload) as DependencyObject);
        }

        public void RightClickContextMenu_Opening(object sender, object e)
        {
            var selectedFileSystemItems = (App.OccupiedInstance.ItemDisplayFrame.Content as BaseLayout).SelectedItems;

            // Find selected items that are not folders
            if (selectedFileSystemItems.Cast<ListedItem>().Any(x => x.FileType != "Folder"))
            {
                UnloadMenuFlyoutItemByName("SidebarPinItem");
                UnloadMenuFlyoutItemByName("OpenInNewTab");
                UnloadMenuFlyoutItemByName("OpenInNewWindowItem");

                if (selectedFileSystemItems.Count == 1)
                {
                    var selectedDataItem = selectedFileSystemItems[0] as ListedItem;

                    if (selectedDataItem.DotFileExtension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        UnloadMenuFlyoutItemByName("OpenItem");
                        UnloadMenuFlyoutItemByName("UnzipItem");
                    }
                    else if (!selectedDataItem.DotFileExtension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        this.FindName("OpenItem");
                        UnloadMenuFlyoutItemByName("UnzipItem");
                    }
                }
                else if (selectedFileSystemItems.Count > 1)
                {
                    UnloadMenuFlyoutItemByName("OpenItem");
                    UnloadMenuFlyoutItemByName("UnzipItem");
                }
            }
            else     // All are Folders
            {
                UnloadMenuFlyoutItemByName("OpenItem");
                if (selectedFileSystemItems.Count <= 5 && selectedFileSystemItems.Count > 0)
                {
                    this.FindName("SidebarPinItem");
                    this.FindName("OpenInNewTab");
                    this.FindName("OpenInNewWindowItem");
                    UnloadMenuFlyoutItemByName("UnzipItem");
                }
                else if (selectedFileSystemItems.Count > 5)
                {
                    this.FindName("SidebarPinItem");
                    UnloadMenuFlyoutItemByName("OpenInNewTab");
                    UnloadMenuFlyoutItemByName("OpenInNewWindowItem");
                    UnloadMenuFlyoutItemByName("UnzipItem");
                }

            }
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (AssociatedViewModel == null && AssociatedInteractions == null)
            {
                AssociatedViewModel = App.OccupiedInstance.instanceViewModel;
                AssociatedInteractions = App.OccupiedInstance.instanceInteraction;
                if (App.OccupiedInstance == null)
                {
                    App.OccupiedInstance = ItemViewModel.GetCurrentSelectedTabInstance<ProHome>();
                }

                if (App.OccupiedInstance.instanceViewModel == null && App.OccupiedInstance.instanceInteraction == null)
                {
                    App.OccupiedInstance.instanceViewModel = new ItemViewModel();
                    App.OccupiedInstance.instanceInteraction = new Interaction();
                    Page_Loaded(null, null);
                }
            }
        }

        protected virtual void Page_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            var focusedElement = FocusManager.GetFocusedElement(XamlRoot) as FrameworkElement;
            if (focusedElement is TextBox)
                return;

            char letterPressed = Convert.ToChar(args.KeyCode);
            App.OccupiedInstance.instanceInteraction.PushJumpChar(letterPressed);
        }
    }
}
