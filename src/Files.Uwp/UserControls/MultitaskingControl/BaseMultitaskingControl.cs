using Files.Helpers;
using Files.ViewModels;
using Microsoft.Toolkit.Uwp;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.Uwp.UserControls.MultitaskingControl
{
    public class BaseMultitaskingControl : UserControl, IMultitaskingControl, INotifyPropertyChanged
    {
        private static bool isRestoringClosedTab = false; // Avoid reopening two tabs

        protected ITabItemContent CurrentSelectedAppInstance;

        protected TabView TabView;

        public const string TabDropHandledIdentifier = "FilesTabViewItemDropHandled";

        public const string TabPathIdentifier = "FilesTabViewItemPath";

        public event EventHandler<CurrentInstanceChangedEventArgs> CurrentInstanceChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual DependencyObject ContainerFromItem(ITabItem item)
        {
            return null;
        }

        public void SelectionChanged() => TabStrip_SelectionChanged(null, null);

        public BaseMultitaskingControl()
        {
            Loaded += MultitaskingControl_Loaded;
        }

        public ObservableCollection<TabItem> Items => MainPageViewModel.AppInstances;

        // RecentlyClosedTabs is shared between all multitasking controls
        public static List<TabItemArguments[]> RecentlyClosedTabs { get; private set; } = new List<TabItemArguments[]>();

        private void MultitaskingControl_CurrentInstanceChanged(object sender, CurrentInstanceChangedEventArgs e)
        {
            foreach (ITabItemContent instance in e.PageInstances)
            {
                if (instance != null)
                {
                    instance.IsCurrentInstance = instance == e.CurrentInstance;
                }
            }
        }

        protected void TabStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (App.MainViewModel.TabStripSelectedIndex >= 0 && App.MainViewModel.TabStripSelectedIndex < Items.Count)
            {
                CurrentSelectedAppInstance = GetCurrentSelectedTabInstance();

                if (CurrentSelectedAppInstance != null)
                {
                    CurrentInstanceChanged?.Invoke(this, new CurrentInstanceChangedEventArgs()
                    {
                        CurrentInstance = CurrentSelectedAppInstance,
                        PageInstances = GetAllTabInstances()
                    });
                }
            }
        }

        protected void OnCurrentInstanceChanged(CurrentInstanceChangedEventArgs args)
        {
            CurrentInstanceChanged?.Invoke(this, args);
        }

        protected void TabStrip_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            CloseTab(args.Item as TabItem, true);
        }

        protected async void TabView_AddTabButtonClick(TabView sender, object args)
        {
            await MainPageViewModel.AddNewTabAsync();
        }

        public void MultitaskingControl_Loaded(object sender, RoutedEventArgs e)
        {
            CurrentInstanceChanged += MultitaskingControl_CurrentInstanceChanged;
        }

        public ITabItemContent GetCurrentSelectedTabInstance()
        {
            return MainPageViewModel.AppInstances[App.MainViewModel.TabStripSelectedIndex].Control?.TabItemContent;
        }

        public List<ITabItemContent> GetAllTabInstances()
        {
            return MainPageViewModel.AppInstances.Select(x => x.Control?.TabItemContent).ToList();
        }

        public void CloseTabsToTheLeft(object sender, RoutedEventArgs e)
            => MultitaskingTabsHelpers.CloseTabsToTheLeft(((FrameworkElement)sender).DataContext as TabItem, this);

        public void CloseTabsToTheRight(object sender, RoutedEventArgs e)
            => MultitaskingTabsHelpers.CloseTabsToTheRight(((FrameworkElement)sender).DataContext as TabItem, this);

        public void CloseOtherTabs(object sender, RoutedEventArgs e)
            => MultitaskingTabsHelpers.CloseOtherTabs(((FrameworkElement)sender).DataContext as TabItem, this);

        public async void ReopenClosedTab(object sender, RoutedEventArgs e)
        {
            if (!isRestoringClosedTab && RecentlyClosedTabs.Any())
            {
                isRestoringClosedTab = true;
                var lastTab = RecentlyClosedTabs.Last();
                RecentlyClosedTabs.Remove(lastTab);
                foreach (var item in lastTab)
                {
                    await MainPageViewModel.AddNewTabByParam(item.InitialPageType, item.NavigationArg);
                }
                isRestoringClosedTab = false;
            }
        }

        public async void MoveTabToNewWindow(object sender, RoutedEventArgs e)
        {
            await MultitaskingTabsHelpers.MoveTabToNewWindow(((FrameworkElement)sender).DataContext as TabItem, this);
        }

        public void CloseTab(TabItem tabItem, bool cancelOperationOnPinnedTabs = false)
        {
            if (Items.Count == 1)
            {
                App.CloseApp();
            }
            else if (Items.Count > 1)
            {
                if (!tabItem.IsPinned ||
                    (tabItem.IsPinned && !cancelOperationOnPinnedTabs))
                {
                    Items.Remove(tabItem);
                    tabItem?.Unload(); // Dispose and save tab arguments
                    RecentlyClosedTabs.Add(new TabItemArguments[] {
                        tabItem.TabItemArguments
                    });
                }
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void SetLoadingIndicatorStatus(ITabItem item, bool loading)
        {
            var tabItem = ContainerFromItem(item) as Control;
            if (tabItem is null)
            {
                return;
            }

            if (loading)
            {
                VisualStateManager.GoToState(tabItem, "Loading", false);
            }
            else
            {
                VisualStateManager.GoToState(tabItem, "NotLoading", false);
            }
        }

        protected void TabStrip_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
        {
            var tabItem = args.Item as TabItem;
            if (tabItem != null)
            {
                var tabData = SelectiveSerialization.ToString(tabItem);
                args.Data.Properties.Add(TabPathIdentifier, tabData);
            }

            args.Data.RequestedOperation = DataPackageOperation.Move;
        }

        protected void TabStrip_TabStripDragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.ContainsKey(TabPathIdentifier))
            {
                TabView.CanReorderTabs = true;
                e.AcceptedOperation = DataPackageOperation.Move;
                e.DragUIOverride.Caption = "TabStripDragAndDropUIOverrideCaption".GetLocalized();
                e.DragUIOverride.IsCaptionVisible = true;
                e.DragUIOverride.IsGlyphVisible = false;
            }
            else
            {
                TabView.CanReorderTabs = false;
            }
        }

        protected void TabStrip_DragLeave(object sender, DragEventArgs e)
        {
            TabView.CanReorderTabs = true;
        }

        protected async void TabStrip_TabStripDrop(object sender, DragEventArgs e)
        {
            TabView.CanReorderTabs = true;
            if (!(sender is TabView tabStrip))
            {
                return;
            }

            if (!e.DataView.Properties.TryGetValue(TabPathIdentifier, out object tabViewItemPathObj) || !(tabViewItemPathObj is string tabItemString))
            {
                return;
            }

            var droppingTabIndex = tabStrip.TabItems.Count;

            for (int i = 0; i < tabStrip.TabItems.Count; i++)
            {
                var item = tabStrip.ContainerFromIndex(i) as TabViewItem;

                if (e.GetPosition(item).Y - item.ActualHeight < 0)
                {
                    droppingTabIndex = i;
                    break;
                }
            }

            var droppingTabItem = new TabItem(false);
            SelectiveSerialization.FromString(ref droppingTabItem, tabItemString);

            var lastPinnedTabIndex = MainPageViewModel.GetLastPinnedTabIndex();
            if (lastPinnedTabIndex != -1)
            {
                if (droppingTabItem.IsPinned)
                {
                    if (droppingTabIndex > lastPinnedTabIndex + 1)
                    {
                        droppingTabIndex = lastPinnedTabIndex + 1;
                    }
                }
                else
                {
                    if (droppingTabIndex <= lastPinnedTabIndex)
                    {
                        droppingTabIndex = lastPinnedTabIndex + 1;
                    }
                }
            }

            ApplicationData.Current.LocalSettings.Values[TabDropHandledIdentifier] = true;
            await MainPageViewModel.AddNewTabByParam(tabItemString, droppingTabIndex);
        }

        protected void TabStrip_TabDragCompleted(TabView sender, TabViewTabDragCompletedEventArgs args)
        {
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(TabDropHandledIdentifier) &&
                (bool)ApplicationData.Current.LocalSettings.Values[TabDropHandledIdentifier])
            {
                CloseTab(args.Item as TabItem);
            }
            else
            {
                TabView.SelectedItem = args.Tab;
                MainPageViewModel.RearrangeTabs();
            }

            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(TabDropHandledIdentifier))
            {
                ApplicationData.Current.LocalSettings.Values.Remove(TabDropHandledIdentifier);
            }
        }

        protected async void TabStrip_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
        {
            if (sender.TabItems.Count == 1)
            {
                return;
            }

            var indexOfTabViewItem = sender.TabItems.IndexOf(args.Item);
            var tabViewItemArgs = (args.Item as TabItem).TabItemArguments;
            var selectedTabViewItemIndex = sender.SelectedIndex;
            Items.Remove(args.Item as TabItem);
            if (!await NavigationHelpers.OpenTabInNewWindowAsync(tabViewItemArgs.Serialize()))
            {
                Items.Insert(indexOfTabViewItem, args.Item as TabItem);
                sender.SelectedIndex = selectedTabViewItemIndex;
            }
            else
            {
                (args.Item as TabItem)?.Unload(); // Dispose tab arguments
            }
        }
    }
}
