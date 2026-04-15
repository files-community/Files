// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class SetDefaultThemeAction : BaseAppThemeAction, IAction
	{
		public string Label
			=> Strings.DefaultTheme.GetLocalizedResource();

		public string Description
			=> Strings.SwitchToDefaultThemeDescription.GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE790");

		public ActionCategory Category
			=> ActionCategory.Theme;

		public bool IsExecutable
			=> AppThemeModeService.AppThemeMode is not ElementTheme.Default;

		public Task ExecuteAsync(object? parameter = null)
			=> SetThemeAsync(ElementTheme.Default);
	}
}
