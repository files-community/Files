// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Helpers
{
    static class LayoutHelpers
    {
		public static void UpdateOpenTabsPreferences()
		{
			var multitaskingContext = Ioc.Default.GetRequiredService<IMultitaskingContext>();
			var tabs = multitaskingContext.Control?.GetAllTabInstances();
			var activePath = ((ShellPanesPage)multitaskingContext.CurrentTabItem.TabItemContent)?.ActivePane?.TabBarItemParameter?.NavigationParameter as string;
			if (tabs is null || activePath is null)
				return;

			for (int i = 0; i < tabs.Count; i++)
			{
				((ShellPanesPage)tabs[i]).UpdatePanesLayout(activePath, i != multitaskingContext.CurrentTabIndex);
			}
		}
	}
}
