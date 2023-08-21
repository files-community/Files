// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.CustomTabView;
using Files.App.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.Helpers
{
	public static class MultitaskingTabsHelpers
	{
		public static void CloseTabsToTheLeft(CustomTabViewItem clickedTab, ICustomTabView multitaskingControl)
		{
			if (multitaskingControl is not null)
			{
				var tabs = MainPageViewModel.AppInstances;
				var currentIndex = tabs.IndexOf(clickedTab);

				tabs.Take(currentIndex).ToList().ForEach(tab => multitaskingControl.CloseTab(tab));
			}
		}

		public static void CloseTabsToTheRight(CustomTabViewItem clickedTab, ICustomTabView multitaskingControl)
		{
			if (multitaskingControl is not null)
			{
				var tabs = MainPageViewModel.AppInstances;
				var currentIndex = tabs.IndexOf(clickedTab);

				tabs.Skip(currentIndex + 1).ToList().ForEach(tab => multitaskingControl.CloseTab(tab));
			}
		}

		public static void CloseOtherTabs(CustomTabViewItem clickedTab, ICustomTabView multitaskingControl)
		{
			if (multitaskingControl is not null)
			{
				var tabs = MainPageViewModel.AppInstances;
				tabs.Where((t) => t != clickedTab).ToList().ForEach(tab => multitaskingControl.CloseTab(tab));
			}
		}

		public static Task MoveTabToNewWindow(CustomTabViewItem tab, ICustomTabView multitaskingControl)
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
