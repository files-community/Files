using Files.Controls;
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
    public abstract class BaseLayout : Page
    {
        public bool IsQuickLookEnabled { get; set; } = false;

        public ItemViewModel AssociatedViewModel = null;
        public Interaction AssociatedInteractions = null;
        public bool isRenamingItem = false;
        public List<ListedItem> SelectedItems 
        { 
            get
            {
                if (App.CurrentInstance.CurrentPageType == typeof(GenericFileBrowser))
                {
                    return (App.CurrentInstance.ContentPage as GenericFileBrowser).AllView.SelectedItems.Cast<ListedItem>().ToList();
                }
                else if (App.CurrentInstance.CurrentPageType == typeof(PhotoAlbum))
                {
                    return (App.CurrentInstance.ContentPage as PhotoAlbum).FileList.SelectedItems.Cast<ListedItem>().ToList();
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
                if (App.CurrentInstance.CurrentPageType == typeof(GenericFileBrowser))
                {
                    return (App.CurrentInstance.ContentPage as GenericFileBrowser).AllView.SelectedItem as ListedItem;
                }
                else if (App.CurrentInstance.CurrentPageType == typeof(PhotoAlbum))
                {
                    return (App.CurrentInstance.ContentPage as PhotoAlbum).FileList.SelectedItem as ListedItem;
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
            if (App.AppSettings.FormFactor == Enums.FormFactorMode.Regular)
            {
                Frame rootFrame = Window.Current.Content as Frame;
                InstanceTabsView instanceTabsView = rootFrame.Content as InstanceTabsView;
                instanceTabsView.TabStrip_SelectionChanged(null, null);
            }
            App.CurrentInstance.NavigationControl.CanRefresh = true;
            (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.AlwaysPresentCommands.isEnabled = true;
            AssociatedViewModel.EmptyTextState.isVisible = Visibility.Collapsed;
            App.CurrentInstance.ViewModel.Universal.path = parameters;
            
            if (App.CurrentInstance.ViewModel.Universal.path == Path.GetPathRoot(App.CurrentInstance.ViewModel.Universal.path))
            {
                App.CurrentInstance.NavigationControl.CanNavigateToParent = false;
            }
            else
            {
                App.CurrentInstance.NavigationControl.CanNavigateToParent = true;
            }

            App.CurrentInstance.ViewModel.AddItemsToCollectionAsync(App.CurrentInstance.ViewModel.Universal.path);
            App.Clipboard_ContentChanged(null, null);

            if (parameters.Equals(App.AppSettings.DesktopPath))
            {
                App.CurrentInstance.NavigationControl.PathControlDisplayText = "Desktop";
            }
            else if (parameters.Equals(App.AppSettings.DocumentsPath))
            {
                App.CurrentInstance.NavigationControl.PathControlDisplayText = "Documents";
            }
            else if (parameters.Equals(App.AppSettings.DownloadsPath))
            {
                App.CurrentInstance.NavigationControl.PathControlDisplayText = "Downloads";
            }
            else if (parameters.Equals(App.AppSettings.PicturesPath))
            {
                App.CurrentInstance.NavigationControl.PathControlDisplayText = "Pictures";
            }
            else if (parameters.Equals(App.AppSettings.MusicPath))
            {
                App.CurrentInstance.NavigationControl.PathControlDisplayText = "Music";
            }
            else if (parameters.Equals(App.AppSettings.OneDrivePath))
            {
                App.CurrentInstance.NavigationControl.PathControlDisplayText = "OneDrive";
            }
            else if (parameters.Equals(App.AppSettings.VideosPath))
            {
                App.CurrentInstance.NavigationControl.PathControlDisplayText = "Videos";
            }
            else
            {
                if (parameters.Equals(@"C:\") || parameters.Equals(@"c:\"))
                {
                    App.CurrentInstance.NavigationControl.PathControlDisplayText = @"Local Disk (C:\)";
                }
                else
                {
                    App.CurrentInstance.NavigationControl.PathControlDisplayText = parameters;
                }
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            // Remove item jumping handler
            Window.Current.CoreWindow.CharacterReceived -= Page_CharacterReceived;
            if (App.CurrentInstance.ViewModel._fileQueryResult != null)
            {
                App.CurrentInstance.ViewModel._fileQueryResult.ContentsChanged -= App.CurrentInstance.ViewModel.FileContentsChanged;
            }
        }

        private void UnloadMenuFlyoutItemByName(string nameToUnload)
        {
            Windows.UI.Xaml.Markup.XamlMarkupHelper.UnloadObject(this.FindName(nameToUnload) as DependencyObject);
        }

        public void RightClickContextMenu_Opening(object sender, object e)
        {
            var selectedFileSystemItems = (App.CurrentInstance.ContentPage as BaseLayout).SelectedItems;

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
                AssociatedViewModel = App.CurrentInstance.ViewModel;
                AssociatedInteractions = App.CurrentInstance.InteractionOperations;
                if (App.CurrentInstance == null)
                {
                    App.CurrentInstance = ItemViewModel.GetCurrentSelectedTabInstance<ProHome>();
                }
            }
        }

        protected virtual void Page_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            var focusedElement = FocusManager.GetFocusedElement(XamlRoot) as FrameworkElement;
            if (focusedElement is TextBox)
                return;

            char letterPressed = Convert.ToChar(args.KeyCode);
            App.CurrentInstance.InteractionOperations.PushJumpChar(letterPressed);
        }
    }
}
