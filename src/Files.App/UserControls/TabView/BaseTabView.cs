// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.TabView
{
	public class BaseTabView : UserControl, ITabView
	{
		protected readonly MainPageViewModel mainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();

		protected ITabViewItemContent CurrentSelectedAppInstance;

		public static event EventHandler<ITabView>? OnLoaded;

		public static event PropertyChangedEventHandler? StaticPropertyChanged;

		public const string TabDropHandledIdentifier = "FilesTabViewItemDropHandled";

		public const string TabPathIdentifier = "FilesTabViewItemPath";

		// RecentlyClosedTabs is shared between all multitasking controls
		public static Stack<TabItemArguments[]> RecentlyClosedTabs { get; private set; } = new();

		public ObservableCollection<TabViewItem> Items
			=> MainPageViewModel.AppInstances;

		public event EventHandler<CurrentInstanceChangedEventArgs> CurrentInstanceChanged;

		private static bool isRestoringClosedTab;
		public static bool IsRestoringClosedTab
		{
			get => isRestoringClosedTab;
			private set
			{
				isRestoringClosedTab = value;
				StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(IsRestoringClosedTab)));
			}
		}

		public BaseTabView()
		{
			Loaded += MultitaskingControl_Loaded;
		}

		public virtual DependencyObject ContainerFromItem(ITabViewItem item)
		{
			return null;
		}

		public void SelectionChanged()
			=> TabStrip_SelectionChanged(null, null);

		public static void PushRecentTab(TabItemArguments[] tab)
		{
			RecentlyClosedTabs.Push(tab);
			StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(RecentlyClosedTabs)));
		}

		private void MultitaskingControl_CurrentInstanceChanged(object sender, CurrentInstanceChangedEventArgs e)
		{
			foreach (ITabViewItemContent instance in e.PageInstances)
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
			CloseTab(args.Item as TabViewItem);
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

		public ITabViewItemContent GetCurrentSelectedTabInstance()
		{
			return MainPageViewModel.AppInstances[App.AppModel.TabStripSelectedIndex].TabItemContent;
		}

		public List<ITabViewItemContent> GetAllTabInstances()
		{
			return MainPageViewModel.AppInstances.Select(x => x.TabItemContent).ToList();
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
			await MultitaskingTabsHelpers.MoveTabToNewWindow(((FrameworkElement)sender).DataContext as TabViewItem, this);
		}

		public void CloseTab(TabViewItem tabItem)
		{
			if (Items.Count == 1)
			{
				MainWindow.Instance.Close();
			}
			else if (Items.Count > 1)
			{
				Items.Remove(tabItem);

				// Dispose and save tab arguments
				tabItem?.Unload();

				RecentlyClosedTabs.Push(new TabItemArguments[] { tabItem.NavigationArguments });
			}
		}

		public void SetLoadingIndicatorStatus(ITabViewItem item, bool loading)
		{
			if (ContainerFromItem(item) is not Control tabItem)
				return;

			var stateToGoName = (loading) ? "Loading" : "NotLoading";
			VisualStateManager.GoToState(tabItem, stateToGoName, false);
		}
	}
}
