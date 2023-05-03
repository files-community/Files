// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.TabView
{
	public class BaseTabView : UserControl, ITabView
	{
		public static event EventHandler<ITabView>? OnLoaded;

		public static event PropertyChangedEventHandler? StaticPropertyChanged;

		// Avoid reopening two tabs
		private static bool _IsRestoringClosedTab;
		public static bool IsRestoringClosedTab
		{
			get => _IsRestoringClosedTab;
			private set
			{
				_IsRestoringClosedTab = value;

				StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(IsRestoringClosedTab)));
			}
		}

		protected readonly MainPageViewModel mainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();

		protected ITabItemContent CurrentSelectedAppInstance;

		public const string TabDropHandledIdentifier = "FilesTabViewItemDropHandled";

		public const string TabPathIdentifier = "FilesTabViewItemPath";

		public event EventHandler<CurrentInstanceChangedEventArgs> CurrentInstanceChanged;

		public virtual DependencyObject ContainerFromItem(ITabItem item)
		{
			return null;
		}

		public void SelectionChanged() => TabStrip_SelectionChanged(null, null);

		public BaseTabView()
		{
			Loaded += MultitaskingControl_Loaded;
		}

		public ObservableCollection<TabItem> Items => MainPageViewModel.AppInstances;

		// RecentlyClosedTabs is shared between all multitasking controls
		public static Stack<TabItemArguments[]> RecentlyClosedTabs { get; private set; } = new();

		public static void PushRecentTab(TabItemArguments[] tab)
		{
			RecentlyClosedTabs.Push(tab);
			StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(RecentlyClosedTabs)));
		}

		private void MultitaskingControl_CurrentInstanceChanged(object sender, CurrentInstanceChangedEventArgs e)
		{
			foreach (ITabItemContent instance in e.PageInstances)
			{
				if (instance is not null)
				{
					instance.IsCurrentInstance = instance == e.CurrentInstance;
				}
			}
		}

		protected void TabStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (App.AppModel.TabStripSelectedIndex >= 0 && App.AppModel.TabStripSelectedIndex < Items.Count)
			{
				CurrentSelectedAppInstance = GetCurrentSelectedTabInstance();

				if (CurrentSelectedAppInstance is not null)
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

		protected void TabStrip_TabCloseRequested(Microsoft.UI.Xaml.Controls.TabView sender, TabViewTabCloseRequestedEventArgs args)
		{
			CloseTab(args.Item as TabItem);
		}

		protected async void TabView_AddTabButtonClick(Microsoft.UI.Xaml.Controls.TabView sender, object args)
		{
			await mainPageViewModel.AddNewTabAsync();
		}

		public void MultitaskingControl_Loaded(object sender, RoutedEventArgs e)
		{
			CurrentInstanceChanged += MultitaskingControl_CurrentInstanceChanged;
			OnLoaded?.Invoke(null, this);
		}

		public ITabItemContent GetCurrentSelectedTabInstance()
		{
			return MainPageViewModel.AppInstances[App.AppModel.TabStripSelectedIndex].Control?.TabItemContent;
		}

		public List<ITabItemContent> GetAllTabInstances()
		{
			return MainPageViewModel.AppInstances.Select(x => x.Control?.TabItemContent).ToList();
		}

		public async Task ReopenClosedTab()
		{
			if (!IsRestoringClosedTab && RecentlyClosedTabs.Count > 0)
			{
				IsRestoringClosedTab = true;
				var lastTab = RecentlyClosedTabs.Pop();
				foreach (var item in lastTab)
					await mainPageViewModel.AddNewTabByParam(item.InitialPageType, item.NavigationArg);

				IsRestoringClosedTab = false;
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
				RecentlyClosedTabs.Push(new TabItemArguments[] {
					tabItem.TabItemArguments
				});
			}
		}

		public void SetLoadingIndicatorStatus(ITabItem item, bool loading)
		{
			if (ContainerFromItem(item) is not Control tabItem)
				return;

			var stateToGoName = (loading) ? "Loading" : "NotLoading";
			VisualStateManager.GoToState(tabItem, stateToGoName, false);
		}
	}
}
