// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Helpers
{
	static class LayoutHelpers
	{
		/// <summary>
		/// Reloads the layout preferences of the open panes that show the specified folder.
		/// </summary>
		public static void ReloadPreferences(string path)
		{
			var tabs = Ioc.Default.GetRequiredService<IMultitaskingContext>().Control?.GetAllTabInstances();
			if (tabs is null)
				return;

			foreach (var pane in tabs.OfType<ShellPanesPage>().SelectMany(page => page.GetPanes()))
			{
				if (pane.ShellViewModel is { } shellViewModel &&
					string.Equals(shellViewModel.WorkingDirectory.TrimPath(), path.TrimPath(), StringComparison.OrdinalIgnoreCase))
					pane.InstanceViewModel.FolderSettings.ReloadPreferences(shellViewModel.WorkingDirectory, () => FolderContentHelpers.GetContent(shellViewModel.FilesAndFolders.ToList()));
			}
		}

		public static void UpdateOpenTabsPreferences()
		{
			// Services
			var multitaskingContext = Ioc.Default.GetRequiredService<IMultitaskingContext>();

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
						if ((isNotCurrentTab || !ReferenceEquals(pane, shPage.ActivePane)) &&
							path.Equals(activePath, StringComparison.OrdinalIgnoreCase))
							if (pane.SlimContentPage is BaseLayoutPage page)
								page.FolderSettings?.ReloadPreferences(path);
					}
				}
			}
		}
	}
}
