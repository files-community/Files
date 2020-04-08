using Files.Filesystem;
using Files.Interacts;
using Files.View_Models;
using Files.Views.Pages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;


namespace Files
{
    /// <summary>
    /// The base class which every layout page must derive from
    /// </summary>
    public abstract class BaseLayout : Page, INotifyPropertyChanged
    {
        public bool IsQuickLookEnabled { get; set; } = false;

        public ItemViewModel AssociatedViewModel = null;
        public Interaction AssociatedInteractions = null;
        public bool isRenamingItem = false;

        private bool isItemSelected = false;
        public bool IsItemSelected
        {
            get
            {
                return isItemSelected;
            }
            internal set
            {
                if (value != isItemSelected)
                {
                    isItemSelected = value;
                    NotifyPropertyChanged("IsItemSelected");
                }
            }
        }

        private List<ListedItem> _SelectedItems;
        public List<ListedItem> SelectedItems
        {
            get
            {
                if(_SelectedItems == null)
                {
                    return new List<ListedItem>();
                }
                else
                {
                    return _SelectedItems;
                }
            }
            internal set
            {
                if (value != _SelectedItems)
                {
                    _SelectedItems = value;
                    if (value == null)
                    {
                        IsItemSelected = false;
                    }
                    else
                    {
                        IsItemSelected = true;
                    }
                    NotifyPropertyChanged("SelectedItems");
                }
            }
        }

        private ListedItem _SelectedItem;
        public ListedItem SelectedItem
        {
            get
            {
                return _SelectedItem;
            }
            internal set
            {
                if (value != _SelectedItem)
                {
                    _SelectedItem = value;
                    if (value == null)
                    {
                        IsItemSelected = false;
                    }
                    else
                    {
                        IsItemSelected = true;
                    }
                    NotifyPropertyChanged("SelectedItem");
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

        private void AppSettings_LayoutModeChangeRequested(object sender, EventArgs e)
        {
            if (App.CurrentInstance.ContentPage != null)
            {
                App.CurrentInstance.ViewModel.CancelLoadAndClearFiles();
                App.CurrentInstance.ViewModel.IsLoadingItems = true;
                App.CurrentInstance.ViewModel.IsLoadingItems = false;
                if (App.AppSettings.LayoutMode == 0)
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), App.CurrentInstance.ViewModel.WorkingDirectory, null);
                }
                else
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(PhotoAlbum), App.CurrentInstance.ViewModel.WorkingDirectory, null);
                }
            }

        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            // Add item jumping handler
            App.AppSettings.LayoutModeChangeRequested += AppSettings_LayoutModeChangeRequested;
            Window.Current.CoreWindow.CharacterReceived += Page_CharacterReceived;
            var parameters = (string)eventArgs.Parameter;
            if (App.AppSettings.FormFactor == Enums.FormFactorMode.Regular)
            {
                Frame rootFrame = Window.Current.Content as Frame;
                InstanceTabsView instanceTabsView = rootFrame.Content as InstanceTabsView;
                instanceTabsView.TabStrip_SelectionChanged(null, null);
            }
            App.CurrentInstance.NavigationToolbar.CanRefresh = true;
            IsItemSelected = false;
            AssociatedViewModel.EmptyTextState.IsVisible = Visibility.Collapsed;
            App.CurrentInstance.ViewModel.WorkingDirectory = parameters;

            if (App.CurrentInstance.ViewModel.WorkingDirectory == Path.GetPathRoot(App.CurrentInstance.ViewModel.WorkingDirectory))
            {
                App.CurrentInstance.NavigationToolbar.CanNavigateToParent = false;
            }
            else
            {
                App.CurrentInstance.NavigationToolbar.CanNavigateToParent = true;
            }

            App.CurrentInstance.ViewModel.AddItemsToCollectionAsync(App.CurrentInstance.ViewModel.WorkingDirectory);
            App.Clipboard_ContentChanged(null, null);

            App.CurrentInstance.NavigationToolbar.PathControlDisplayText = parameters;
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
            App.AppSettings.LayoutModeChangeRequested -= AppSettings_LayoutModeChangeRequested;
        }

        private void UnloadMenuFlyoutItemByName(string nameToUnload)
        {
            Windows.UI.Xaml.Markup.XamlMarkupHelper.UnloadObject(this.FindName(nameToUnload) as DependencyObject);
        }

        public void RightClickContextMenu_Opening(object sender, object e)
        {
            var selectedFileSystemItems = (App.CurrentInstance.ContentPage as BaseLayout).SelectedItems;

            // Find selected items that are not folders
            if (selectedFileSystemItems.Cast<ListedItem>().Any(x => x.PrimaryItemAttribute != StorageItemTypes.Folder))
            {
                UnloadMenuFlyoutItemByName("SidebarPinItem");
                UnloadMenuFlyoutItemByName("OpenInNewTab");
                UnloadMenuFlyoutItemByName("OpenInNewWindowItem");

                if (selectedFileSystemItems.Count == 1)
                {
                    var selectedDataItem = selectedFileSystemItems[0] as ListedItem;

                    if (selectedDataItem.FileExtension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        UnloadMenuFlyoutItemByName("OpenItem");
                        this.FindName("UnzipItem");
                    }
                    else if (!selectedDataItem.FileExtension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
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

            //check if the selected file is an image
            App.InteractionViewModel.CheckForImage();
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (AssociatedViewModel == null && AssociatedInteractions == null)
            {
                AssociatedViewModel = App.CurrentInstance.ViewModel;
                AssociatedInteractions = App.CurrentInstance.InteractionOperations;
                if (App.CurrentInstance == null)
                {
                    App.CurrentInstance = ItemViewModel.GetCurrentSelectedTabInstance<ModernShellPage>();
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
