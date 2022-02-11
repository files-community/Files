using Files.Backend.Helpers;
using Files.Backend.ViewModels.Shell.Multitasking;
using Files.Backend.ViewModels.Shell.Tabs;
using Microsoft.UI.Xaml.Controls;
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
using Windows.UI.Xaml.Navigation;


namespace Files.UserControls.MultitaskingControl
{
    public sealed partial class MultitaskingControl : UserControl
    {
        private const string TabDropHandledIdentifier = "FilesTabViewItemDropHandled";
        private const string TabPathIdentifier = "FilesTabViewItemPath";

        private TabViewItem hoveredTabViewItem = null;

        public MultitaskingControlViewModel ViewModel
        {
            get => (MultitaskingControlViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        // Using a DependencyProperty as the backing store for ViewModel.
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(MultitaskingControlViewModel), typeof(MultitaskingControl), new PropertyMetadata(null));

        public Visibility TabStripVisibility
        {
            get { return (Visibility)GetValue(TabStripVisibilityProperty); }
            set { SetValue(TabStripVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TabStripVisibility.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TabStripVisibilityProperty =
            DependencyProperty.Register("TabStripVisibility", typeof(Visibility), typeof(HorizontalMultitaskingControl), new PropertyMetadata(Visibility.Visible));


        private DispatcherTimer tabHoverTimer;

        public MultitaskingControl()
        {
            this.InitializeComponent();
            tabHoverTimer = new DispatcherTimer();
            tabHoverTimer.Interval = TimeSpan.FromMilliseconds(500);
            tabHoverTimer.Tick += TabHoverSelected;

        }

        private async void TabViewItem_Drop(object sender, DragEventArgs e)
        {
            throw new NotImplementedException();
            //await ((sender as TabViewItem).DataContext as TabItemViewModel).TabShell.ActiveLayoutViewModel.TabItemDrop(sender, e);
            HorizontalTabView.CanReorderTabs = true;
            tabHoverTimer.Stop();
        }

        private async void TabViewItem_DragEnter(object sender, DragEventArgs e)
        {
            throw new NotImplementedException();
            //await ((sender as TabViewItem).DataContext as TabItemViewModel).TabShell.ActiveLayoutViewModel.TabItemDragOver(sender, e);
            if (e.AcceptedOperation != DataPackageOperation.None)
            {
                HorizontalTabView.CanReorderTabs = false;
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
                ViewModel.SelectedItem = hoveredTabViewItem.DataContext as TabItemViewModel;
            }
        }

        private void TabStrip_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
        {
            var tabViewItemArgs = (args.Item as TabItemViewModel).TabItemArguments;
            args.Data.Properties.Add(TabPathIdentifier, tabViewItemArgs.Serialize());
            args.Data.RequestedOperation = DataPackageOperation.Move;
        }

        private void TabStrip_TabStripDragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.ContainsKey(TabPathIdentifier))
            {
                HorizontalTabView.CanReorderTabs = true;
                e.AcceptedOperation = DataPackageOperation.Move;
                e.DragUIOverride.Caption = "TabStripDragAndDropUIOverrideCaption".GetLocalized();
                e.DragUIOverride.IsCaptionVisible = true;
                e.DragUIOverride.IsGlyphVisible = false;
            }
            else
            {
                HorizontalTabView.CanReorderTabs = false;
            }
        }

        private void TabStrip_DragLeave(object sender, DragEventArgs e)
        {
            HorizontalTabView.CanReorderTabs = true;
        }

        private async void TabStrip_TabStripDrop(object sender, DragEventArgs e)
        {
            HorizontalTabView.CanReorderTabs = true;
            if (!(sender is TabView tabStrip))
            {
                return;
            }

            if (!e.DataView.Properties.TryGetValue(TabPathIdentifier, out object tabViewItemPathObj) || !(tabViewItemPathObj is string tabViewItemString))
            {
                return;
            }

            var index = -1;

            for (int i = 0; i < ViewModel.Tabs.Count; i++)
            {
                var item = tabStrip.ContainerFromIndex(i) as TabViewItem;

                if (e.GetPosition(item).Y - item.ActualHeight < 0)
                {
                    index = i;
                    break;
                }
            }

            var tabViewItemArgs = TabItemArguments.Deserialize(tabViewItemString);
            ApplicationData.Current.LocalSettings.Values[TabDropHandledIdentifier] = true;
            await MainPageViewModel.AddNewTabByParam(tabViewItemArgs.InitialPageType, tabViewItemArgs.NavigationArg, index);
        }

        private void TabStrip_TabDragCompleted(TabView sender, TabViewTabDragCompletedEventArgs args)
        {
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(TabDropHandledIdentifier) &&
                (bool)ApplicationData.Current.LocalSettings.Values[TabDropHandledIdentifier])
            {
                CloseTab(args.Item as TabItemViewModel);
            }
            else
            {
                HorizontalTabView.SelectedItem = args.Tab;
            }

            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(TabDropHandledIdentifier))
            {
                ApplicationData.Current.LocalSettings.Values.Remove(TabDropHandledIdentifier);
            }
        }

        private async void TabStrip_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
        {
            if (ViewModel.Tabs.Count == 1)
            {
                return;
            }

            var indexOfTabViewItem = ViewModel.Tabs.IndexOf((TabItemViewModel)args.Item);
            var selectedTabViewItemIndex = sender.SelectedIndex;
            ViewModel.Tabs.Remove(args.Item as TabItemViewModel);
            if (!await NavigationHelpers.OpenPathInNewWindowAsync( /* path goes here*/    ))
            {
                ViewModel.Tabs.Insert(indexOfTabViewItem, args.Item as TabItemViewModel);
                sender.SelectedIndex = selectedTabViewItemIndex;
            }
            else
            {
                ViewModel.CloseTab(args.Item as TabItemViewModel);
            }
        }

        private void TabItemContextMenu_Opening(object sender, object e)
        {
            MenuItemMoveTabToNewWindow.IsEnabled = ViewModel.Tabs.Count > 1;
            MenuItemReopenClosedTab.IsEnabled = RecentlyClosedTabs.Any();
        }

        private void MenuItemCloseTabsToTheRight_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            TabItemViewModel tabItem = args.NewValue as TabItemViewModel;

            if (ViewModel.Tabs.IndexOf(tabItem) == ViewModel.Tabs.Count - 1)
            {
                MenuItemCloseTabsToTheRight.IsEnabled = false;
            }
            else
            {
                MenuItemCloseTabsToTheRight.IsEnabled = true;
            }
        }
    }
}
