using Files.Controllers;
using Files.DataModels;
using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.View_Models;
using Microsoft.Toolkit.Uwp.Extensions;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;

namespace Files.Controls
{
    public sealed partial class SidebarControl : UserControl, INotifyPropertyChanged
    {
        public SettingsViewModel AppSettings => App.AppSettings;

        public delegate void SidebarItemInvokedEventHandler(object sender, SidebarItemInvokedEventArgs e);
        public event SidebarItemInvokedEventHandler SidebarItemInvoked;

        public delegate void SidebarItemPropertiesInvokedEventHandler(object sender, SidebarItemPropertiesInvokedEventArgs e);
        public event SidebarItemPropertiesInvokedEventHandler SidebarItemPropertiesInvoked;

        public delegate void SidebarItemDroppedEventHandler(object sender, SidebarItemDroppedEventArgs e);
        public event SidebarItemDroppedEventHandler SidebarItemDropped;

        public event EventHandler RecycleBinItemRightTapped;

        /// <summary>
        /// The Model for the pinned sidebar items
        /// </summary>
        public SidebarPinnedModel SidebarPinnedModel => App.SidebarPinnedController.Model;

        public static readonly DependencyProperty EmptyRecycleBinCommandProperty = DependencyProperty.Register(
          "EmptyRecycleBinCommand",
          typeof(ICommand),
          typeof(SidebarControl),
          new PropertyMetadata(null)
        );
        public ICommand EmptyRecycleBinCommand
        {
            get
            {
                return (ICommand)GetValue(EmptyRecycleBinCommandProperty);
            }
            set
            {
                SetValue(EmptyRecycleBinCommandProperty, value);
            }
        }

        public SidebarControl()
        {
            InitializeComponent();
        }

        private INavigationControlItem _SelectedSidebarItem;

        public INavigationControlItem SelectedSidebarItem
        {
            get
            {
                return _SelectedSidebarItem;
            }
            set
            {
                if (value != _SelectedSidebarItem)
                {
                    _SelectedSidebarItem = value;
                    NotifyPropertyChanged(nameof(SelectedSidebarItem));
                }
            }
        }

        /// <summary>
        /// ShowUnpinItem property indicating whether the unpin button should by displayed when right-clicking an item in the navigation bar
        /// </summary>
        private bool _ShowUnpinItem;

        /// <summary>
        /// Binding property for the MenuFlyoutItem SideBarUnpinFromSideBar
        /// </summary>
        public bool ShowUnpinItem
        {
            get
            {
                return _ShowUnpinItem;
            }
            set
            {
                if (value != _ShowUnpinItem)
                {
                    _ShowUnpinItem = value;
                    NotifyPropertyChanged(nameof(ShowUnpinItem));
                }
            }
        }

        private bool _ShowProperties;

        public bool ShowProperties
        {
            get
            {
                return _ShowProperties;
            }
            set
            {
                if (value != _ShowProperties)
                {
                    _ShowProperties = value;
                    NotifyPropertyChanged(nameof(ShowProperties));
                }
            }
        }

        private bool _ShowEmptyRecycleBin;

        public bool ShowEmptyRecycleBin
        {
            get
            {
                return _ShowEmptyRecycleBin;
            }
            set
            {
                if (value != _ShowEmptyRecycleBin)
                {
                    _ShowEmptyRecycleBin = value;
                    NotifyPropertyChanged(nameof(ShowEmptyRecycleBin));
                }
            }
        }

        private bool _RecycleBinHasItems;

        public bool RecycleBinHasItems
        {
            get
            {
                return _RecycleBinHasItems;
            }
            set
            {
                if (value != _RecycleBinHasItems)
                {
                    _RecycleBinHasItems = value;
                    NotifyPropertyChanged(nameof(RecycleBinHasItems));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Sidebar_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem == null)
            {
                return;
            }
            SidebarItemInvoked?.Invoke(this, new SidebarItemInvokedEventArgs(args.InvokedItemContainer));
        }

        private void NavigationViewLocationItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            Microsoft.UI.Xaml.Controls.NavigationViewItem sidebarItem = (Microsoft.UI.Xaml.Controls.NavigationViewItem)sender;
            var item = sidebarItem.DataContext as LocationItem;

            if (item.IsDefaultLocation)
            {
                ShowUnpinItem = false;
            }
            else
            {
                ShowUnpinItem = true;
            }

            if (item.Path.Equals(App.AppSettings.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
            {
                RecycleBinItemRightTapped?.Invoke(this, EventArgs.Empty);

                ShowEmptyRecycleBin = true;
                ShowUnpinItem = true;
                ShowProperties = false;
            }
            else
            {
                ShowEmptyRecycleBin = false;
                // Set to true if properties should be displayed for pinned folders
                ShowProperties = false;
            }

            // Additional check needed because ShowProperties is set to true if not recycle bin
            if (item.IsDefaultLocation)
            {
                ShowProperties = false;
            }

            SideBarItemContextFlyout.ShowAt(sidebarItem, e.GetPosition(sidebarItem));
            App.rightClickedItem = item;
        }

        private void NavigationViewDriveItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            Microsoft.UI.Xaml.Controls.NavigationViewItem sidebarItem = (Microsoft.UI.Xaml.Controls.NavigationViewItem)sender;

            ShowUnpinItem = false;
            ShowEmptyRecycleBin = false;
            ShowProperties = true;

            SideBarItemContextFlyout.ShowAt(sidebarItem, e.GetPosition(sidebarItem));

            App.rightClickedItem = sidebarItem.DataContext as DriveItem;
        }

        private void OpenInNewTab_Click(object sender, RoutedEventArgs e)
        {
            Interaction.OpenPathInNewTab(App.rightClickedItem.Path.ToString());
        }

        private async void OpenInNewWindow_Click(object sender, RoutedEventArgs e)
        {
            await Interaction.OpenPathInNewWindow(App.rightClickedItem.Path.ToString());
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
        
        private DispatcherTimer dragOverTimer = new DispatcherTimer();

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
            if (!((sender as Microsoft.UI.Xaml.Controls.NavigationViewItem).DataContext is LocationItem locationItem))
            {
                return;
            }

            // If the dragged item is a folder or file from a file system
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var deferral = e.GetDeferral();
                e.Handled = true;
                var storageItems = await e.DataView.GetStorageItemsAsync();

                if (storageItems.Count == 0 ||
                    locationItem.IsDefaultLocation ||
                    locationItem.Path.Equals(App.AppSettings.RecycleBinPath, StringComparison.OrdinalIgnoreCase) ||
                    storageItems.AreItemsAlreadyInFolder(locationItem.Path))
                {
                    e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
                }
                else
                {
                    e.DragUIOverride.IsCaptionVisible = true;
                    if (storageItems.AreItemsInSameDrive(locationItem.Path))
                    {
                        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
                        e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalized(), locationItem.Text);
                    }
                    else
                    {
                        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
                        e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), locationItem.Text);
                    }
                }

                deferral.Complete();
            }
            else if ((e.DataView.Properties["sourceLocationItem"] as Microsoft.UI.Xaml.Controls.NavigationViewItem).DataContext is LocationItem sourceLocationItem)
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
            // If the location item is the same as the original dragged item or the default location (home button), the dragging should be disabled
            if (sourceLocationItem.Equals(senderLocationItem) || senderLocationItem.IsDefaultLocation == true)
            {
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
                e.DragUIOverride.IsCaptionVisible = false;
            }
            else
            {
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
                e.DragUIOverride.IsCaptionVisible = true;
                e.DragUIOverride.Caption = "PinToSidebarByDraggingCaptionText".GetLocalized();
            }
        }

        private async void NavigationViewLocationItem_Drop(object sender, DragEventArgs e)
        {
            var deferral = e.GetDeferral();
            if (!((sender as Microsoft.UI.Xaml.Controls.NavigationViewItem).DataContext is LocationItem locationItem))
                return;

            // If the dropped item is a folder or file from a file system
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                VisualStateManager.GoToState(sender as Microsoft.UI.Xaml.Controls.NavigationViewItem, "Drop", false);

                SidebarItemDropped?.Invoke(this, new SidebarItemDroppedEventArgs() { Package = e.DataView, ItemPath = locationItem.Path, AcceptedOperation = e.AcceptedOperation });
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
                !e.DataView.Contains(StandardDataFormats.StorageItems)) return;

            var deferral = e.GetDeferral();
            e.Handled = true;
            var storageItems = await e.DataView.GetStorageItemsAsync();

            if (storageItems.Count == 0 ||
                "Unknown".Equals(driveItem.SpaceText, StringComparison.OrdinalIgnoreCase) ||
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

        private async void NavigationViewDriveItem_Drop(object sender, DragEventArgs e)
        {
            var deferral = e.GetDeferral();

            if (!((sender as Microsoft.UI.Xaml.Controls.NavigationViewItem).DataContext is DriveItem driveItem)) return;

            VisualStateManager.GoToState(sender as Microsoft.UI.Xaml.Controls.NavigationViewItem, "Drop", false);

            SidebarItemDropped?.Invoke(this, new SidebarItemDroppedEventArgs() { Package = e.DataView, ItemPath = driveItem.Path, AcceptedOperation = e.AcceptedOperation });
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
            rootFrame.Navigate(typeof(Settings));

            return;
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

                    case NavigationControlItemType.OneDrive:
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