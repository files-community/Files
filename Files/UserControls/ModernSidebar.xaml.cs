using Files.Filesystem;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Files.Controls
{
    public sealed partial class ModernSidebar : UserControl, INotifyPropertyChanged
    {
        public ModernSidebar()
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

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Sidebar_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            //(App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.HomeItems.isEnabled = false;
            //(App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.ShareItems.isEnabled = false;
            string NavigationPath = ""; // path to navigate

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
                            App.CurrentInstance.ContentFrame.Navigate(typeof(YourHome), "New tab", new SuppressNavigationTransitionInfo());

                            return; // cancel so it doesn't try to Navigate to a path
                        }
                        else // Any other item
                        {
                            NavigationPath = args.InvokedItemContainer.Tag.ToString();
                        }

                        break;
                    }
                case NavigationControlItemType.OneDrive:
                    {
                        NavigationPath = App.AppSettings.OneDrivePath;
                        break;
                    }
                default:
                    {
                        var clickedItem = args.InvokedItemContainer;

                        NavigationPath = clickedItem.Tag.ToString();

                        App.CurrentInstance.NavigationToolbar.PathControlDisplayText = clickedItem.Tag.ToString();
                        //(App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;

                        break;
                    }
            }

            if (App.AppSettings.LayoutMode == 0) // List View
            {
                App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), NavigationPath, new SuppressNavigationTransitionInfo());
            }
            else
            {
                App.CurrentInstance.ContentFrame.Navigate(typeof(PhotoAlbum), NavigationPath, new SuppressNavigationTransitionInfo());
            }

            App.InteractionViewModel.IsPageTypeNotHome = true; // show controls that were hidden on the home page

            App.CurrentInstance.NavigationToolbar.PathControlDisplayText = App.CurrentInstance.ViewModel.WorkingDirectory;
        }

        private void NavigationViewItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
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

            SideBarItemContextFlyout.ShowAt(sidebarItem, e.GetPosition(sidebarItem));
            App.rightClickedItem = item;
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

        private void OpenInNewWindow_Click(object sender, RoutedEventArgs e)
        {
            App.CurrentInstance.InteractionOperations.OpenPathInNewWindow(App.rightClickedItem.Path.ToString());
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