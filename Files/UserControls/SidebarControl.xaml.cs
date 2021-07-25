using Files.DataModels;
using Files.DataModels.NavigationControlItems;
using Files.Filesystem;
using Files.Helpers;
using Files.UserControls.MultitaskingControl;
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
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

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

        public event EventHandler RecycleBinItemRightTapped;

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

        /// <summary>
        /// ShowUnpinItem property indicating whether the unpin button should by displayed when right-clicking an item in the navigation bar
        /// </summary>
        private bool showUnpinItem;

        /// <summary>
        /// Binding property for the MenuFlyoutItem SideBarUnpinFromFavorites
        /// </summary>
        public bool ShowUnpinItem
        {
            get => showUnpinItem;
            set
            {
                if (value != showUnpinItem)
                {
                    showUnpinItem = value;
                    NotifyPropertyChanged(nameof(ShowUnpinItem));
                }
            }
        }

        private bool showHideSection;

        public bool ShowHideSection
        {
            get => showHideSection;
            set
            {
                if (value != showHideSection)
                {
                    showHideSection = value;
                    NotifyPropertyChanged(nameof(ShowHideSection));
                }
            }
        }

        private bool showProperties;

        public bool ShowProperties
        {
            get => showProperties;
            set
            {
                if (value != showProperties)
                {
                    showProperties = value;
                    NotifyPropertyChanged(nameof(ShowProperties));
                }
            }
        }

        private bool showEmptyRecycleBin;

        public bool ShowEmptyRecycleBin
        {
            get => showEmptyRecycleBin;
            set
            {
                if (value != showEmptyRecycleBin)
                {
                    showEmptyRecycleBin = value;
                    NotifyPropertyChanged(nameof(ShowEmptyRecycleBin));
                }
            }
        }

        private bool showEjectDevice;

        public bool ShowEjectDevice
        {
            get => showEjectDevice;
            set
            {
                if (value != showEjectDevice)
                {
                    showEjectDevice = value;
                    NotifyPropertyChanged(nameof(ShowEjectDevice));
                }
            }
        }

        private bool recycleBinHasItems;

        public bool RecycleBinHasItems
        {
            get => recycleBinHasItems;
            set
            {
                if (value != recycleBinHasItems)
                {
                    recycleBinHasItems = value;
                    NotifyPropertyChanged(nameof(RecycleBinHasItems));
                }
            }
        }

        private bool isLocationItem;

        public bool IsLocationItem
        {
            get => isLocationItem;
            set
            {
                if (value != isLocationItem)
                {
                    isLocationItem = value;
                    NotifyPropertyChanged(nameof(IsLocationItem));
                }
            }
        }

        private bool isLibrariesHeader;

        public bool IsLibrariesHeader
        {
            get => isLibrariesHeader;
            set
            {
                if (value != isLibrariesHeader)
                {
                    isLibrariesHeader = value;
                    NotifyPropertyChanged(nameof(IsLibrariesHeader));
                }
            }
        }

        public INavigationControlItem RightClickedItem;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void HideSection_Click(object sender, RoutedEventArgs e)
        {
            if ("SidebarLibraries".GetLocalized().Equals(RightClickedItem.Text))
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

            var ctrlPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            if (ctrlPressed && !(args.InvokedItemContainer.Tag is null))
            {
                string navigationPath = args.InvokedItemContainer.Tag.ToString();
                await NavigationHelpers.OpenPathInNewTab(navigationPath);
                return;
            }

            SidebarItemInvoked?.Invoke(this, new SidebarItemInvokedEventArgs(args.InvokedItemContainer));
        }

        private async void Sidebar_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var properties = e.GetCurrentPoint(null).Properties;
            var context = (sender as Microsoft.UI.Xaml.Controls.NavigationViewItem).DataContext;
            if (properties.IsMiddleButtonPressed && context is INavigationControlItem item)
            {
                IsInPointerPressed = true;
                await NavigationHelpers.OpenPathInNewTab(item.Path);
                e.Handled = true;
            }
        }

        private void NavigationViewLocationItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
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
                ShowHideSection = false;
                ShowEjectDevice = false;

                if (string.Equals(item.Path, "Home".GetLocalized(), StringComparison.OrdinalIgnoreCase))
                {
                    ShowProperties = false;
                }

                if (string.Equals(item.Path, AppSettings.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
                {
                    RecycleBinItemRightTapped?.Invoke(this, EventArgs.Empty);
                    ShowEmptyRecycleBin = true;
                    ShowUnpinItem = true;
                    ShowProperties = false;
                }
                else
                {
                    ShowEmptyRecycleBin = false;
                }

                RightClickedItem = item;
                SideBarItemContextFlyout.ShowAt(sidebarItem, e.GetPosition(sidebarItem));
            }
            else if (!favoritesHeader)
            {
                IsLocationItem = false;
                ShowProperties = false;
                IsLibrariesHeader = librariesHeader;
                ShowUnpinItem = false;
                ShowHideSection = true;
                ShowEjectDevice = false;
                ShowEmptyRecycleBin = false;

                HideSectionItem.Text = string.Format("SideBarHideSectionFromSideBar/Text".GetLocalized(), item.Text);

                RightClickedItem = item;
                SideBarItemContextFlyout.ShowAt(sidebarItem, e.GetPosition(sidebarItem));
            }

            e.Handled = true;
        }

        private void NavigationViewDriveItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var sidebarItem = sender as Microsoft.UI.Xaml.Controls.NavigationViewItem;
            var item = sidebarItem.DataContext as DriveItem;

            IsLocationItem = true;
            IsLibrariesHeader = false;
            ShowEjectDevice = item.IsRemovable;
            ShowUnpinItem = false;
            ShowEmptyRecycleBin = false;
            ShowProperties = true;

            RightClickedItem = item;
            SideBarItemContextFlyout.ShowAt(sidebarItem, e.GetPosition(sidebarItem));

            e.Handled = true;
        }

        private void NavigationViewWSLItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var sidebarItem = sender as Microsoft.UI.Xaml.Controls.NavigationViewItem;
            var item = sidebarItem.DataContext as WslDistroItem;

            IsLocationItem = true;
            IsLibrariesHeader = false;
            ShowEjectDevice = false;
            ShowUnpinItem = false;
            ShowEmptyRecycleBin = false;
            ShowProperties = true;

            RightClickedItem = item;
            SideBarItemContextFlyout.ShowAt(sidebarItem, e.GetPosition(sidebarItem));

            e.Handled = true;
        }

        private async void OpenInNewTab_Click(object sender, RoutedEventArgs e)
        {
            await NavigationHelpers.OpenPathInNewTab(RightClickedItem.Path);
        }

        private async void OpenInNewWindow_Click(object sender, RoutedEventArgs e)
        {
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

            // If the dragged item is a folder or file from a file system
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var deferral = e.GetDeferral();
                e.Handled = true;
                IReadOnlyList<IStorageItem> storageItems;
                try
                {
                    storageItems = await e.DataView.GetStorageItemsAsync();
                }
                catch (Exception ex) when ((uint)ex.HResult == 0x80040064 || (uint)ex.HResult == 0x8004006A)
                {
                    // Handled by FTP
                    storageItems = new List<IStorageItem>();
                }
                catch (Exception ex)
                {
                    App.Logger.Warn(ex, ex.Message);
                    e.AcceptedOperation = DataPackageOperation.None;
                    deferral.Complete();
                    return;
                }

                if (string.IsNullOrEmpty(locationItem.Path) ||
                    (storageItems.Any() && storageItems.AreItemsAlreadyInFolder(locationItem.Path)))
                {
                    e.AcceptedOperation = DataPackageOperation.None;
                }
                else if (!storageItems.Any())
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
                else
                {
                    e.DragUIOverride.IsCaptionVisible = true;
                    if (locationItem.Path.StartsWith(App.AppSettings.RecycleBinPath))
                    {
                        e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalized(), locationItem.Text);
                        e.AcceptedOperation = DataPackageOperation.Move;
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

                deferral.Complete();
            }
            else if ((e.DataView.Properties["sourceLocationItem"] as Microsoft.UI.Xaml.Controls.NavigationViewItem)?.DataContext is LocationItem sourceLocationItem)
            {
                // else if the drag over event is called over a location item

                NavigationViewLocationItem_DragOver_SetCaptions(locationItem, sourceLocationItem, e);
            }
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

        private void NavigationViewLocationItem_Drop(object sender, DragEventArgs e)
        {
            dragOverItem = null; // Reset dragged over item
            dragOverSection = null; // Reset dragged over section

            if (!((sender as Microsoft.UI.Xaml.Controls.NavigationViewItem).DataContext is LocationItem locationItem))
            {
                return;
            }

            // If the dropped item is a folder or file from a file system
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                VisualStateManager.GoToState(sender as Microsoft.UI.Xaml.Controls.NavigationViewItem, "Drop", false);

                var deferral = e.GetDeferral();
                SidebarItemDropped?.Invoke(this, new SidebarItemDroppedEventArgs()
                {
                    Package = e.DataView,
                    ItemPath = locationItem.Path,
                    AcceptedOperation = e.AcceptedOperation
                });
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
                !e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                return;
            }

            var deferral = e.GetDeferral();
            e.Handled = true;
            IReadOnlyList<IStorageItem> storageItems;
            try
            {
                storageItems = await e.DataView.GetStorageItemsAsync();
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80040064 || (uint)ex.HResult == 0x8004006A)
            {
                // Handled by FTP
                storageItems = new List<IStorageItem>();
            }
            catch (Exception ex)
            {
                App.Logger.Warn(ex, ex.Message);
                e.AcceptedOperation = DataPackageOperation.None;
                deferral.Complete();
                return;
            }

            if ("DriveCapacityUnknown".GetLocalized().Equals(driveItem.SpaceText, StringComparison.OrdinalIgnoreCase) ||
                (storageItems.Any() && storageItems.AreItemsAlreadyInFolder(driveItem.Path)))
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
            else if (!storageItems.Any())
            {
                e.DragUIOverride.IsCaptionVisible = true;
                e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), driveItem.Text);
                e.AcceptedOperation = DataPackageOperation.Copy;
            }
            else
            {
                e.DragUIOverride.IsCaptionVisible = true;
                if (e.Modifiers.HasFlag(DragDropModifiers.Control))
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
            var item = (sender as MenuFlyoutItem).DataContext;
            SidebarItemPropertiesInvoked?.Invoke(this, new SidebarItemPropertiesInvokedEventArgs(item));
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
            } else if(e.Key == VirtualKey.Right)
            {
                IsPaneOpen = !IsPaneOpen;
                return;
            }

            App.AppSettings.SidebarWidth = new GridLength(OpenPaneLength);
        }

        /// <summary>
        /// true if the user is currently resizing the sidebar
        /// </summary>
        bool dragging;

        double originalSize = 0;

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

        private void OpenInNewPane_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as MenuFlyoutItem).DataContext;
            SidebarItemNewPaneInvoked?.Invoke(this, new SidebarItemNewPaneInvokedEventArgs(item));
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