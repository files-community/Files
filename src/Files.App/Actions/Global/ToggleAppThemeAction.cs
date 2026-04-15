// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class ToggleAppThemeAction : BaseAppThemeAction, IAction
	{
		public string Label
			=> Strings.ToggleTheme.GetLocalizedResource();

		public string Description
			=> Strings.ToggleThemeDescription.GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.T, KeyModifiers.CtrlAlt);

		public RichGlyph Glyph
			=> new("\uE790");

		public ActionCategory Category
			=> ActionCategory.Theme;

		public Task ExecuteAsync(object? parameter = null)
		{
			var selectedTheme = AppThemeModeService.AppThemeMode;
			var nextTheme = selectedTheme switch
			{
				ElementTheme.Light => ElementTheme.Dark,
				ElementTheme.Dark => ElementTheme.Light,
				ElementTheme.Default => GetEffectiveTheme() is ElementTheme.Dark
					? ElementTheme.Light
					: ElementTheme.Dark,
				_ => ElementTheme.Light,
			};

			return SetThemeAsync(nextTheme);
		}
	}
}
