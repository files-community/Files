// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Helpers
{
	public static class MultitaskingTabsHelpers
	{
		public static void CloseTabsToTheLeft(TabBarItem clickedTab, ITabBar multitaskingControl)
		{
			if (multitaskingControl is not null)
			{
				var tabs = MainPageViewModel.CurrentInstanceTabBarItems;
				var currentIndex = tabs.IndexOf(clickedTab);

				tabs.Take(currentIndex).ToList().ForEach(tab => multitaskingControl.CloseTab(tab));
			}
		}

		public static void CloseTabsToTheRight(TabBarItem clickedTab, ITabBar multitaskingControl)
		{
			if (multitaskingControl is not null)
			{
				var tabs = MainPageViewModel.CurrentInstanceTabBarItems;
				var currentIndex = tabs.IndexOf(clickedTab);

				tabs.Skip(currentIndex + 1).ToList().ForEach(tab => multitaskingControl.CloseTab(tab));
			}
		}

		public static void CloseOtherTabs(TabBarItem clickedTab, ITabBar multitaskingControl)
		{
			if (multitaskingControl is not null)
			{
				var tabs = MainPageViewModel.CurrentInstanceTabBarItems;
				tabs.Where((t) => t != clickedTab).ToList().ForEach(tab => multitaskingControl.CloseTab(tab));
			}
		}

		public static Task MoveTabToNewWindow(TabBarItem tab, ITabBar multitaskingControl)
		{
			int index = MainPageViewModel.CurrentInstanceTabBarItems.IndexOf(tab);
			CustomTabViewItemParameter tabItemArguments = MainPageViewModel.CurrentInstanceTabBarItems[index].NavigationParameter;

			multitaskingControl?.CloseTab(MainPageViewModel.CurrentInstanceTabBarItems[index]);

			return tabItemArguments is not null
				? NavigationHelpers.OpenTabInNewWindowAsync(tabItemArguments.Serialize())
				: NavigationHelpers.OpenPathInNewWindowAsync("Home");
		}
	}
}
