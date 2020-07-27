using Files.Filesystem;
using Files.Interacts;
using Files.View_Models;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;

namespace Files.Controls
{
    public sealed partial class SidebarControl : UserControl, INotifyPropertyChanged
    {
        public SettingsViewModel AppSettings => App.AppSettings;

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
                    NotifyPropertyChanged("SelectedSidebarItem");
                }
            }
        }

        private bool _ShowUnpinItem;

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
                    NotifyPropertyChanged("ShowUnpinItem");
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
                    NotifyPropertyChanged("ShowEmptyRecycleBin");
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
                            if (ItemPath.Equals(SelectedSidebarItem.Path, StringComparison.OrdinalIgnoreCase)) return; // return if already selected

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
                ShowEmptyRecycleBin = true;
                ShowUnpinItem = true;
            }
            else
            {
                ShowEmptyRecycleBin = false;
            }

            SideBarItemContextFlyout.ShowAt(sidebarItem, e.GetPosition(sidebarItem));
            App.rightClickedItem = item;
        }

        private void NavigationViewDriveItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            Microsoft.UI.Xaml.Controls.NavigationViewItem sidebarItem = (Microsoft.UI.Xaml.Controls.NavigationViewItem)sender;

            ShowUnpinItem = false;
            ShowEmptyRecycleBin = false;

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

        private async void NavigationViewLocationItem_DragOver(object sender, DragEventArgs e)
        {
            if (!((sender as Microsoft.UI.Xaml.Controls.NavigationViewItem).DataContext is LocationItem locationItem)) return;

            var deferral = e.GetDeferral();
            e.Handled = true;
            var storageItems = await e.DataView.GetStorageItemsAsync();

            if (storageItems.Count == 0 ||
                locationItem.IsDefaultLocation ||
                locationItem.Path.Equals(App.AppSettings.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
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

        private async void NavigationViewLocationItem_Drop(object sender, DragEventArgs e)
        {
            if (!((sender as Microsoft.UI.Xaml.Controls.NavigationViewItem).DataContext is LocationItem locationItem)) return;

            var deferral = e.GetDeferral();
            await App.CurrentInstance.InteractionOperations.PasteItems(e.DataView, locationItem.Path, e.AcceptedOperation);
            deferral.Complete();
        }

        private async void NavigationViewDriveItem_DragOver(object sender, DragEventArgs e)
        {
            if (!((sender as Microsoft.UI.Xaml.Controls.NavigationViewItem).DataContext is DriveItem driveItem)) return;

            var deferral = e.GetDeferral();
            e.Handled = true;
            var storageItems = await e.DataView.GetStorageItemsAsync();

            if (storageItems.Count == 0 || "Unknown".Equals(driveItem.SpaceText, StringComparison.OrdinalIgnoreCase))
            {
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
            }
            else
            {
                e.DragUIOverride.IsCaptionVisible = true;
                if (storageItems.AreItemsInSameDrive(driveItem.Path))
                {
                    e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
                    e.DragUIOverride.Caption = string.Format(ResourceController.GetTranslation("MoveToFolderCaptionText"), driveItem.Text);
                }
                else
                {
                    e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
                    e.DragUIOverride.Caption = string.Format(ResourceController.GetTranslation("CopyToFolderCaptionText"), driveItem.Text);
                }
            }
            deferral.Complete();
        }

        private async void NavigationViewDriveItem_Drop(object sender, DragEventArgs e)
        {
            if (!((sender as Microsoft.UI.Xaml.Controls.NavigationViewItem).DataContext is DriveItem driveItem)) return;

            var deferral = e.GetDeferral();
            await App.CurrentInstance.InteractionOperations.PasteItems(e.DataView, driveItem.Path, e.AcceptedOperation);
            deferral.Complete();
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
