using Files.App.Helpers;
using Files.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Files.App.UserControls.MultitaskingControl
{
	public class BaseMultitaskingControl : UserControl, IMultitaskingControl, INotifyPropertyChanged
	{
		public static event EventHandler<IMultitaskingControl>? OnLoaded;

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
		public static List<TabItemArguments[]> RecentlyClosedTabs { get; private set; } = new List<TabItemArguments[]>();

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
			if (ContainerFromItem(item) is not Control tabItem)
				return;

			var stateToGoName = (loading) ? "Loading" : "NotLoading";
			VisualStateManager.GoToState(tabItem, stateToGoName, false);
		}
	}
}
