// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Dialogs;

namespace Files.App.Actions
{
	internal sealed partial class OpenSettingsAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> Strings.Settings.GetLocalizedResource();

		public string Description
			=> Strings.OpenSettingsDescription.GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.OemComma, KeyModifiers.Ctrl);

		public Task ExecuteAsync(object? parameter = null)
		{
			// Find index of existing Settings tab or open new one
			var existingTabIndex = MainPageViewModel.AppInstances
				.Select((tabItem, index) => new { TabItem = tabItem, Index = index })
				.FirstOrDefault(item =>
					item.TabItem.NavigationParameter.NavigationParameter is PaneNavigationArguments paneArgs &&
					(paneArgs.LeftPaneNavPathParam == "Settings" || paneArgs.RightPaneNavPathParam == "Settings"))
				?.Index ?? -1;

			if (existingTabIndex >= 0)
				App.AppModel.TabStripSelectedIndex = existingTabIndex;
			else
				NavigationHelpers.OpenPathInNewTab("Settings", true);

			return Task.CompletedTask;
		}
	}
}