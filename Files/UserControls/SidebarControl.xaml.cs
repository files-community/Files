using Files.DataModels;
using Files.DataModels.NavigationControlItems;
using Files.Extensions;
using Files.Filesystem;
using Files.Filesystem.StorageItems;
using Files.Helpers;
using Files.Helpers.ContextFlyouts;
using Files.ViewModels;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Files.UserControls
{
    public sealed partial class SidebarControl : Microsoft.UI.Xaml.Controls.NavigationView, INotifyPropertyChanged
    {
        public static SemaphoreSlim SideBarItemsSemaphore = new SemaphoreSlim(1, 1);

        public static BulkConcurrentObservableCollection<INavigationControlItem> SideBarItems { get; private set; } = new BulkConcurrentObservableCollection<INavigationControlItem>();

        public SettingsViewModel AppSettings => App.AppSettings;

        public delegate void SidebarItemInvokedEventHandler(object sender, SidebarItemInvokedEventArgs e);

        public event SidebarItemInvokedEventHandler SidebarItemInvoked;

        public delegate void SidebarItemNewPaneInvokedEventHandler(object sender, SidebarItemNewPaneInvokedEventArgs e);

        public event SidebarItemNewPaneInvokedEventHandler SidebarItemNewPaneInvoked;

        public delegate void SidebarItemPropertiesInvokedEventHandler(object sender, SidebarItemPropertiesInvokedEventArgs e);

        public event SidebarItemPropertiesInvokedEventHandler SidebarItemPropertiesInvoked;

        public delegate void SidebarItemDroppedEventHandler(object sender, SidebarItemDroppedEventArgs e);

        public event SidebarItemDroppedEventHandler SidebarItemDropped;

        /// <summary>
        /// The Model for the pinned sidebar items
        /// </summary>
        public SidebarPinnedModel SidebarPinnedModel => App.SidebarPinnedController.Model;

        public static readonly DependencyProperty EmptyRecycleBinCommandProperty = DependencyProperty.Register(nameof(EmptyRecycleBinCommand), typeof(ICommand), typeof(SidebarControl), new PropertyMetadata(null));

        public ICommand EmptyRecycleBinCommand
        {
            get => (ICommand)GetValue(EmptyRecycleBinCommandProperty);
            set => SetValue(EmptyRecycleBinCommandProperty, value);
        }

        public readonly RelayCommand CreateLibraryCommand = new RelayCommand(LibraryHelper.ShowCreateNewLibraryDialog);

        public readonly RelayCommand RestoreLibrariesCommand = new RelayCommand(LibraryHelper.ShowRestoreDefaultLibrariesDialog);

        private bool IsInPointerPressed = false;

        private DispatcherQueueTimer dragOverSectionTimer, dragOverItemTimer;

        public SidebarControl()
        {
            this.InitializeComponent();
            this.Loaded += SidebarNavView_Loaded;

            dragOverSectionTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            dragOverItemTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        }

        public static readonly DependencyProperty SelectedSidebarItemProperty = DependencyProperty.Register(nameof(SelectedSidebarItem), typeof(INavigationControlItem), typeof(SidebarControl), new PropertyMetadata(null));

        public INavigationControlItem SelectedSidebarItem
        {
            get => (INavigationControlItem)GetValue(SelectedSidebarItemProperty);
            set
            {
                if (this.IsLoaded)
                {
                    SetValue(SelectedSidebarItemProperty, value);
                }
            }
        }

        public static readonly DependencyProperty TabContentProperty = DependencyProperty.Register(nameof(TabContent), typeof(UIElement), typeof(SidebarControl), new PropertyMetadata(null));

        public UIElement TabContent
        {
            get => (UIElement)GetValue(TabContentProperty);
            set => SetValue(TabContentProperty, value);
        }

        private bool canOpenInNewPane;

        public bool CanOpenInNewPane
        {
            get => canOpenInNewPane;
            set
            {
                if (value != canOpenInNewPane)
                {
                    canOpenInNewPane = value;
                    NotifyPropertyChanged(nameof(CanOpenInNewPane));
                }
            }
        }

        public bool ShowMoveItemUp { get; set; }

        public bool ShowMoveItemDown { get; set; }

        public bool ShowUnpinItem { get; set; }

        public bool ShowHideSection { get; set; }

        public bool ShowProperties { get; set; }

        public bool ShowEmptyRecycleBin { get; set; }

        public bool ShowEjectDevice { get; set; }

        public bool IsLocationItem { get; set; }

        public bool IsLibrariesHeader { get; set; }

        public INavigationControlItem RightClickedItem;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void HideSection_Click(object sender, RoutedEventArgs e)
        {
            if ("SidebarFavorites".GetLocalized().Equals(RightClickedItem.Text))
            {
                AppSettings.ShowFavoritesSection = false;
                App.SidebarPinnedController.Model.UpdateFavoritesSectionVisibility();
            }
            else if ("SidebarLibraries".GetLocalized().Equals(RightClickedItem.Text))
            {
                AppSettings.ShowLibrarySection = false;
                App.LibraryManager.UpdateLibrariesSectionVisibility();
            }
            else if ("SidebarCloudDrives".GetLocalized().Equals(RightClickedItem.Text))
            {
                AppSettings.ShowCloudDrivesSection = false;
                App.CloudDrivesManager.UpdateCloudDrivesSectionVisibility();
            }
            else if ("SidebarDrives".GetLocalized().Equals(RightClickedItem.Text))
            {
                AppSettings.ShowDrivesSection = false;
                App.DrivesManager.UpdateDrivesSectionVisibility();
            }
            else if ("SidebarNetworkDrives".GetLocalized().Equals(RightClickedItem.Text))
            {
                AppSettings.ShowNetworkDrivesSection = false;
                App.NetworkDrivesManager.UpdateNetworkDrivesSectionVisibility();
            }
            else if ("WSL".GetLocalized().Equals(RightClickedItem.Text))
            {
                AppSettings.ShowWslSection = false;
                App.WSLDistroManager.UpdateWslSectionVisibility();
            }
        }

        public void UnpinItem_Click(object sender, RoutedEventArgs e)
        {
            if (string.Equals(AppSettings.RecycleBinPath, RightClickedItem.Path, StringComparison.OrdinalIgnoreCase))
            {
                AppSettings.PinRecycleBinToSideBar = false;
            }
            else if (RightClickedItem.Section == SectionType.Favorites)
            {
                App.SidebarPinnedController.Model.RemoveItem(RightClickedItem.Path.ToString());
            }
        }

        public void MoveItemToTop_Click(object sender, RoutedEventArgs e)
        {
            if (RightClickedItem.Section == SectionType.Favorites)
            {
                bool isSelectedSidebarItem = false;

                if (SelectedSidebarItem == RightClickedItem)
                {
                    isSelectedSidebarItem = true;
                }

                int oldIndex = App.SidebarPinnedController.Model.IndexOfItem(RightClickedItem);
                App.SidebarPinnedController.Model.MoveItem(RightClickedItem, oldIndex, 1);

                if (isSelectedSidebarItem)
                {
                    SetValue(SelectedSidebarItemProperty, RightClickedItem);
                }
            }
        }

        public void MoveItemUp_Click(object sender, RoutedEventArgs e)
        {
            if (RightClickedItem.Section == SectionType.Favorites)
            {
                bool isSelectedSidebarItem = false;

                if (SelectedSidebarItem == RightClickedItem)
                {
                    isSelectedSidebarItem = true;
                }

                int oldIndex = App.SidebarPinnedController.Model.IndexOfItem(RightClickedItem);
                App.SidebarPinnedController.Model.MoveItem(RightClickedItem, oldIndex, oldIndex - 1);

                if (isSelectedSidebarItem)
                {
                    SetValue(SelectedSidebarItemProperty, RightClickedItem);
                }
            }
        }

        public void MoveItemDown_Click(object sender, RoutedEventArgs e)
        {
            if (RightClickedItem.Section == SectionType.Favorites)
            {
                bool isSelectedSidebarItem = false;

                if (SelectedSidebarItem == RightClickedItem)
                {
                    isSelectedSidebarItem = true;
                }

                int oldIndex = App.SidebarPinnedController.Model.IndexOfItem(RightClickedItem);
                App.SidebarPinnedController.Model.MoveItem(RightClickedItem, oldIndex, oldIndex + 1);

                if (isSelectedSidebarItem)
                {
                    SetValue(SelectedSidebarItemProperty, RightClickedItem);
                }
            }
        }

        public void MoveItemToBottom_Click(object sender, RoutedEventArgs e)
        {
            if (RightClickedItem.Section == SectionType.Favorites)
            {
                bool isSelectedSidebarItem = false;

                if (SelectedSidebarItem == RightClickedItem)
                {
                    isSelectedSidebarItem = true;
                }

                int oldIndex = App.SidebarPinnedController.Model.IndexOfItem(RightClickedItem);
                App.SidebarPinnedController.Model.MoveItem(RightClickedItem, oldIndex, App.SidebarPinnedController.Model.FavoriteItems.Count());

                if (isSelectedSidebarItem)
                {
                    SetValue(SelectedSidebarItemProperty, RightClickedItem);
                }
            }
        }

        public static GridLength GetSidebarCompactSize()
        {
            if (App.Current.Resources.TryGetValue("NavigationViewCompactPaneLength", out object paneLength))
            {
                if (paneLength is double paneLengthDouble)
                {
                    return new GridLength(paneLengthDouble);
                }
            }
            return new GridLength(200);
        }

        private async void Sidebar_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            if (IsInPointerPressed || args.InvokedItem == null || args.InvokedItemContainer == null)
            {
                IsInPointerPressed = false;
                return;
            }

            string navigationPath = args.InvokedItemContainer.Tag?.ToString();

            if (await CheckEmptyDrive(navigationPath))
            {
                return;
            }

            var ctrlPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            if (ctrlPressed && navigationPath is not null)
            {
                await NavigationHelpers.OpenPathInNewTab(navigationPath);
                return;
            }

            SidebarItemInvoked?.Invoke(this, new SidebarItemInvokedEventArgs(args.InvokedItemContainer));
        }

        private async void Sidebar_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var properties = e.GetCurrentPoint(null).Properties;
            var context = (sender as Microsoft.UI.Xaml.Controls.NavigationViewItem).DataContext;
            if (properties.IsMiddleButtonPressed && context is INavigationControlItem item && item.Path != null)
            {
                if (await CheckEmptyDrive(item.Path))
                {
                    return;
                }
                IsInPointerPressed = true;
                e.Handled = true;
                await NavigationHelpers.OpenPathInNewTab(item.Path);
            }
        }

        private void NavigationViewLocationItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var itemContextMenuFlyout = new Microsoft.UI.Xaml.Controls.CommandBarFlyout();
            var sidebarItem = sender as Microsoft.UI.Xaml.Controls.NavigationViewItem;
            var item = sidebarItem.DataContext as LocationItem;

            bool drivesHeader = "SidebarDrives".GetLocalized().Equals(item.Text);
            bool networkDrivesHeader = "SidebarNetworkDrives".GetLocalized().Equals(item.Text);
            bool cloudDrivesHeader = "SidebarCloudDrives".GetLocalized().Equals(item.Text);
            bool librariesHeader = "SidebarLibraries".GetLocalized().Equals(item.Text);
            bool wslHeader = "WSL".GetLocalized().Equals(item.Text);
            bool favoritesHeader = "SidebarFavorites".GetLocalized().Equals(item.Text);
            bool header = drivesHeader || networkDrivesHeader || cloudDrivesHeader || librariesHeader || wslHeader || favoritesHeader;

            if (!header)
            {
                bool library = item.Section == SectionType.Library;
                bool favorite = item.Section == SectionType.Favorites;

                IsLocationItem = true;
                ShowProperties = true;
                IsLibrariesHeader = false;
                ShowUnpinItem = ((library || favorite) && !item.IsDefaultLocation);
                ShowMoveItemUp = ShowUnpinItem && App.SidebarPinnedController.Model.IndexOfItem(item) > 1;
                ShowMoveItemDown = ShowUnpinItem && App.SidebarPinnedController.Model.IndexOfItem(item) < App.SidebarPinnedController.Model.FavoriteItems.Count();
                ShowHideSection = false;
                ShowEjectDevice = false;

                if (string.Equals(item.Path, "Home".GetLocalized(), StringComparison.OrdinalIgnoreCase))
                {
                    ShowProperties = false;
                }

                if (string.Equals(item.Path, AppSettings.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
                {
                    ShowEmptyRecycleBin = true;
                    ShowUnpinItem = true;
                    ShowProperties = false;
                }
                else
                {
                    ShowEmptyRecycleBin = false;
                }

                RightClickedItem = item;
                var menuItems = GetLocationItemMenuItems();
                var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);

                if (!App.AppSettings.MoveOverflowMenuItemsToSubMenu)
                {
                    secondaryElements.OfType<FrameworkElement>().ForEach(i => i.MinWidth = 250); // Set menu min width if the overflow menu setting is disabled
                }

                secondaryElements.ForEach(i => itemContextMenuFlyout.SecondaryCommands.Add(i));
                itemContextMenuFlyout.ShowAt(sidebarItem, new Windows.UI.Xaml.Controls.Primitives.FlyoutShowOptions() { Position = e.GetPosition(sidebarItem) });

                LoadShellMenuItems(itemContextMenuFlyout);
            }
            else
            {
                IsLocationItem = false;
                ShowProperties = false;
                IsLibrariesHeader = librariesHeader;
                ShowUnpinItem = false;
                ShowMoveItemUp = false;
                ShowMoveItemDown = false;
                ShowHideSection = true;
                ShowEjectDevice = false;
                ShowEmptyRecycleBin = false;

                RightClickedItem = item;
                var menuItems = GetLocationItemMenuItems();
                var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);
                secondaryElements.ForEach(i => itemContextMenuFlyout.SecondaryCommands.Add(i));
                itemContextMenuFlyout.ShowAt(sidebarItem, new Windows.UI.Xaml.Controls.Primitives.FlyoutShowOptions() { Position = e.GetPosition(sidebarItem) });
            }

            e.Handled = true;
        }

        private void NavigationViewDriveItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var itemContextMenuFlyout = new Microsoft.UI.Xaml.Controls.CommandBarFlyout();
            var sidebarItem = sender as Microsoft.UI.Xaml.Controls.NavigationViewItem;
            var item = sidebarItem.DataContext as DriveItem;

            IsLocationItem = true;
            IsLibrariesHeader = false;
            ShowEjectDevice = item.IsRemovable;
            ShowUnpinItem = false;
            ShowMoveItemUp = false;
            ShowMoveItemDown = false;
            ShowEmptyRecycleBin = false;
            ShowProperties = true;
            ShowHideSection = false;

            RightClickedItem = item;
            var menuItems = GetLocationItemMenuItems();
            var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);

            if (!App.AppSettings.MoveOverflowMenuItemsToSubMenu)
            {
                secondaryElements.OfType<FrameworkElement>().ForEach(i => i.MinWidth = 250); // Set menu min width if the overflow menu setting is disabled
            }

            secondaryElements.ForEach(i => itemContextMenuFlyout.SecondaryCommands.Add(i));
            itemContextMenuFlyout.ShowAt(sidebarItem, new Windows.UI.Xaml.Controls.Primitives.FlyoutShowOptions() { Position = e.GetPosition(sidebarItem) });

            LoadShellMenuItems(itemContextMenuFlyout);

            e.Handled = true;
        }

        private void NavigationViewWSLItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var itemContextMenuFlyout = new Microsoft.UI.Xaml.Controls.CommandBarFlyout();
            var sidebarItem = sender as Microsoft.UI.Xaml.Controls.NavigationViewItem;
            var item = sidebarItem.DataContext as WslDistroItem;

            IsLocationItem = true;
            IsLibrariesHeader = false;
            ShowEjectDevice = false;
            ShowUnpinItem = false;
            ShowMoveItemUp = false;
            ShowMoveItemDown = false;
            ShowEmptyRecycleBin = false;
            ShowProperties = false;
            ShowHideSection = false;

            RightClickedItem = item;
            var menuItems = GetLocationItemMenuItems();
            var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);
            secondaryElements.ForEach(i => itemContextMenuFlyout.SecondaryCommands.Add(i));
            itemContextMenuFlyout.ShowAt(sidebarItem, new Windows.UI.Xaml.Controls.Primitives.FlyoutShowOptions() { Position = e.GetPosition(sidebarItem) });

            e.Handled = true;
        }

        private async void OpenInNewTab_Click(object sender, RoutedEventArgs e)
        {
            if (await CheckEmptyDrive(RightClickedItem.Path))
            {
                return;
            }
            await NavigationHelpers.OpenPathInNewTab(RightClickedItem.Path);
        }

        private async void OpenInNewWindow_Click(object sender, RoutedEventArgs e)
        {
            if (await CheckEmptyDrive(RightClickedItem.Path))
            {
                return;
            }
            await NavigationHelpers.OpenPathInNewWindowAsync(RightClickedItem.Path);
        }

        private void NavigationViewItem_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            if (!((sender as Microsoft.UI.Xaml.Controls.NavigationViewItem).DataContext is LocationItem locationItem))
            {
                return;
            }

            // Adding the original Location item dragged to the DragEvents data view
            var navItem = (sender as Microsoft.UI.Xaml.Controls.NavigationViewItem);
            args.Data.Properties.Add("sourceLocationItem", navItem);
        }

        private object dragOverSection, dragOverItem = null;

        private bool isDropOnProcess = false;

        private void NavigationViewItem_DragEnter(object sender, DragEventArgs e)
        {
            VisualStateManager.GoToState(sender as Microsoft.UI.Xaml.Controls.NavigationViewItem, "DragEnter", false);

            if ((sender as Microsoft.UI.Xaml.Controls.NavigationViewItem).DataContext is INavigationControlItem iNavItem)
            {
                if (string.IsNullOrEmpty(iNavItem.Path))
                {
                    dragOverSection = sender;
                    dragOverSectionTimer.Stop();
                    dragOverSectionTimer.Debounce(() =>
                    {
                        if (dragOverSection != null)
                        {
                            dragOverSectionTimer.Stop();
                            if ((dragOverSection as Microsoft.UI.Xaml.Controls.NavigationViewItem).DataContext is LocationItem section)
                            {
                                section.IsExpanded = true;
                            }
                            dragOverSection = null;
                        }
                    }, TimeSpan.FromMilliseconds(1000), false);
                }
                else
                {
                    dragOverItem = sender;
                    dragOverItemTimer.Stop();
                    dragOverItemTimer.Debounce(() =>
                    {
                        if (dragOverItem != null)
                        {
                            dragOverItemTimer.Stop();
                            SidebarItemInvoked?.Invoke(this, new SidebarItemInvokedEventArgs(dragOverItem as Microsoft.UI.Xaml.Controls.NavigationViewItemBase));
                            dragOverItem = null;
                        }
                    }, TimeSpan.FromMilliseconds(1000), false);
                }
            }
        }

        private void NavigationViewItem_DragLeave(object sender, DragEventArgs e)
        {
            VisualStateManager.GoToState(sender as Microsoft.UI.Xaml.Controls.NavigationViewItem, "DragLeave", false);

            isDropOnProcess = false;

            if ((sender as Microsoft.UI.Xaml.Controls.NavigationViewItem).DataContext is INavigationControlItem)
            {
                if (sender == dragOverItem)
                {
                    // Reset dragged over item
                    dragOverItem = null;
                }
                if (sender == dragOverSection)
                {
                    // Reset dragged over item
                    dragOverSection = null;
                }
            }
        }

        private async void NavigationViewLocationItem_DragOver(object sender, DragEventArgs e)
        {
            if (!((sender as Microsoft.UI.Xaml.Controls.NavigationViewItem)?.DataContext is LocationItem locationItem))
            {
                return;
            }

            var deferral = e.GetDeferral();

            if (Filesystem.FilesystemHelpers.HasDraggedStorageItems(e.DataView))
            {
                e.Handled = true;
                isDropOnProcess = true;

                var (handledByFtp, storageItems) = await Filesystem.FilesystemHelpers.GetDraggedStorageItems(e.DataView);
                storageItems ??= new List<IStorageItemWithPath>();

                if (string.IsNullOrEmpty(locationItem.Path) && SectionType.Favorites.Equals(locationItem.Section) && storageItems.Any())
                {
                    bool haveFoldersToPin = false;

                    foreach (var item in storageItems)
                    {
                        if (item.ItemType == FilesystemItemType.Directory && !SidebarPinnedModel.FavoriteItems.Contains(item.Path))
                        {
                            haveFoldersToPin = true;
                            break;
                        }
                    }

                    if (!haveFoldersToPin)
                    {
                        e.AcceptedOperation = DataPackageOperation.None;
                    }
                    else
                    {
                        e.DragUIOverride.IsCaptionVisible = true;
                        e.DragUIOverride.Caption = "BaseLayoutItemContextFlyoutPinToFavorites/Text".GetLocalized();
                        e.AcceptedOperation = DataPackageOperation.Move;
                    }
                }
                else if (string.IsNullOrEmpty(locationItem.Path) ||
                    (storageItems.Any() && storageItems.AreItemsAlreadyInFolder(locationItem.Path))
                    || locationItem.Path.StartsWith("Home".GetLocalized(), StringComparison.OrdinalIgnoreCase))
                {
                    e.AcceptedOperation = DataPackageOperation.None;
                }
                else if (handledByFtp)
                {
                    if (locationItem.Path.StartsWith(App.AppSettings.RecycleBinPath))
                    {
                        e.AcceptedOperation = DataPackageOperation.None;
                    }
                    else
                    {
                        e.DragUIOverride.IsCaptionVisible = true;
                        e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), locationItem.Text);
                        e.AcceptedOperation = DataPackageOperation.Copy;
                    }
                }
                else if (!storageItems.Any())
                {
                    e.AcceptedOperation = DataPackageOperation.None;
                }
                else
                {
                    e.DragUIOverride.IsCaptionVisible = true;
                    if (locationItem.Path.StartsWith(App.AppSettings.RecycleBinPath))
                    {
                        e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalized(), locationItem.Text);
                        e.AcceptedOperation = DataPackageOperation.Move;
                    }
                    else if (e.Modifiers.HasFlag(DragDropModifiers.Alt) || e.Modifiers.HasFlag(DragDropModifiers.Control | DragDropModifiers.Shift))
                    {
                        e.DragUIOverride.Caption = string.Format("LinkToFolderCaptionText".GetLocalized(), locationItem.Text);
                        e.AcceptedOperation = DataPackageOperation.Link;
                    }
                    else if (e.Modifiers.HasFlag(DragDropModifiers.Control))
                    {
                        e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), locationItem.Text);
                        e.AcceptedOperation = DataPackageOperation.Copy;
                    }
                    else if (e.Modifiers.HasFlag(DragDropModifiers.Shift))
                    {
                        e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalized(), locationItem.Text);
                        e.AcceptedOperation = DataPackageOperation.Move;
                    }
                    else if (storageItems.Any(x => x.Item is ZipStorageFile || x.Item is ZipStorageFolder)
                        || ZipStorageFolder.IsZipPath(locationItem.Path))
                    {
                        e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), locationItem.Text);
                        e.AcceptedOperation = DataPackageOperation.Copy;
                    }
                    else if (storageItems.AreItemsInSameDrive(locationItem.Path) || locationItem.IsDefaultLocation)
                    {
                        e.AcceptedOperation = DataPackageOperation.Move;
                        e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalized(), locationItem.Text);
                    }
                    else
                    {
                        e.AcceptedOperation = DataPackageOperation.Copy;
                        e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), locationItem.Text);
                    }
                }
            }
            else if ((e.DataView.Properties["sourceLocationItem"] as Microsoft.UI.Xaml.Controls.NavigationViewItem)?.DataContext is LocationItem sourceLocationItem)
            {
                // else if the drag over event is called over a location item
                NavigationViewLocationItem_DragOver_SetCaptions(locationItem, sourceLocationItem, e);
            }

            deferral.Complete();
        }

        /// <summary>
        /// Sets the captions when dragging a location item over another location item
        /// </summary>
        /// <param name="senderLocationItem">The location item which fired the DragOver event</param>
        /// <param name="sourceLocationItem">The source location item</param>
        /// <param name="e">DragEvent args</param>
        private void NavigationViewLocationItem_DragOver_SetCaptions(LocationItem senderLocationItem, LocationItem sourceLocationItem, DragEventArgs e)
        {
            // If the location item is the same as the original dragged item
            if (sourceLocationItem.Equals(senderLocationItem))
            {
                e.AcceptedOperation = DataPackageOperation.None;
                e.DragUIOverride.IsCaptionVisible = false;
            }
            else
            {
                e.AcceptedOperation = DataPackageOperation.Move;
                e.DragUIOverride.IsCaptionVisible = true;
                e.DragUIOverride.Caption = "PinToSidebarByDraggingCaptionText".GetLocalized();
            }
        }

        private async void NavigationViewLocationItem_Drop(object sender, DragEventArgs e)
        {
            dragOverItem = null; // Reset dragged over item
            dragOverSection = null; // Reset dragged over section

            if (!((sender as Microsoft.UI.Xaml.Controls.NavigationViewItem).DataContext is LocationItem locationItem))
            {
                return;
            }

            // If the dropped item is a folder or file from a file system
            if (Filesystem.FilesystemHelpers.HasDraggedStorageItems(e.DataView))
            {
                VisualStateManager.GoToState(sender as Microsoft.UI.Xaml.Controls.NavigationViewItem, "Drop", false);

                var deferral = e.GetDeferral();

                if (string.IsNullOrEmpty(locationItem.Path) && SectionType.Favorites.Equals(locationItem.Section) && isDropOnProcess) // Pin to Favorites section
                {
                    var storageItems = await e.DataView.GetStorageItemsAsync();
                    foreach (var item in storageItems)
                    {
                        if (item.IsOfType(StorageItemTypes.Folder) && !SidebarPinnedModel.FavoriteItems.Contains(item.Path))
                        {
                            SidebarPinnedModel.AddItem(item.Path);
                        }
                    }
                }
                else
                {
                    SidebarItemDropped?.Invoke(this, new SidebarItemDroppedEventArgs()
                    {
                        Package = e.DataView,
                        ItemPath = locationItem.Path,
                        AcceptedOperation = e.AcceptedOperation
                    });
                }

                isDropOnProcess = false;
                deferral.Complete();
            }
            else if ((e.DataView.Properties["sourceLocationItem"] as Microsoft.UI.Xaml.Controls.NavigationViewItem).DataContext is LocationItem sourceLocationItem)
            {
                // Else if the dropped item is a location item

                // Swap the two items
                SidebarPinnedModel.SwapItems(sourceLocationItem, locationItem);
            }
        }

        private async void NavigationViewDriveItem_DragOver(object sender, DragEventArgs e)
        {
            if (!((sender as Microsoft.UI.Xaml.Controls.NavigationViewItem).DataContext is DriveItem driveItem) ||
                !Filesystem.FilesystemHelpers.HasDraggedStorageItems(e.DataView))
            {
                return;
            }

            var deferral = e.GetDeferral();
            e.Handled = true;

            var (handledByFtp, storageItems) = await Filesystem.FilesystemHelpers.GetDraggedStorageItems(e.DataView);
            storageItems ??= new List<IStorageItemWithPath>();

            if ("DriveCapacityUnknown".GetLocalized().Equals(driveItem.SpaceText, StringComparison.OrdinalIgnoreCase) ||
                (storageItems.Any() && storageItems.AreItemsAlreadyInFolder(driveItem.Path)))
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
            else if (handledByFtp)
            {
                e.DragUIOverride.IsCaptionVisible = true;
                e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), driveItem.Text);
                e.AcceptedOperation = DataPackageOperation.Copy;
            }
            else if (!storageItems.Any())
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
            else
            {
                e.DragUIOverride.IsCaptionVisible = true;
                if (e.Modifiers.HasFlag(DragDropModifiers.Alt) || e.Modifiers.HasFlag(DragDropModifiers.Control | DragDropModifiers.Shift))
                {
                    e.DragUIOverride.Caption = string.Format("LinkToFolderCaptionText".GetLocalized(), driveItem.Text);
                    e.AcceptedOperation = DataPackageOperation.Link;
                }
                else if (e.Modifiers.HasFlag(DragDropModifiers.Control))
                {
                    e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), driveItem.Text);
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
                else if (e.Modifiers.HasFlag(DragDropModifiers.Shift))
                {
                    e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalized(), driveItem.Text);
                    e.AcceptedOperation = DataPackageOperation.Move;
                }
                else if (storageItems.AreItemsInSameDrive(driveItem.Path))
                {
                    e.AcceptedOperation = DataPackageOperation.Move;
                    e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalized(), driveItem.Text);
                }
                else
                {
                    e.AcceptedOperation = DataPackageOperation.Copy;
                    e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), driveItem.Text);
                }
            }

            deferral.Complete();
        }

        private void NavigationViewDriveItem_Drop(object sender, DragEventArgs e)
        {
            dragOverItem = null; // Reset dragged over item
            dragOverSection = null; // Reset dragged over section

            if (!((sender as Microsoft.UI.Xaml.Controls.NavigationViewItem).DataContext is DriveItem driveItem))
            {
                return;
            }

            VisualStateManager.GoToState(sender as Microsoft.UI.Xaml.Controls.NavigationViewItem, "Drop", false);

            var deferral = e.GetDeferral();
            SidebarItemDropped?.Invoke(this, new SidebarItemDroppedEventArgs()
            {
                Package = e.DataView,
                ItemPath = driveItem.Path,
                AcceptedOperation = e.AcceptedOperation
            });
            deferral.Complete();
        }

        private void Properties_Click(object sender, RoutedEventArgs e)
        {
            SidebarItemPropertiesInvoked?.Invoke(this, new SidebarItemPropertiesInvokedEventArgs(RightClickedItem));
        }

        private async void EjectDevice_Click(object sender, RoutedEventArgs e)
        {
            await DriveHelpers.EjectDeviceAsync(RightClickedItem.Path);
        }

        private void SidebarNavView_Loaded(object sender, RoutedEventArgs e)
        {
            (this.FindDescendant("TabContentBorder") as Border).Child = TabContent;

            DisplayModeChanged += SidebarControl_DisplayModeChanged;
        }

        private void SidebarControl_DisplayModeChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewDisplayModeChangedEventArgs args)
        {
            IsPaneToggleButtonVisible = args.DisplayMode == Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Minimal;
        }

        private void Border_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var step = 1;
            var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
            originalSize = IsPaneOpen ? AppSettings.SidebarWidth.Value : CompactPaneLength;

            if (ctrl.HasFlag(CoreVirtualKeyStates.Down))
            {
                step = 5;
            }

            if (e.Key == VirtualKey.Space || e.Key == VirtualKey.Enter)
            {
                IsPaneOpen = !IsPaneOpen;
                return;
            }

            if (IsPaneOpen)
            {
                if (e.Key == VirtualKey.Left)
                {
                    SetSize(-step, true);
                    e.Handled = true;
                }
                else if (e.Key == VirtualKey.Right)
                {
                    SetSize(step, true);
                    e.Handled = true;
                }
            }
            else if (e.Key == VirtualKey.Right)
            {
                IsPaneOpen = !IsPaneOpen;
                return;
            }

            App.AppSettings.SidebarWidth = new GridLength(OpenPaneLength);
        }

        /// <summary>
        /// true if the user is currently resizing the sidebar
        /// </summary>
        private bool dragging;

        private double originalSize = 0;

        private void Border_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (DisplayMode == Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Expanded)
            {
                SetSize(e.Cumulative.Translation.X);
            }
        }

        private void Border_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!dragging) // keep showing pressed event if currently resizing the sidebar
            {
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
                VisualStateManager.GoToState((sender as Grid).FindAscendant<SplitView>(), "ResizerNormal", true);
            }
        }

        private void SetSize(double val, bool closeImmediatleyOnOversize = false)
        {
            if (IsPaneOpen)
            {
                var newSize = originalSize + val;
                if (newSize <= Constants.UI.MaximumSidebarWidth && newSize >= Constants.UI.MinimumSidebarWidth)
                {
                    OpenPaneLength = newSize; // passing a negative value will cause an exception
                }

                if (newSize < Constants.UI.MinimumSidebarWidth) // if the new size is below the minimum, check whether to toggle the pane
                {
                    if (Constants.UI.MinimumSidebarWidth + val <= CompactPaneLength || closeImmediatleyOnOversize) // collapse the sidebar
                    {
                        IsPaneOpen = false;
                    }
                }
            }
            else
            {
                if (val >= Constants.UI.MinimumSidebarWidth - CompactPaneLength || closeImmediatleyOnOversize)
                {
                    OpenPaneLength = Constants.UI.MinimumSidebarWidth + (val + CompactPaneLength - Constants.UI.MinimumSidebarWidth); // set open sidebar length to minimum value to keep it smooth
                    IsPaneOpen = true;
                }
            }
        }

        private void Border_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (DisplayMode == Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Expanded)
            {
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeWestEast, 0);
                VisualStateManager.GoToState((sender as Grid).FindAscendant<SplitView>(), "ResizerPointerOver", true);
            }
        }

        private void ResizeElementBorder_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
            VisualStateManager.GoToState((sender as Grid).FindAscendant<SplitView>(), "ResizerNormal", true);
            App.AppSettings.SidebarWidth = new GridLength(OpenPaneLength);
            dragging = false;
        }

        private void ResizeElementBorder_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            IsPaneOpen = !IsPaneOpen;
        }

        private async void OpenInNewPane_Click(object sender, RoutedEventArgs e)
        {
            if (await CheckEmptyDrive((RightClickedItem as INavigationControlItem)?.Path))
            {
                return;
            }
            SidebarItemNewPaneInvoked?.Invoke(this, new SidebarItemNewPaneInvokedEventArgs(RightClickedItem));
        }

        private void ResizeElementBorder_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (DisplayMode == Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Expanded)
            {
                originalSize = IsPaneOpen ? AppSettings.SidebarWidth.Value : CompactPaneLength;
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeWestEast, 0);
                VisualStateManager.GoToState((sender as Grid).FindAscendant<SplitView>(), "ResizerPressed", true);
                dragging = true;
            }
        }

        private async Task<bool> CheckEmptyDrive(string drivePath)
        {
            if (drivePath is not null)
            {
                var matchingDrive = App.DrivesManager.Drives.FirstOrDefault(x => drivePath.StartsWith(x.Path));
                if (matchingDrive != null && matchingDrive.Type == DriveType.CDRom && matchingDrive.MaxSpace == ByteSizeLib.ByteSize.FromBytes(0))
                {
                    bool ejectButton = await DialogDisplayHelper.ShowDialogAsync("InsertDiscDialog/Title".GetLocalized(), string.Format("InsertDiscDialog/Text".GetLocalized(), matchingDrive.Path), "InsertDiscDialog/OpenDriveButton".GetLocalized(), "Close".GetLocalized());
                    if (ejectButton)
                    {
                        await DriveHelpers.EjectDeviceAsync(matchingDrive.Path);
                    }
                    return true;
                }
            }
            return false;
        }

        private async void LoadShellMenuItems(Microsoft.UI.Xaml.Controls.CommandBarFlyout itemContextMenuFlyout)
        {
            try
            {
                if (ShowEmptyRecycleBin)
                {
                    var emptyRecycleBinItem = itemContextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton appBarButton && (appBarButton.Tag as string) == "EmptyRecycleBin") as AppBarButton;
                    if (emptyRecycleBinItem is not null)
                    {
                        var binHasItems = await new RecycleBinHelpers().RecycleBinHasItems();
                        emptyRecycleBinItem.IsEnabled = binHasItems;
                    }
                }
                if (IsLocationItem)
                {
                    var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
                    var shellMenuItems = await ContextFlyoutItemHelper.GetItemContextShellCommandsAsync(connection: await AppServiceConnectionHelper.Instance, currentInstanceViewModel: null, workingDir: null,
                        new List<ListedItem>() { new ListedItem(null) { ItemPath = RightClickedItem.Path } }, shiftPressed: shiftPressed, showOpenMenu: false);
                    if (!App.AppSettings.MoveOverflowMenuItemsToSubMenu)
                    {
                        var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(shellMenuItems);
                        if (secondaryElements.Any())
                        {
                            var openedPopups = Windows.UI.Xaml.Media.VisualTreeHelper.GetOpenPopups(Window.Current);
                            var secondaryMenu = openedPopups.FirstOrDefault(popup => popup.Name == "OverflowPopup");
                            var itemsControl = secondaryMenu?.Child.FindDescendant<ItemsControl>();
                            if (itemsControl is not null)
                            {
                                secondaryElements.OfType<FrameworkElement>().ForEach(x => x.MaxWidth = itemsControl.ActualWidth - 10); // Set items max width to current menu width (#5555)
                            }

                            itemContextMenuFlyout.SecondaryCommands.Add(new AppBarSeparator());
                            secondaryElements.ForEach(i => itemContextMenuFlyout.SecondaryCommands.Add(i));
                        }
                    }
                    else
                    {
                        var overflowItems = ItemModelListToContextFlyoutHelper.GetMenuFlyoutItemsFromModel(shellMenuItems);
                        var overflowItem = itemContextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton appBarButton && (appBarButton.Tag as string) == "ItemOverflow") as AppBarButton;
                        if (overflowItem is not null)
                        {
                            overflowItems.ForEach(i => (overflowItem.Flyout as MenuFlyout).Items.Add(i));
                            overflowItem.Visibility = overflowItems.Any() ? Visibility.Visible : Visibility.Collapsed;
                        }
                    }
                }
            }
            catch { }
        }

        public List<ContextMenuFlyoutItemViewModel> GetLocationItemMenuItems()
        {
            return new List<ContextMenuFlyoutItemViewModel>()
            {
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "SideBarCreateNewLibrary/Text".GetLocalized(),
                    Glyph = "\uE710",
                    Command = CreateLibraryCommand,
                    ShowItem = IsLibrariesHeader
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "SideBarRestoreLibraries/Text".GetLocalized(),
                    Glyph = "\uE10E",
                    Command = RestoreLibrariesCommand,
                    ShowItem = IsLibrariesHeader
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutEmptyRecycleBin/Text".GetLocalized(),
                    Glyph = "\uEF88",
                    GlyphFontFamilyName = "RecycleBinIcons",
                    Command = EmptyRecycleBinCommand,
                    ShowItem = ShowEmptyRecycleBin,
                    IsEnabled = false,
                    ID = "EmptyRecycleBin",
                    Tag = "EmptyRecycleBin",
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "SideBarOpenInNewPane/Text".GetLocalized(),
                    Glyph = "\uF117",
                    GlyphFontFamilyName = "CustomGlyph",
                    Command = new RelayCommand(() => OpenInNewPane_Click(null, null)),
                    ShowItem = IsLocationItem && CanOpenInNewPane
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "SideBarOpenInNewTab/Text".GetLocalized(),
                    Glyph = "\uF113",
                    GlyphFontFamilyName = "CustomGlyph",
                    Command = new RelayCommand(() => OpenInNewTab_Click(null, null)),
                    ShowItem = IsLocationItem
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "SideBarOpenInNewWindow/Text".GetLocalized(),
                    Glyph = "\uE737",
                    Command = new RelayCommand(() => OpenInNewWindow_Click(null, null)),
                    ShowItem = IsLocationItem
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "SideBarFavoritesMoveToTop".GetLocalized(),
                    Glyph = "\uE11C",
                    Command = new RelayCommand(() => MoveItemToTop_Click(null, null)),
                    ShowItem = ShowMoveItemUp
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "SideBarFavoritesMoveOneUp".GetLocalized(),
                    Glyph = "\uE70E",
                    Command = new RelayCommand(() => MoveItemUp_Click(null, null)),
                    ShowItem = ShowMoveItemUp
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "SideBarFavoritesMoveOneDown".GetLocalized(),
                    Glyph = "\uE70D",
                    Command = new RelayCommand(() => MoveItemDown_Click(null, null)),
                    ShowItem = ShowMoveItemDown
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "SideBarFavoritesMoveToBottom".GetLocalized(),
                    Glyph = "\uE118",
                    Command = new RelayCommand(() => MoveItemToBottom_Click(null, null)),
                    ShowItem = ShowMoveItemDown
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "SideBarUnpinFromFavorites/Text".GetLocalized(),
                    Glyph = "\uE77A",
                    Command = new RelayCommand(() => UnpinItem_Click(null, null)),
                    ShowItem = ShowUnpinItem
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = string.Format("SideBarHideSectionFromSideBar/Text".GetLocalized(), RightClickedItem.Text),
                    Glyph = "\uE77A",
                    Command = new RelayCommand(() => HideSection_Click(null, null)),
                    ShowItem = ShowHideSection
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "SideBarEjectDevice/Text".GetLocalized(),
                    Glyph = "\uF10B",
                    GlyphFontFamilyName = "CustomGlyph",
                    Command = new RelayCommand(() => EjectDevice_Click(null, null)),
                    ShowItem = ShowEjectDevice
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "BaseLayoutContextFlyoutPropertiesFolder/Text".GetLocalized(),
                    Glyph = "\uE946",
                    Command = new RelayCommand(() => Properties_Click(null, null)),
                    ShowItem = ShowProperties
                },
                new ContextMenuFlyoutItemViewModel()
                {
                    Text = "ContextMenuMoreItemsLabel".GetLocalized(),
                    Glyph = "\xE712",
                    Items = new List<ContextMenuFlyoutItemViewModel>(),
                    ID = "ItemOverflow",
                    Tag = "ItemOverflow",
                    IsHidden = true,
                }
            }.Where(x => x.ShowItem).ToList();
        }
    }

    public class SidebarItemDroppedEventArgs : EventArgs
    {
        public DataPackageView Package { get; set; }
        public string ItemPath { get; set; }
        public DataPackageOperation AcceptedOperation { get; set; }
    }

    public class SidebarItemInvokedEventArgs : EventArgs
    {
        public Microsoft.UI.Xaml.Controls.NavigationViewItemBase InvokedItemContainer { get; set; }

        public SidebarItemInvokedEventArgs(Microsoft.UI.Xaml.Controls.NavigationViewItemBase ItemContainer)
        {
            InvokedItemContainer = ItemContainer;
        }
    }

    public class SidebarItemPropertiesInvokedEventArgs : EventArgs
    {
        public object InvokedItemDataContext { get; set; }

        public SidebarItemPropertiesInvokedEventArgs(object invokedItemDataContext)
        {
            InvokedItemDataContext = invokedItemDataContext;
        }
    }

    public class SidebarItemNewPaneInvokedEventArgs : EventArgs
    {
        public object InvokedItemDataContext { get; set; }

        public SidebarItemNewPaneInvokedEventArgs(object invokedItemDataContext)
        {
            InvokedItemDataContext = invokedItemDataContext;
        }
    }

    public class NavItemDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate LocationNavItemTemplate { get; set; }
        public DataTemplate DriveNavItemTemplate { get; set; }
        public DataTemplate LinuxNavItemTemplate { get; set; }
        public DataTemplate HeaderNavItemTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item != null && item is INavigationControlItem)
            {
                INavigationControlItem navControlItem = item as INavigationControlItem;
                switch (navControlItem.ItemType)
                {
                    case NavigationControlItemType.Location:
                        return LocationNavItemTemplate;

                    case NavigationControlItemType.Drive:
                        return DriveNavItemTemplate;

                    case NavigationControlItemType.CloudDrive:
                        return DriveNavItemTemplate;

                    case NavigationControlItemType.LinuxDistro:
                        return LinuxNavItemTemplate;

                    case NavigationControlItemType.Header:
                        return HeaderNavItemTemplate;
                }
            }
            return null;
        }
    }
}