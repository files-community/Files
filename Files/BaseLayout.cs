using Files.Filesystem;
using Files.Interacts;
using Files.Views.Pages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
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

        private List<ListedItem> _SelectedItems = new List<ListedItem>();

        public List<ListedItem> SelectedItems
        {
            get
            {
                return _SelectedItems;
            }
            internal set
            {
                if (value != _SelectedItems)
                {
                    _SelectedItems = value;
                    if (_SelectedItems.Count == 0)
                    {
                        IsItemSelected = false;
                        SelectedItem = null;
                    }
                    else
                    {
                        IsItemSelected = true;
                        SelectedItem = _SelectedItems.First();
                    }
                    NotifyPropertyChanged("SelectedItems");
                    SetDragModeForItems();
                }
            }
        }

        public ListedItem SelectedItem { get; private set; }

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

        public abstract void SetSelectedItemOnUi(ListedItem item);

        public abstract void SetSelectedItemsOnUi(List<ListedItem> items);

        public abstract void SelectAllItems();

        public abstract void InvertSelection();

        public abstract void ClearSelection();

        public abstract void SetDragModeForItems();

        public abstract void ScrollIntoView(ListedItem item);

        public abstract int GetSelectedIndex();

        public abstract void FocusSelectedItems();

        public abstract void StartRenameItem();

        public abstract void ResetItemOpacity();

        public abstract void SetItemOpacity(ListedItem item);

        protected abstract ListedItem GetItemFromElement(object element);

        private void AppSettings_LayoutModeChangeRequested(object sender, EventArgs e)
        {
            if (App.CurrentInstance.ContentPage != null)
            {
                App.CurrentInstance.ViewModel.CancelLoadAndClearFiles();
                App.CurrentInstance.ViewModel.IsLoadingItems = true;
                App.CurrentInstance.ViewModel.IsLoadingItems = false;

                App.CurrentInstance.ContentFrame.Navigate(App.AppSettings.GetLayoutType(), App.CurrentInstance.ViewModel.WorkingDirectory, null);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override async void OnNavigatedTo(NavigationEventArgs eventArgs)
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

            // pathRoot will be empty on recycle bin path
            string pathRoot = Path.GetPathRoot(App.CurrentInstance.ViewModel.WorkingDirectory);
            if (string.IsNullOrEmpty(pathRoot) || App.CurrentInstance.ViewModel.WorkingDirectory == pathRoot)
            {
                App.CurrentInstance.NavigationToolbar.CanNavigateToParent = false;
            }
            else
            {
                App.CurrentInstance.NavigationToolbar.CanNavigateToParent = true;
            }
            App.InteractionViewModel.IsPageTypeNotHome = true; // show controls that were hidden on the home page
            App.InteractionViewModel.IsPageTypeNotRecycleBin = 
                !App.CurrentInstance.ViewModel.WorkingDirectory.StartsWith(App.AppSettings.RecycleBinPath);

            await App.CurrentInstance.ViewModel.RefreshItems();

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
            var menuItem = this.FindName(nameToUnload) as DependencyObject;
            if (menuItem != null) // Prevent crash if the MenuFlyoutItem is missing
                (menuItem as MenuFlyoutItemBase).Visibility = Visibility.Collapsed;
        }

        public void RightClickContextMenu_Opening(object sender, object e)
        {
            if (App.CurrentInstance.ViewModel.WorkingDirectory.StartsWith(App.AppSettings.RecycleBinPath))
            {
                (this.FindName("EmptyRecycleBin") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                (this.FindName("OpenTerminal") as MenuFlyoutItemBase).IsEnabled = false;
                UnloadMenuFlyoutItemByName("NewEmptySpace");
            }
            else
            {
                UnloadMenuFlyoutItemByName("EmptyRecycleBin");
                (this.FindName("OpenTerminal") as MenuFlyoutItemBase).IsEnabled = true;
                (this.FindName("NewEmptySpace") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
            }
        }

        public void RightClickItemContextMenu_Opening(object sender, object e)        
        {
            var selectedFileSystemItems = App.CurrentInstance.ContentPage.SelectedItems;

            // Find selected items that are not folders
            if (selectedFileSystemItems.Cast<ListedItem>().Any(x => x.PrimaryItemAttribute != StorageItemTypes.Folder))
            {
                UnloadMenuFlyoutItemByName("SidebarPinItem");
                UnloadMenuFlyoutItemByName("OpenInNewTab");
                UnloadMenuFlyoutItemByName("OpenInNewWindowItem");

                if (selectedFileSystemItems.Count == 1)
                {
                    var selectedDataItem = selectedFileSystemItems[0] as ListedItem;

                    if (!string.IsNullOrEmpty(selectedDataItem.FileExtension))
                    {
                        if (selectedDataItem.FileExtension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            UnloadMenuFlyoutItemByName("OpenItem");
                            (this.FindName("UnzipItem") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                            //this.FindName("UnzipItem");
                        }
                        else if (!selectedDataItem.FileExtension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            (this.FindName("OpenItem") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                            //this.FindName("OpenItem");
                            UnloadMenuFlyoutItemByName("UnzipItem");
                        }
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
                    (this.FindName("SidebarPinItem") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                    (this.FindName("OpenInNewTab") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                    (this.FindName("OpenInNewWindowItem") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                    //this.FindName("SidebarPinItem");
                    //this.FindName("OpenInNewTab");
                    //this.FindName("OpenInNewWindowItem");
                    UnloadMenuFlyoutItemByName("UnzipItem");
                }
                else if (selectedFileSystemItems.Count > 5)
                {
                    (this.FindName("SidebarPinItem") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                    //this.FindName("SidebarPinItem");
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
            char letterPressed = Convert.ToChar(args.KeyCode);
            App.CurrentInstance.InteractionOperations.PushJumpChar(letterPressed);
        }

        protected async void List_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> draggedItems = await e.DataView.GetStorageItemsAsync();
                // As long as one file doesn't already belong to this folder
                if (draggedItems.Any(draggedItem => !Directory.GetParent(draggedItem.Path).FullName.Equals(App.CurrentInstance.ViewModel.WorkingDirectory, StringComparison.OrdinalIgnoreCase)))
                {
                    e.AcceptedOperation = DataPackageOperation.Copy;
                    e.Handled = true;
                }
                else
                {
                    e.AcceptedOperation = DataPackageOperation.None;
                }
            }
        }

        protected async void List_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                await AssociatedInteractions.PasteItems(e.DataView, App.CurrentInstance.ViewModel.WorkingDirectory, e.AcceptedOperation);
                e.Handled = true;
            }
        }

        protected async void Item_DragStarting(object sender, DragStartingEventArgs e)
        {
            List<IStorageItem> selectedStorageItems = new List<IStorageItem>();
            foreach (ListedItem item in App.CurrentInstance.ContentPage.SelectedItems)
            {
                if (item.PrimaryItemAttribute == StorageItemTypes.File)
                    selectedStorageItems.Add(await StorageFile.GetFileFromPathAsync(item.ItemPath));
                else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                    selectedStorageItems.Add(await StorageFolder.GetFolderFromPathAsync(item.ItemPath));
            }

            if (selectedStorageItems.Count == 0)
            {
                e.Cancel = true;
                return;
            }

            e.Data.SetStorageItems(selectedStorageItems);
            e.DragUI.SetContentFromDataPackage();
        }

        protected async void Item_DragOver(object sender, DragEventArgs e)
        {
            ListedItem item = GetItemFromElement(sender);
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.Handled = true;
                IReadOnlyList<IStorageItem> draggedItems = await e.DataView.GetStorageItemsAsync();
                // Items from the same parent folder as this folder are dragged into this folder, so we move the items instead of copy
                if (draggedItems.Any(draggedItem => Directory.GetParent(draggedItem.Path).FullName == Directory.GetParent(item.ItemPath).FullName))
                {
                    e.AcceptedOperation = DataPackageOperation.Move;
                }
                else
                {
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
            }
        }

        protected async void Item_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            ListedItem rowItem = GetItemFromElement(sender);
            await App.CurrentInstance.InteractionOperations.PasteItems(e.DataView, rowItem.ItemPath, e.AcceptedOperation);
        }

        protected void InitializeDrag(UIElement element)
        {
            ListedItem item = GetItemFromElement(element);
            if (item != null)
            {
                element.AllowDrop = false;
                element.DragStarting -= Item_DragStarting;
                element.DragStarting += Item_DragStarting;
                element.DragOver -= Item_DragOver;
                element.Drop -= Item_Drop;
                if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    element.AllowDrop = true;
                    element.DragOver += Item_DragOver;
                    element.Drop += Item_Drop;
                }
            }
        }

        // VirtualKey doesn't support / accept plus and minus by default.
        public readonly VirtualKey plusKey = (VirtualKey)187;
        public readonly VirtualKey minusKey = (VirtualKey)189;

        public void GridViewSizeIncrease(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            App.AppSettings.GridViewSize = App.AppSettings.GridViewSize + 25; // Make Larger
            if (args != null)
                args.Handled = true;
        }

        public void GridViewSizeDecrease(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            App.AppSettings.GridViewSize = App.AppSettings.GridViewSize - 25; // Make Smaller
            if (args != null)
                args.Handled = true;
        }

        public void BaseLayout_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (e.KeyModifiers == VirtualKeyModifiers.Control)
            {
                if (e.GetCurrentPoint(null).Properties.MouseWheelDelta < 0) // Mouse wheel down
                {
                    GridViewSizeDecrease(null, null);
                }
                else // Mouse wheel up
                {
                    GridViewSizeIncrease(null, null);
                }

                e.Handled = true;
            }
        }

    }
}