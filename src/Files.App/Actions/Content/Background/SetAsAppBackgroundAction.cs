// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class SetAsAppBackgroundAction : BaseSetAsAction
	{
		private readonly IAppearanceSettingsService AppearanceSettingsService = Ioc.Default.GetRequiredService<IAppearanceSettingsService>();

		public override string Label
			=> "SetAsAppBackground".GetLocalizedResource();

		public override string Description
			=> "SetAsAppBackgroundDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new("\uE91B");

		public override bool IsExecutable =>
			base.IsExecutable &&
			ContentPageContext.SelectedItem is not null;

		public override Task ExecuteAsync(object? parameter = null)
		{
			AppearanceSettingsService.AppThemeBackgroundImageSource = ContentPageContext.SelectedItem.ItemPath;

			return Task.CompletedTask;
		}
	}
}
