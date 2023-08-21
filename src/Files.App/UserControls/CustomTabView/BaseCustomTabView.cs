// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.CustomTabView
{
	/// <summary>
	/// Represents base class for <see cref="CustomTabView"/>.
	/// </summary>
	public abstract class BaseCustomTabView : UserControl, ICustomTabView
	{
		protected readonly MainPageViewModel mainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();

		protected ICustomTabViewItemContent CurrentSelectedAppInstance;

		public static event EventHandler<ICustomTabView>? OnLoaded;

		public static event PropertyChangedEventHandler? StaticPropertyChanged;

		public const string TabDropHandledIdentifier = "FilesTabViewItemDropHandled";

		public const string TabPathIdentifier = "FilesTabViewItemPath";

		// RecentlyClosedTabs is shared between all multitasking controls
		public static Stack<CustomTabViewItemParameter[]> RecentlyClosedTabs { get; private set; } = new();

		public ObservableCollection<CustomTabViewItem> Items
			=> MainPageViewModel.AppInstances;

		public event EventHandler<CurrentInstanceChangedEventArgs> CurrentInstanceChanged;

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

		public BaseCustomTabView()
		{
			Loaded += TabView_Loaded;
		}

		public virtual DependencyObject ContainerFromItem(ICustomTabViewItem item)
		{
			return null;
		}

		private void TabView_CurrentInstanceChanged(object sender, CurrentInstanceChangedEventArgs e)
		{
			foreach (ICustomTabViewItemContent instance in e.PageInstances)
			{
				if (instance is not null)
				{
					instance.IsCurrentInstance = instance == e.CurrentInstance;
				}
			}
		}

		protected void TabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

		protected void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
		{
			CloseTab(args.Item as CustomTabViewItem);
		}

		protected void OnCurrentInstanceChanged(CurrentInstanceChangedEventArgs args)
		{
			CurrentInstanceChanged?.Invoke(this, args);
		}

		public void TabView_Loaded(object sender, RoutedEventArgs e)
		{
			CurrentInstanceChanged += TabView_CurrentInstanceChanged;
			OnLoaded?.Invoke(null, this);
		}

		public ICustomTabViewItemContent GetCurrentSelectedTabInstance()
		{
			return MainPageViewModel.AppInstances[App.AppModel.TabStripSelectedIndex].TabItemContent;
		}

		public void SelectionChanged()
		{
			TabView_SelectionChanged(null, null);
		}

		public static void PushRecentTab(CustomTabViewItemParameter[] tab)
		{
			RecentlyClosedTabs.Push(tab);
			StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(RecentlyClosedTabs)));
		}

		public List<ICustomTabViewItemContent> GetAllTabInstances()
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
					await mainPageViewModel.AddNewTabByParam(item.InitialPageType, item.NavigationParameter);

				IsRestoringClosedTab = false;
			}
		}

		public async void MoveTabToNewWindow(object sender, RoutedEventArgs e)
		{
			await MultitaskingTabsHelpers.MoveTabToNewWindow(((FrameworkElement)sender).DataContext as CustomTabViewItem, this);
		}

		public void CloseTab(CustomTabViewItem tabItem)
		{
			Items.Remove(tabItem);
			tabItem?.Unload();
			
			// Dispose and save tab arguments
			RecentlyClosedTabs.Push(new CustomTabViewItemParameter[]
			{
				tabItem.NavigationParameter,
			});

			if (Items.Count == 0)
				MainWindow.Instance.Close();
		}

		public void SetLoadingIndicatorStatus(ICustomTabViewItem item, bool loading)
		{
			if (ContainerFromItem(item) is not Control tabItem)
				return;

			var stateToGoName = (loading) ? "Loading" : "NotLoading";

			VisualStateManager.GoToState(tabItem, stateToGoName, false);
		}
	}
}
