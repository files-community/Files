using Files.Helpers;
using Files.ViewModels;
using Microsoft.Toolkit.Uwp;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls.MultitaskingControl
{
    public sealed partial class HorizontalMultitaskingControl : BaseMultitaskingControl
    {
        private readonly DispatcherTimer tabHoverTimer = new DispatcherTimer();
        private TabViewItem hoveredTabViewItem = null;

        private SettingsViewModel AppSettings => App.AppSettings;

        public HorizontalMultitaskingControl()
        {
            InitializeComponent();

            TabView = HorizontalTabView;

            tabHoverTimer.Interval = TimeSpan.FromMilliseconds(500);
            tabHoverTimer.Tick += TabHoverSelected;
        }

        private void HorizontalTabView_TabItemsChanged(TabView sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
        {
            if (args.CollectionChange == Windows.Foundation.Collections.CollectionChange.ItemRemoved)
            {
                App.MainViewModel.TabStripSelectedIndex = Items.IndexOf(TabView.SelectedItem as TabItem);
            }

            if (App.MainViewModel.TabStripSelectedIndex >= 0 && App.MainViewModel.TabStripSelectedIndex < Items.Count)
            {
                CurrentSelectedAppInstance = GetCurrentSelectedTabInstance();

                if (CurrentSelectedAppInstance != null)
                {
                    OnCurrentInstanceChanged(new CurrentInstanceChangedEventArgs()
                    {
                        CurrentInstance = CurrentSelectedAppInstance,
                        PageInstances = GetAllTabInstances()
                    });
                }
            }

            TabView.SelectedIndex = App.MainViewModel.TabStripSelectedIndex;
        }

        private async void TabViewItem_Drop(object sender, DragEventArgs e)
        {
            await ((sender as TabViewItem).DataContext as TabItem).Control.TabItemContent.TabItemDrop(sender, e);
            TabView.CanReorderTabs = true;
            tabHoverTimer.Stop();
        }

        private async void TabViewItem_DragEnter(object sender, DragEventArgs e)
        {
            await ((sender as TabViewItem).DataContext as TabItem).Control.TabItemContent.TabItemDragOver(sender, e);
            if (e.AcceptedOperation != DataPackageOperation.None)
            {
                TabView.CanReorderTabs = false;
                tabHoverTimer.Start();
                hoveredTabViewItem = sender as TabViewItem;
            }
        }

        private void TabViewItem_DragLeave(object sender, DragEventArgs e)
        {
            tabHoverTimer.Stop();
            hoveredTabViewItem = null;
        }

        // Select tab that is hovered over for a certain duration
        private void TabHoverSelected(object sender, object e)
        {
            tabHoverTimer.Stop();
            if (hoveredTabViewItem != null)
            {
                App.MainViewModel.TabStripSelectedIndex = Items.IndexOf(hoveredTabViewItem.DataContext as TabItem);
            }
        }

        private void TabItemContextMenu_Opening(object sender, object e)
        {
            MenuItemMoveTabToNewWindow.IsEnabled = Items.Count > 1;
            MenuItemReopenClosedTab.IsEnabled = RecentlyClosedTabs.Any();
        }

        private void MenuItemCloseTabsToTheLeft_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            TabItem tabItem = args.NewValue as TabItem;
            MenuItemCloseTabsToTheLeft.IsEnabled = MainPageViewModel.AppInstances.IndexOf(tabItem) > 0;
        }

        private void MenuItemCloseTabsToTheRight_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            TabItem tabItem = args.NewValue as TabItem;
            MenuItemCloseTabsToTheRight.IsEnabled = MainPageViewModel.AppInstances.IndexOf(tabItem) < MainPageViewModel.AppInstances.Count - 1;
        }

        private void MenuItemCloseOtherTabs_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            TabItem tabItem = args.NewValue as TabItem;
            MenuItemCloseOtherTabs.IsEnabled = MainPageViewModel.AppInstances.Count > 1;
        }

        public override DependencyObject ContainerFromItem(ITabItem item) => TabView.ContainerFromItem(item);

        public UIElement ActionsControl
        {
            get { return (UIElement)GetValue(ActionsControlProperty); }
            set { SetValue(ActionsControlProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ActionsControl.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActionsControlProperty =
            DependencyProperty.Register("ActionsControl", typeof(UIElement), typeof(HorizontalMultitaskingControl), new PropertyMetadata(null));

        public Visibility TabStripVisibility
        {
            get { return (Visibility)GetValue(TabStripVisibilityProperty); }
            set { SetValue(TabStripVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TabStripVisibility.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TabStripVisibilityProperty =
            DependencyProperty.Register("TabStripVisibility", typeof(Visibility), typeof(HorizontalMultitaskingControl), new PropertyMetadata(Visibility.Visible));
    }
}
