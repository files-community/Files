using Files.Commands;
using Files.DataModels;
using Files.Enums;
using Files.Filesystem;
using Files.Interacts;
using Files.View_Models;
using Microsoft.Toolkit.Uwp.UI;
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

        /// <summary>
        /// ShowOrderBy property indicating whether the Sort by button should by displayed when right-clicking an item in the navigation bar
        /// </summary>
        private bool _ShowOrderBy;

        /// <summary>
        /// Binding property for the MenuFlyoutItem SideBarSortSideBarItems
        /// </summary>
        public bool ShowOrderBy
        {
            get
            {
                return _ShowOrderBy;
            }
            set
            {
                if (value != _ShowOrderBy)
                {
                    _ShowOrderBy = value;
                    NotifyPropertyChanged(nameof(ShowOrderBy));
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

        /// <summary>
        /// IsLocationItemSortedByName property indicating whether the location items in the navigation bar should be sorted by name
        /// </summary>
        private bool _IsLocationItemSortedByName = false;

        /// <summary>
        /// Binding property for the RadioMenuFlyoutItem IsLocationItemSortedByName
        /// </summary>
        public bool IsLocationItemSortedByName
        {
            get => SidebarPinnedModel.SidebarSortOption == SidebarSortOption.Name;
            set
            {
                if (value && SidebarPinnedModel.SidebarSortOption != SidebarSortOption.Name)
                {
                    SidebarPinnedModel.SidebarSortOption = SidebarSortOption.Name;
                }

                if (_IsLocationItemSortedByName != value)
                {
                    _IsLocationItemSortedByName = value;
                    NotifyPropertyChanged(nameof(IsLocationItemSortedByName));
                }
            }
        }

        /// <summary>
        /// IsLocationItemSortedAscending property indicating whether the sorting of location items in the navigation bar should be ascending
        /// </summary>
        private bool _IsLocationItemSortedByDate = false;

        /// <summary>
        /// Binding property for the RadioMenuFlyoutItem SidebarSortByDate
        /// </summary>
        public bool IsLocationItemSortedByDate
        {
            get => SidebarPinnedModel.SidebarSortOption == SidebarSortOption.DateAdded;
            set
            {
                if (value && SidebarPinnedModel.SidebarSortOption != SidebarSortOption.DateAdded)
                {
                    SidebarPinnedModel.SidebarSortOption = SidebarSortOption.DateAdded;
                }

                if (_IsLocationItemSortedByDate != value)
                {
                    _IsLocationItemSortedByDate = value;
                    NotifyPropertyChanged(nameof(IsLocationItemSortedByDate));
                }
            }
        }

        /// <summary>
        /// IsLocationItemSortedAscending property indicating whether the sorting of location items in the navigation bar should be ascending
        /// </summary>
        private bool _IsLocationItemSortedAscending;

        /// <summary>
        /// Binding property for the RadioMenuFlyoutItem IsLocationItemSortedAscending
        /// </summary>
        public bool IsLocationItemSortedAscending
        {
            get => SidebarPinnedModel.SidebarSortDirection == SortDirection.Ascending;
            set
            {
                if(value && SidebarPinnedModel.SidebarSortDirection != SortDirection.Ascending)
                {
                    SidebarPinnedModel.SidebarSortDirection = SortDirection.Ascending;
                }

                if (_IsLocationItemSortedAscending != value)
                {
                    _IsLocationItemSortedAscending = value;
                    NotifyPropertyChanged(nameof(IsLocationItemSortedAscending));
                }
            }
        }

        /// <summary>
        /// IsLocationItemSortedDescending property indicating whether the sorting of location items in the navigation bar should be descending
        /// </summary>
        private bool _IsLocationItemSortedDescending;

        /// <summary>
        /// Binding property for the RadioMenuFlyoutItem IsLocationItemSortedDescending
        /// </summary>
        public bool IsLocationItemSortedDescending
        {
            get => !IsLocationItemSortedAscending;
            set
            {
                if (value && SidebarPinnedModel.SidebarSortDirection != SortDirection.Descending)
                {
                    SidebarPinnedModel.SidebarSortDirection = SortDirection.Descending;
                }

                if (_IsLocationItemSortedDescending != value)
                {
                    _IsLocationItemSortedDescending = value;
                    NotifyPropertyChanged(nameof(IsLocationItemSortedDescending));
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
            string navigationPath; // path to navigate
            Type sourcePageType = null; // type of page to navigate

            if (args.InvokedItem == null)
            {
                return;
            }

            switch ((args.InvokedItemContainer.DataContext as INavigationControlItem).ItemType)
            {
                case NavigationControlItemType.Location:
                    {
                        var ItemPath = (args.InvokedItemContainer.DataContext as INavigationControlItem).Path; // Get the path of the invoked item

                        if (ItemPath.Equals("Home", StringComparison.OrdinalIgnoreCase)) // Home item
                        {
                            if (ItemPath.Equals(SelectedSidebarItem?.Path, StringComparison.OrdinalIgnoreCase)) return; // return if already selected

                            navigationPath = ResourceController.GetTranslation("NewTab");
                            sourcePageType = typeof(YourHome);
                        }
                        else // Any other item
                        {
                            navigationPath = args.InvokedItemContainer.Tag.ToString();
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
                        navigationPath = args.InvokedItemContainer.Tag.ToString();
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
                ShowOrderBy = false;
            }
            else
            {
                ShowUnpinItem = true;
                ShowOrderBy = true;
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
                ShowOrderBy = true;
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
            ShowOrderBy = false;
            ShowEmptyRecycleBin = false;
            ShowProperties = true;

            SideBarItemContextFlyout.ShowAt(sidebarItem, e.GetPosition(sidebarItem));

            App.rightClickedItem = sidebarItem.DataContext as DriveItem;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(Settings));

            return;
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
            args.Data.Properties.Add("sourceLocationItem", sender);
        }

        private void NavigationViewItem_DragEnter(object sender, DragEventArgs e)
        {
            VisualStateManager.GoToState(sender as Microsoft.UI.Xaml.Controls.NavigationViewItem, "DragEnter", false);
        }

        private void NavigationViewItem_DragLeave(object sender, DragEventArgs e)
        {
            VisualStateManager.GoToState(sender as Microsoft.UI.Xaml.Controls.NavigationViewItem, "DragLeave", false);
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
                        e.DragUIOverride.Caption = string.Format(ResourceController.GetTranslation("MoveToFolderCaptionText"), locationItem.Text);
                    }
                    else
                    {
                        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
                        e.DragUIOverride.Caption = string.Format(ResourceController.GetTranslation("CopyToFolderCaptionText"), locationItem.Text);
                    }
                }
                deferral.Complete();
            }
            else if ((e.DataView.Properties["sourceLocationItem"] as Microsoft.UI.Xaml.Controls.NavigationViewItem).DataContext is LocationItem sourceLocationItem)
            {
                // else if the drag over event is called over a location item

                var deferral = e.GetDeferral();
                e.Handled = true;

                // If the location item is the same as the original dragged item or the default location (home button), the dragging should be disabled
                if (sourceLocationItem.Equals(locationItem) || locationItem.IsDefaultLocation == true)
                {
                    e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
                    e.DragUIOverride.IsCaptionVisible = false;
                }
                else
                {
                    e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
                    e.DragUIOverride.IsCaptionVisible = true;
                    e.DragUIOverride.Caption = string.Format("Pin to Quick access");
                }

                e.Data = new DataPackage();
                deferral.Complete();
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

                var deferral = e.GetDeferral();
                e.Handled = true;

                // Get the new index for the dropped item
                var newIndex = App.SidebarPinnedController.Model.IndexOfItem(locationItem);
                SidebarPinnedModel.MoveItem(sourceLocationItem, newIndex);
                deferral.Complete();
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
                    e.DragUIOverride.Caption = string.Format(ResourceController.GetTranslation("MoveToFolderCaptionText"), driveItem.Text);
                }
                else
                {
                    e.AcceptedOperation = DataPackageOperation.Copy;
                    e.DragUIOverride.Caption = string.Format(ResourceController.GetTranslation("CopyToFolderCaptionText"), driveItem.Text);
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
                    ItemType = ResourceController.GetTranslation("FileFolderListItem"),
                    LoadFolderGlyph = true
                };
                await App.CurrentInstance.InteractionOperations.OpenPropertiesWindow(listedItem);
            }
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