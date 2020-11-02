using Files.Commands;
using Files.Controllers;
using Files.DataModels;
using Files.Enums;
using Files.Filesystem;
using Files.Interacts;
using Files.View_Models;
using Microsoft.Toolkit.Uwp.Extensions;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
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

        /// <summary>
        /// The Model for the pinned sidebar items
        /// </summary>
        public SidebarPinnedModel SidebarPinnedModel => App.SidebarPinnedController.Model;

        public SidebarControl()
        {
            this.InitializeComponent();
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

            OpenSidebarItem(args.InvokedItemContainer);
        }

        private void OpenSidebarItem(Microsoft.UI.Xaml.Controls.NavigationViewItemBase invokedItemContainer)
        {
            string navigationPath; // path to navigate
            Type sourcePageType = null; // type of page to navigate

            switch ((invokedItemContainer.DataContext as INavigationControlItem).ItemType)
            {
                case NavigationControlItemType.Location:
                    {
                        var ItemPath = (invokedItemContainer.DataContext as INavigationControlItem).Path; // Get the path of the invoked item

                        if (ItemPath.Equals("Home", StringComparison.OrdinalIgnoreCase)) // Home item
                        {
                            if (ItemPath.Equals(SelectedSidebarItem?.Path, StringComparison.OrdinalIgnoreCase)) return; // return if already selected

                            navigationPath = "NewTab".GetLocalized();
                            sourcePageType = typeof(YourHome);
                        }
                        else // Any other item
                        {
                            navigationPath = invokedItemContainer.Tag.ToString();
                        }

                        break;
                    }
                case NavigationControlItemType.OneDrive:
                    {
                        navigationPath = App.AppSettings.OneDrivePath;
                        break;
                    }
                default:
                    {
                        navigationPath = invokedItemContainer.Tag.ToString();
                        break;
                    }
            }

            if (string.IsNullOrEmpty(navigationPath) ||
                (!string.IsNullOrEmpty(App.CurrentInstance.FilesystemViewModel.WorkingDirectory) &&
                navigationPath.TrimEnd(Path.DirectorySeparatorChar).Equals(
                    App.CurrentInstance.FilesystemViewModel.WorkingDirectory.TrimEnd(Path.DirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase))) // return if already selected
            {
                return;
            }

            App.CurrentInstance.ContentFrame.Navigate(
                sourcePageType == null ? App.AppSettings.GetLayoutType() : sourcePageType,
                navigationPath,
                new SuppressNavigationTransitionInfo());

            App.CurrentInstance.NavigationToolbar.PathControlDisplayText = App.CurrentInstance.FilesystemViewModel.WorkingDirectory;
        }

        private async void NavigationViewLocationItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
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
                var value = new ValueSet
                {
                    { "Arguments", "RecycleBin" },
                    { "action", "Query" }
                };
                var response = await App.Connection.SendMessageAsync(value);
                if (response.Status == Windows.ApplicationModel.AppService.AppServiceResponseStatus.Success && response.Message.TryGetValue("NumItems", out var numItems))
                {
                    RecycleBinHasItems = (long)numItems > 0;
                }
                else
                {
                    RecycleBinHasItems = false;
                }

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
            App.CurrentInstance.InteractionOperations.OpenPathInNewTab(App.rightClickedItem.Path.ToString());
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
                        OpenSidebarItem(dragOverItem as Microsoft.UI.Xaml.Controls.NavigationViewItem);
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

        private void NavigationViewLocationItem_Drop(object sender, DragEventArgs e)
        {
            if (!((sender as Microsoft.UI.Xaml.Controls.NavigationViewItem).DataContext is LocationItem locationItem))
            {
                return;
            }

            // If the dropped item is a folder or file from a file system
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                VisualStateManager.GoToState(sender as Microsoft.UI.Xaml.Controls.NavigationViewItem, "Drop", false);

                var deferral = e.GetDeferral();
                ItemOperations.PasteItemWithStatus(e.DataView, locationItem.Path, e.AcceptedOperation);
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

        private void NavigationViewDriveItem_Drop(object sender, DragEventArgs e)
        {
            if (!((sender as Microsoft.UI.Xaml.Controls.NavigationViewItem).DataContext is DriveItem driveItem)) return;

            VisualStateManager.GoToState(sender as Microsoft.UI.Xaml.Controls.NavigationViewItem, "Drop", false);

            var deferral = e.GetDeferral();
            ItemOperations.PasteItemWithStatus(e.DataView, driveItem.Path, e.AcceptedOperation);
            deferral.Complete();
        }

        private async void Properties_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as MenuFlyoutItem).DataContext;

            if (item is DriveItem)
            {
                await App.CurrentInstance.InteractionOperations.OpenPropertiesWindow(item);
            }
            else if (item is LocationItem)
            {
                ListedItem listedItem = new ListedItem(null)
                {
                    ItemPath = (item as LocationItem).Path,
                    ItemName = (item as LocationItem).Text,
                    PrimaryItemAttribute = Windows.Storage.StorageItemTypes.Folder,
                    ItemType = "FileFolderListItem".GetLocalized(),
                    LoadFolderGlyph = true
                };
                await App.CurrentInstance.InteractionOperations.OpenPropertiesWindow(listedItem);
            }
        }

        private void SettingsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(Settings));

            return;
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