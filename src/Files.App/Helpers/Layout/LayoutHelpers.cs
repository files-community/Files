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

			var layoutSettingsService = Ioc.Default.GetRequiredService<ILayoutSettingsService>();
			for (int i = 0; i < tabs.Count; i++)
			{
				var isNotCurrentTab = i != multitaskingContext.CurrentTabIndex;
				var shPage = (ShellPanesPage)tabs[i];
				foreach (var pane in shPage.GetPanes())
				{
					var path = pane.ShellViewModel.CurrentFolder?.ItemPath;
					if ((isNotCurrentTab || pane != shPage.ActivePane) &&
						(layoutSettingsService.SyncFolderPreferencesAcrossDirectories ||
						path is not null &&
						path.Equals(activePath, StringComparison.OrdinalIgnoreCase)))
					{
						var page = pane.SlimContentPage as BaseLayoutPage;
						page?.FolderSettings?.ReloadGroupAndSortPreferences(path);
					}
				}
			}
		}
	}
}
