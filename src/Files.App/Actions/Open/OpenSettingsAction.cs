// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class OpenSettingsAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> Strings.Settings.GetLocalizedResource();

		public string Description
			=> Strings.OpenSettingsDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Open;

		public HotKey HotKey
			=> new(Keys.OemComma, KeyModifiers.Ctrl);

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Settings");

		public Task ExecuteAsync(object? parameter = null)
		{
			var settingsPage = (parameter as SettingsNavigationParams)?.PageKind.ToString();

			return settingsPage is not null
				? NavigationHelpers.AddNewTabByParamAsync(
					typeof(ShellPanesPage),
					new PaneNavigationArguments()
					{
						LeftPaneNavPathParam = "Settings",
						LeftPaneSelectItemParam = settingsPage,
					})
				: NavigationHelpers.OpenPathInNewTab("Settings", true);
		}
	}
}