using Files.Filesystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;



namespace Files.Controls
{
    public sealed partial class Sidebar : UserControl
    {
        public Sidebar()
        {
            this.InitializeComponent();
            SidebarNavView.SelectedItem = App.sideBarItems[0];
        }

        private void Sidebar_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.HomeItems.isEnabled = false;
            (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.ShareItems.isEnabled = false;

            if ((args.InvokedItemContainer.DataContext as INavigationControlItem).ItemType == NavigationControlItemType.Location)
            {
                switch (args.InvokedItem.ToString())
                {
                    case "Home":
                        App.CurrentInstance.ContentFrame.Navigate(typeof(YourHome), "New tab", new SuppressNavigationTransitionInfo());
                        App.CurrentInstance.PathControlDisplayText = "New tab";
                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.HomeItems.isEnabled = false;
                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.ShareItems.isEnabled = false;

                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = false;
                        break;
                    case "Desktop":
                        App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.DesktopPath, new SuppressNavigationTransitionInfo());
                        App.CurrentInstance.PathControlDisplayText = "Desktop";
                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.HomeItems.isEnabled = false;
                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.ShareItems.isEnabled = false;

                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;
                        break;
                    case "Downloads":
                        App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.DownloadsPath, new SuppressNavigationTransitionInfo());
                        App.CurrentInstance.PathControlDisplayText = "Downloads";
                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.HomeItems.isEnabled = false;
                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.ShareItems.isEnabled = false;

                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;
                        break;
                    case "Documents":
                        App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.DocumentsPath, new SuppressNavigationTransitionInfo());
                        App.CurrentInstance.PathControlDisplayText = "Documents";
                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.HomeItems.isEnabled = false;
                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.ShareItems.isEnabled = false;

                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;
                        break;
                    case "Pictures":
                        App.CurrentInstance.ContentFrame.Navigate(typeof(PhotoAlbum), App.AppSettings.PicturesPath, new SuppressNavigationTransitionInfo());
                        App.CurrentInstance.PathControlDisplayText = "Pictures";
                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.HomeItems.isEnabled = false;
                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.ShareItems.isEnabled = false;

                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;
                        break;
                    case "Music":
                        App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.MusicPath, new SuppressNavigationTransitionInfo());
                        App.CurrentInstance.PathControlDisplayText = "Music";
                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.HomeItems.isEnabled = false;
                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.ShareItems.isEnabled = false;

                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;
                        break;
                    case "Videos":
                        App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.VideosPath, new SuppressNavigationTransitionInfo());
                        App.CurrentInstance.PathControlDisplayText = "Videos";
                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.HomeItems.isEnabled = false;
                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.ShareItems.isEnabled = false;

                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;
                        break;
                    default:
                        App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), args.InvokedItemContainer.Tag.ToString(), new SuppressNavigationTransitionInfo());
                        App.CurrentInstance.PathControlDisplayText = args.InvokedItem.ToString();
                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.HomeItems.isEnabled = false;
                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.ShareItems.isEnabled = false;

                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;
                        break;
                }
            }
            else if ((args.InvokedItemContainer.DataContext as INavigationControlItem).ItemType == NavigationControlItemType.Drive)
            {
                var clickedItem = args.InvokedItemContainer;

                if (clickedItem.Tag.ToString() == "LocalDisk")
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), @"C:\", new SuppressNavigationTransitionInfo());
                    App.CurrentInstance.PathControlDisplayText = @"Local Disk (C:\)";
                    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;
                }
                else if (clickedItem.Tag.ToString() == "OneDrive")
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.OneDrivePath, new SuppressNavigationTransitionInfo());
                    App.CurrentInstance.PathControlDisplayText = "OneDrive";
                    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;
                }
                else
                {
                    App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), clickedItem.Tag.ToString(), new SuppressNavigationTransitionInfo());
                    App.CurrentInstance.PathControlDisplayText = clickedItem.Tag.ToString();
                    (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;
                }
            }
            else if ((args.InvokedItemContainer.DataContext as INavigationControlItem).ItemType == NavigationControlItemType.LinuxDistro)
            {
                var clickedItem = args.InvokedItemContainer;

                App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), clickedItem.Tag.ToString(), new SuppressNavigationTransitionInfo());
                App.CurrentInstance.PathControlDisplayText = clickedItem.Tag.ToString();
                (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;

            }
        }

        private void NavigationViewItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            Microsoft.UI.Xaml.Controls.NavigationViewItem sidebarItem = (Microsoft.UI.Xaml.Controls.NavigationViewItem)sender;
            var item = ((FrameworkElement)e.OriginalSource).DataContext as LocationItem;
            if (!item.IsDefaultLocation)
            {
                SideBarItemContextFlyout.ShowAt(sidebarItem, e.GetPosition(sidebarItem));
                App.rightClickedItem = item;
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
            if(item != null && item is INavigationControlItem)
            {
                INavigationControlItem navControlItem = item as INavigationControlItem;
                switch (navControlItem.ItemType)
                {
                    case NavigationControlItemType.Location:
                        return LocationNavItemTemplate;
                    case NavigationControlItemType.Drive:
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
