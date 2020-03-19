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
        }

        private void Sidebar_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.HomeItems.isEnabled = false;
            (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.ShareItems.isEnabled = false;

            if (args.InvokedItem == null)
            {
                return;
            }

            switch ((args.InvokedItemContainer.DataContext as INavigationControlItem).ItemType)
            {
                case NavigationControlItemType.Location:
                    {
                        switch (args.InvokedItem.ToString())
                        {
                            case "Home":
                                App.CurrentInstance.ContentFrame.Navigate(typeof(YourHome), "New tab", new SuppressNavigationTransitionInfo());
                                App.CurrentInstance.NavigationControl.PathControlDisplayText = "New tab";
                                (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.HomeItems.isEnabled = false;
                                (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.ShareItems.isEnabled = false;

                                (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = false;
                                break;
                            default:
                                App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), args.InvokedItemContainer.Tag.ToString(), new SuppressNavigationTransitionInfo());
                                App.CurrentInstance.NavigationControl.PathControlDisplayText = args.InvokedItem.ToString();
                                (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.HomeItems.isEnabled = false;
                                (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.ShareItems.isEnabled = false;

                                (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;
                                break;
                        }
                        break;
                    }
                case NavigationControlItemType.Drive:
                    {
                        var clickedItem = args.InvokedItemContainer;

                        App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), clickedItem.Tag.ToString(), new SuppressNavigationTransitionInfo());
                        App.CurrentInstance.NavigationControl.PathControlDisplayText = clickedItem.Tag.ToString();
                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;

                        break;
                    }

                case NavigationControlItemType.OneDrive:
                    {
                        var clickedItem = args.InvokedItemContainer;

                        if (clickedItem.Tag.ToString() == "LocalDisk")
                        {
                            App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), @"C:\", new SuppressNavigationTransitionInfo());
                            App.CurrentInstance.NavigationControl.PathControlDisplayText = @"Local Disk (C:\)";
                            (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;
                        }
                        else if (clickedItem.Tag.ToString() == "OneDrive")
                        {
                            App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), App.AppSettings.OneDrivePath, new SuppressNavigationTransitionInfo());
                            App.CurrentInstance.NavigationControl.PathControlDisplayText = "OneDrive";
                            (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;
                        }
                        else
                        {
                            App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), clickedItem.Tag.ToString(), new SuppressNavigationTransitionInfo());
                            App.CurrentInstance.NavigationControl.PathControlDisplayText = clickedItem.Tag.ToString();
                            (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;
                        }

                        break;
                    }

                case NavigationControlItemType.LinuxDistro:
                    {
                        var clickedItem = args.InvokedItemContainer;

                        App.CurrentInstance.ContentFrame.Navigate(typeof(GenericFileBrowser), clickedItem.Tag.ToString(), new SuppressNavigationTransitionInfo());
                        App.CurrentInstance.NavigationControl.PathControlDisplayText = clickedItem.Tag.ToString();
                        (App.CurrentInstance.OperationsControl as RibbonArea).RibbonViewModel.LayoutItems.isEnabled = true;

                        break;
                    }
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
