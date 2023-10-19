// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.TabBar;
using Files.App.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.Helpers
{
	public static class MultitaskingTabsHelpers
	{
		public static void CloseTabsToTheLeft(TabBarItem clickedTab, ITabBar multitaskingControl)
		{
			if (multitaskingControl is not null)
			{
				var tabs = MainPageViewModel.AppInstances;
				var currentIndex = tabs.IndexOf(clickedTab);

				tabs.Take(currentIndex).ToList().ForEach(tab => multitaskingControl.CloseTab(tab));
			}
		}

		public static void CloseTabsToTheRight(TabBarItem clickedTab, ITabBar multitaskingControl)
		{
			if (multitaskingControl is not null)
			{
				var tabs = MainPageViewModel.AppInstances;
				var currentIndex = tabs.IndexOf(clickedTab);

				tabs.Skip(currentIndex + 1).ToList().ForEach(tab => multitaskingControl.CloseTab(tab));
			}
		}

		public static void CloseOtherTabs(TabBarItem clickedTab, ITabBar multitaskingControl)
		{
			if (multitaskingControl is not null)
			{
				var tabs = MainPageViewModel.AppInstances;
				tabs.Where((t) => t != clickedTab).ToList().ForEach(tab => multitaskingControl.CloseTab(tab));
			}
		}

		public static Task MoveTabToNewWindow(TabBarItem tab, ITabBar multitaskingControl)
		{
			int index = MainPageViewModel.AppInstances.IndexOf(tab);
			CustomTabViewItemParameter tabItemArguments = MainPageViewModel.AppInstances[index].NavigationParameter;

			multitaskingControl?.CloseTab(MainPageViewModel.AppInstances[index]);

			return tabItemArguments is not null
				? NavigationHelpers.OpenTabInNewWindowAsync(tabItemArguments.Serialize())
				: NavigationHelpers.OpenPathInNewWindowAsync("Home");
		}
	}
}
