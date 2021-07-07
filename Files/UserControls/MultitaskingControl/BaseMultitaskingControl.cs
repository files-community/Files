﻿using Files.Helpers;
using Files.ViewModels;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls.MultitaskingControl
{
    public class BaseMultitaskingControl : UserControl, IMultitaskingControl, INotifyPropertyChanged
    {
        private static bool isRestoringClosedTab = false; // Avoid reopening two tabs

        protected ITabItemContent CurrentSelectedAppInstance;

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
        public static List<ITabItem> RecentlyClosedTabs { get; private set; } = new List<ITabItem>();

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
            CloseTab(args.Item as TabItem);
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

        public void CloseTabsToTheRight(object sender, RoutedEventArgs e)
        {
            MultitaskingTabsHelpers.CloseTabsToTheRight(((FrameworkElement)sender).DataContext as TabItem, this);
        }

        public async void ReopenClosedTab(object sender, RoutedEventArgs e)
        {
            if (!isRestoringClosedTab && RecentlyClosedTabs.Any())
            {
                isRestoringClosedTab = true;
                ITabItem lastTab = RecentlyClosedTabs.Last();
                RecentlyClosedTabs.Remove(lastTab);
                await MainPageViewModel.AddNewTabByParam(lastTab.TabItemArguments.InitialPageType, lastTab.TabItemArguments.NavigationArg);
                isRestoringClosedTab = false;
            }
        }

        public async void MoveTabToNewWindow(object sender, RoutedEventArgs e)
        {
            await MultitaskingTabsHelpers.MoveTabToNewWindow(((FrameworkElement)sender).DataContext as TabItem, this);
        }

        public void CloseTab(TabItem tabItem)
        {
            if (Items.Count == 1)
            {
                App.CloseApp();
            }
            else if (Items.Count > 1)
            {
                Items.Remove(tabItem);
                tabItem?.Unload(); // Dispose and save tab arguments
                RecentlyClosedTabs.Add((ITabItem)tabItem);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void SetLoadingIndicatorStatus(ITabItem item, bool loading)
        {
            var tabItem = ContainerFromItem(item) as Control;
            if(tabItem is null)
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
    }
}