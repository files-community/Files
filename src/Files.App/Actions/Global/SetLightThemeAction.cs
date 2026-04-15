// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class SetLightThemeAction : BaseAppThemeAction, IAction
	{
		public string Label
			=> $"{Strings.LightTheme.GetLocalizedResource()} Theme";

		public string Description
			=> Strings.SwitchToLightThemeDescription.GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE790");

		public ActionCategory Category
			=> ActionCategory.Theme;

		public bool IsExecutable
			=> AppThemeModeService.AppThemeMode is not ElementTheme.Light;

		public Task ExecuteAsync(object? parameter = null)
			=> SetThemeAsync(ElementTheme.Light);
	}
}
