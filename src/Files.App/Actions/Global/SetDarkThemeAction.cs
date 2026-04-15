// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class SetDarkThemeAction : BaseAppThemeAction, IAction
	{
		public string Label
			=> $"{Strings.DarkTheme.GetLocalizedResource()} Theme";

		public string Description
			=> Strings.SwitchToDarkThemeDescription.GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE790");

		public ActionCategory Category
			=> ActionCategory.Theme;

		public bool IsExecutable
			=> AppThemeModeService.AppThemeMode is not ElementTheme.Dark;

		public Task ExecuteAsync(object? parameter = null)
			=> SetThemeAsync(ElementTheme.Dark);
	}
}
