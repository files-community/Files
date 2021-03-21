using Files.DataModels;
using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.ViewModels;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Files.UserControls
{
    public sealed partial class SidebarControl : UserControl, INotifyPropertyChanged
    {
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

        public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(SidebarControl), new PropertyMetadata(true));

        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        public static readonly DependencyProperty IsCompactProperty = DependencyProperty.Register(nameof(IsCompact), typeof(bool), typeof(SidebarControl), new PropertyMetadata(false));

        public bool IsCompact
        {
            get => (bool)GetValue(IsCompactProperty);
            set => SetValue(IsCompactProperty, value);
        }

        public static readonly DependencyProperty EmptyRecycleBinCommandProperty = DependencyProperty.Register(nameof(EmptyRecycleBinCommand), typeof(ICommand), typeof(SidebarControl), new PropertyMetadata(null));

        public ICommand EmptyRecycleBinCommand
        {
            get => (ICommand)GetValue(EmptyRecycleBinCommandProperty);
            set => SetValue(EmptyRecycleBinCommandProperty, value);
        }

        private DispatcherQueueTimer dragOverTimer;

        public SidebarControl()
        {
            this.InitializeComponent();
            SidebarNavView.Loaded += SidebarNavView_Loaded;

            dragOverTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        }

        private INavigationControlItem selectedSidebarItem;

        public INavigationControlItem SelectedSidebarItem
        {
            get => selectedSidebarItem;
            set
            {
                if (value != selectedSidebarItem)
                {
                    selectedSidebarItem = value;
                    NotifyPropertyChanged(nameof(SelectedSidebarItem));
                }
            }
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
        /// Binding property for the MenuFlyoutItem SideBarUnpinFromSideBar
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

        public INavigationControlItem RightClickedItem;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void UnpinItem_Click(object sender, RoutedEventArgs e)
        {
            if (RightClickedItem.Path.Equals(AppSettings.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
            {
                AppSettings.PinRecycleBinToSideBar = false;
            }
            else if (RightClickedItem.Section == SectionType.Favorites)
            {
                App.SidebarPinnedController.Model.RemoveItem(RightClickedItem.Path.ToString());
            }
        }

        private void Sidebar_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem == null || args.InvokedItemContainer == null)
            {
                return;
            }
            SidebarItemInvoked?.Invoke(this, new SidebarItemInvokedEventArgs(args.InvokedItemContainer));
        }

        private void NavigationViewLocationItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            Microsoft.UI.Xaml.Controls.NavigationViewItem sidebarItem = (Microsoft.UI.Xaml.Controls.NavigationViewItem)sender;
            var item = sidebarItem.DataContext as LocationItem;

            if (!item.Text.Equals("SidebarDrives".GetLocalized()) &&
                !item.Text.Equals("SidebarNetworkDrives".GetLocalized()) &&
                !item.Text.Equals("SidebarCloudDrives".GetLocalized()) &&
                !item.Text.Equals("SidebarLibraries".GetLocalized()) &&
                !item.Text.Equals("WSL") &&
                !item.Text.Equals("SidebarFavorites".GetLocalized()))
            {
                ShowEmptyRecycleBin = false;
                ShowUnpinItem = true;
                ShowProperties = true;
                ShowEjectDevice = false;

                if (item.IsDefaultLocation)
                {
                    if (item.Section != SectionType.Library)
                    {
                        ShowProperties = false;
                    }

                    if (item.Path.Equals(App.AppSettings.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
                    {
                        RecycleBinItemRightTapped?.Invoke(this, EventArgs.Empty);
                        ShowEmptyRecycleBin = true;
                    }
                    else
                    {
                        ShowUnpinItem = false;
                    }
                }

                RightClickedItem = item;
                SideBarItemContextFlyout.ShowAt(sidebarItem, e.GetPosition(sidebarItem));
            }

            e.Handled = true;
        }

        private void NavigationViewDriveItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            Microsoft.UI.Xaml.Controls.NavigationViewItem sidebarItem = (Microsoft.UI.Xaml.Controls.NavigationViewItem)sender;
            var item = sidebarItem.DataContext as DriveItem;

            ShowEjectDevice = item.IsRemovable;
            ShowUnpinItem = false;
            ShowEmptyRecycleBin = false;
            ShowProperties = true;

            SideBarItemContextFlyout.ShowAt(sidebarItem, e.GetPosition(sidebarItem));

            RightClickedItem = item;

            e.Handled = true;
        }

        private void NavigationViewWSLItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            Microsoft.UI.Xaml.Controls.NavigationViewItem sidebarItem = (Microsoft.UI.Xaml.Controls.NavigationViewItem)sender;
            var item = sidebarItem.DataContext as WSLDistroItem;

            ShowEjectDevice = false;
            ShowUnpinItem = false;
            ShowEmptyRecycleBin = false;
            ShowProperties = true;

            SideBarItemContextFlyout.ShowAt(sidebarItem, e.GetPosition(sidebarItem));

            RightClickedItem = item;

            e.Handled = true;
        }

        private void OpenInNewTab_Click(object sender, RoutedEventArgs e)
        {
            Interaction.OpenPathInNewTab(RightClickedItem.Path);
        }

        private async void OpenInNewWindow_Click(object sender, RoutedEventArgs e)
        {
            await Interaction.OpenPathInNewWindowAsync(RightClickedItem.Path);
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

        private object dragOverItem = null;

        private void NavigationViewItem_DragEnter(object sender, DragEventArgs e)
        {
            VisualStateManager.GoToState(sender as Microsoft.UI.Xaml.Controls.NavigationViewItem, "DragEnter", false);

            if ((sender as Microsoft.UI.Xaml.Controls.NavigationViewItem).DataContext is INavigationControlItem)
            {
                dragOverItem = sender;
                dragOverTimer.Stop();
                dragOverTimer.Debounce(() =>
                {
                    if (dragOverItem != null)
                    {
                        dragOverTimer.Stop();
                        if ((dragOverItem as Microsoft.UI.Xaml.Controls.NavigationViewItem).DataContext is LocationItem locItem)
                        {
                            locItem.IsExpanded = true;
                        }
                        SidebarItemInvoked?.Invoke(this, new SidebarItemInvokedEventArgs(dragOverItem as Microsoft.UI.Xaml.Controls.NavigationViewItem));
                        dragOverItem = null;
                    }
                }, TimeSpan.FromMilliseconds(1000), false);
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
                catch (Exception ex) when ((uint)ex.HResult == 0x80040064)
                {
                    e.AcceptedOperation = DataPackageOperation.None;
                    deferral.Complete();
                    return;
                }
                catch (Exception ex)
                {
                    NLog.LogManager.GetCurrentClassLogger().Warn(ex, ex.Message);
                    e.AcceptedOperation = DataPackageOperation.None;
                    deferral.Complete();
                    return;
                }

                if (storageItems.Count == 0 ||
                    string.IsNullOrEmpty(locationItem.Path) ||
                    locationItem.Path.Equals(App.AppSettings.RecycleBinPath, StringComparison.OrdinalIgnoreCase) ||
                    storageItems.AreItemsAlreadyInFolder(locationItem.Path))
                {
                    e.AcceptedOperation = DataPackageOperation.None;
                }
                else
                {
                    e.DragUIOverride.IsCaptionVisible = true;
                    if (storageItems.AreItemsInSameDrive(locationItem.Path) || locationItem.IsDefaultLocation)
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
            catch (Exception ex) when ((uint)ex.HResult == 0x80040064)
            {
                e.AcceptedOperation = DataPackageOperation.None;
                deferral.Complete();
                return;
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(ex, ex.Message);
                e.AcceptedOperation = DataPackageOperation.None;
                deferral.Complete();
                return;
            }

            if (storageItems.Count == 0 ||
                "DriveCapacityUnknown".GetLocalized().Equals(driveItem.SpaceText, StringComparison.OrdinalIgnoreCase) ||
                storageItems.AreItemsAlreadyInFolder(driveItem.Path))
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
            else
            {
                e.DragUIOverride.IsCaptionVisible = true;
                if (storageItems.AreItemsInSameDrive(driveItem.Path))
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

        private void SettingsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(Views.Settings));

            return;
        }

        private async void EjectDevice_Click(object sender, RoutedEventArgs e)
        {
            await DeviceHelpers.EjectDeviceAsync(RightClickedItem.Path);
        }

        private void SidebarNavView_Loaded(object sender, RoutedEventArgs e)
        {
            var settings = (Microsoft.UI.Xaml.Controls.NavigationViewItem)SidebarNavView.SettingsItem;
            settings.SelectsOnInvoked = false;

            SidebarNavView.Loaded -= SidebarNavView_Loaded;
        }

        private void OpenInNewPane_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as MenuFlyoutItem).DataContext;
            SidebarItemNewPaneInvoked?.Invoke(this, new SidebarItemNewPaneInvokedEventArgs(item));
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