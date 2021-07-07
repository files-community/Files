﻿using Files.Helpers;
using Files.ViewModels;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI;
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
            tabHoverTimer.Interval = TimeSpan.FromMilliseconds(500);
            tabHoverTimer.Tick += TabHoverSelected;
        }

        private void HorizontalTabView_TabItemsChanged(TabView sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
        {
            if (args.CollectionChange == Windows.Foundation.Collections.CollectionChange.ItemRemoved)
            {
                App.MainViewModel.TabStripSelectedIndex = Items.IndexOf(HorizontalTabView.SelectedItem as TabItem);
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
        }

        private async void TabViewItem_Drop(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = await ((sender as TabViewItem).DataContext as TabItem).Control.TabItemContent.TabItemDrop(sender, e);
            HorizontalTabView.CanReorderTabs = true;
            tabHoverTimer.Stop();
        }

        private void TabViewItem_DragEnter(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = ((sender as TabViewItem).DataContext as TabItem).Control.TabItemContent.TabItemDragOver(sender, e);
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
                App.MainViewModel.TabStripSelectedIndex = Items.IndexOf(hoveredTabViewItem.DataContext as TabItem);
            }
        }

        private void TabStrip_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
        {
            var tabViewItemArgs = (args.Item as TabItem).TabItemArguments;
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

            for (int i = 0; i < tabStrip.TabItems.Count; i++)
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
                CloseTab(args.Item as TabItem);
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
            if (sender.TabItems.Count == 1)
            {
                return;
            }

            var indexOfTabViewItem = sender.TabItems.IndexOf(args.Tab);
            var tabViewItemArgs = (args.Item as TabItem).TabItemArguments;
            var selectedTabViewItemIndex = sender.SelectedIndex;
            CloseTab(args.Item as TabItem);
            if (!await NavigationHelpers.OpenTabInNewWindowAsync(tabViewItemArgs.Serialize()))
            {
                sender.TabItems.Insert(indexOfTabViewItem, args.Tab);
                sender.SelectedIndex = selectedTabViewItemIndex;
            }
        }

        private void TabItemContextMenu_Opening(object sender, object e)
        {
            MenuItemMoveTabToNewWindow.IsEnabled = Items.Count > 1;
            MenuItemReopenClosedTab.IsEnabled = RecentlyClosedTabs.Any();
        }

        private void MenuItemCloseTabsToTheRight_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            TabItem tabItem = args.NewValue as TabItem;

            if (MainPageViewModel.AppInstances.IndexOf(tabItem) == MainPageViewModel.AppInstances.Count - 1)
            {
                MenuItemCloseTabsToTheRight.IsEnabled = false;
            }
            else
            {
                MenuItemCloseTabsToTheRight.IsEnabled = true;
            }
        }

        public override DependencyObject ContainerFromItem(ITabItem item) => HorizontalTabView.ContainerFromItem(item);
    }
}