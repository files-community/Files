// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Helpers
{
	static class LayoutHelpers
	{
		public static void UpdateOpenTabsPreferences()
		{
			// Services
			var multitaskingContext = Ioc.Default.GetRequiredService<IMultitaskingContext>();
			var layoutSettingsService = Ioc.Default.GetRequiredService<ILayoutSettingsService>();

			// Get all tab instances and active path
			var tabs = multitaskingContext.Control?.GetAllTabInstances();
			var activePath = (multitaskingContext.CurrentTabItem?.TabItemContent as ShellPanesPage)?.ActivePane?.TabBarItemParameter?.NavigationParameter as string;

			// Return if required data is missing
			if (tabs is null || activePath is null)
				return;

			for (int i = 0; i < tabs.Count; i++)
			{
				var isNotCurrentTab = i != multitaskingContext.CurrentTabIndex;
				var shPage = tabs[i] as ShellPanesPage;

				if (shPage is not null)
				{
					foreach (var pane in shPage.GetPanes())
					{
						var path = pane.ShellViewModel?.CurrentFolder?.ItemPath;

						// Skip panes without a valid path
						if (path is null)
							continue;

						// Check if we need to update preferences for this pane
						if ((isNotCurrentTab || pane != shPage.ActivePane) &&
							(layoutSettingsService.SyncFolderPreferencesAcrossDirectories ||
							 path.Equals(activePath, StringComparison.OrdinalIgnoreCase)))
							if (pane.SlimContentPage is BaseLayoutPage page)
								page.FolderSettings?.ReloadGroupAndSortPreferences(path);
					}
				}
			}
		}
	}
}
