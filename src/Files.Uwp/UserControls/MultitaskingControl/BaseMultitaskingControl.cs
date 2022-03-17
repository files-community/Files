using Files.Helpers;
using Files.Uwp.Helpers;
using Files.ViewModels;
using Files.Views;
using Microsoft.Toolkit.Uwp;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls.MultitaskingControl
{
    public class BaseMultitaskingControl : UserControl, IMultitaskingControl, INotifyPropertyChanged
    {
        private ObservableCollection<TabItem> _items;
        private static bool isRestoringClosedTab = false; // Avoid reopening two tabs

        private ITabItemContent _currentSelectedAppInstance;
        public ITabItemContent CurrentSelectedAppInstance
        {
            get => _currentSelectedAppInstance;
            internal set
            {
                if (value != _currentSelectedAppInstance)
                {
                    _currentSelectedAppInstance = value;
                    OnPropertyChanged("CurrentSelectedAppInstance");
                }
            }
        }

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

        public ObservableCollection<TabItem> Items
        {
            get { return (ObservableCollection<TabItem>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register("Items", typeof(ObservableCollection<TabItem>), typeof(BaseMultitaskingControl), new PropertyMetadata(null));

        // RecentlyClosedTabs is shared between all multitasking controls
        public static List<TabItemArguments[]> RecentlyClosedTabs { get; private set; } = new List<TabItemArguments[]>();

        public TabItem SelectedTabItem => Items.ElementAtOrDefault(App.MainViewModel.TabStripSelectedIndex);
        public TabView TabViewControl { get; private set; }

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
            if (TabViewControl.SelectedIndex >= 0 && TabViewControl.SelectedIndex < Items.Count)
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
            await AddNewTabByPathAsync(typeof(PaneHolderPage), "Home".GetLocalized());
        }

        public void MultitaskingControl_Loaded(object sender, RoutedEventArgs e)
        {
            CurrentInstanceChanged += MultitaskingControl_CurrentInstanceChanged;
            TabViewControl = ((TabView)this.FindName("TabControl"));
            CurrentSelectedAppInstance = GetCurrentSelectedTabInstance();
        }

        public ITabItemContent GetCurrentSelectedTabInstance()
        {
            return SelectedTabItem?.Control?.TabItemContent;
        }

        public List<ITabItemContent> GetAllTabInstances()
        {
            return Items.Select(x => x.Control?.TabItemContent).ToList();
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
                    await AddNewTabByParam(item.InitialPageType, item.NavigationArg.ToString());
                }
                isRestoringClosedTab = false;
            }
        }

        public async Task AddNewTabByPathAsync(Type type, string path, int atIndex = -1)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = "Home".GetLocalized();
            }

            // Support drives launched through jump list by stripping away the question mark at the end.
            if (path.EndsWith("\\?"))
            {
                path = path.Remove(path.Length - 1);
            }

            TabItem tabItem = new TabItem()
            {
                Header = null,
                IconSource = null,
                Description = null
            };
            tabItem.Control.NavigationArguments = new TabItemArguments()
            {
                InitialPageType = type,
                NavigationArg = path
            };
            tabItem.RegisterForContentChanges();
            await MainPageViewModel.UpdateTabInfo(tabItem, path);
            var index = atIndex == -1 ? Items.Count : atIndex;
            Items.Insert(index, tabItem);
            App.MainViewModel.TabStripSelectedIndex = index;
        }

        public void AddNewTab(object sender, RoutedEventArgs args)
        {
            AddNewTab();
        }

        public async void AddNewTab()
        {
            await AddNewTabByPathAsync(typeof(PaneHolderPage), "Home".GetLocalized());
        }

        public async void DuplicateTabAtIndex(object sender, RoutedEventArgs e)
        {
            var tabItem = ((FrameworkElement)sender).DataContext as TabItem;
            var index = Items.IndexOf(tabItem);

            if (Items[index].TabItemArguments != null)
            {
                var tabArgs = Items[index].TabItemArguments;
                await AddNewTabByParam(tabArgs.InitialPageType, tabArgs.NavigationArg.ToString(), index + 1);
            }
            else
            {
                await AddNewTabByPathAsync(typeof(PaneHolderPage), "Home".GetLocalized());
            }
        }

        public async Task AddNewTabByParam(Type type, string tabViewItemArgs, int atIndex = -1)
        {
            Microsoft.UI.Xaml.Controls.FontIconSource fontIconSource = new Microsoft.UI.Xaml.Controls.FontIconSource();
            fontIconSource.FontFamily = App.MainViewModel.FontName;

            TabItem tabItem = new TabItem()
            {
                Header = null,
                IconSource = null,
                Description = null
            };
            tabItem.Control.NavigationArguments = new TabItemArguments()
            {
                InitialPageType = type,
                NavigationArg = tabViewItemArgs
            };
            tabItem.RegisterForContentChanges();
            await MainPageViewModel.UpdateTabInfo(tabItem, tabViewItemArgs);
            var index = atIndex == -1 ? Items.Count : atIndex;
            Items.Insert(index, tabItem);
            App.MainViewModel.TabStripSelectedIndex = index;
        }

        public async void MoveTabToNewWindow(object sender, RoutedEventArgs e)
        {
            await MultitaskingTabsHelpers.MoveTabToNewWindow(((FrameworkElement)sender).DataContext as TabItem, this);
        }

        public async void CloseTab(TabItem tabItem)
        {
            if (Items.Count == 1)
            {
                object currentWindow = WindowManagementHelpers.GetWindowFromUIContext(this.UIContext);
                if (currentWindow is AppWindow appWindow)
                {
                    await appWindow.CloseAsync();
                }
                else
                {
                    await ApplicationView.GetForCurrentView().TryConsolidateAsync();
                }
            }
            else if (Items.Count > 1)
            {
                Items.Remove(tabItem);
                tabItem?.Unload(); // Dispose and save tab arguments
                RecentlyClosedTabs.Add(new TabItemArguments[] {
                    tabItem.TabItemArguments
                });
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
    }
}
