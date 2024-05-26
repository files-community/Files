// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class SetAsAppBackgroundAction : BaseSetAsAction
	{
		private IAppearanceSettingsService AppearanceSettingsService { get; } = Ioc.Default.GetRequiredService<IAppearanceSettingsService>();

		public override string Label
			=> "SetAsAppBackground".GetLocalizedResource();

		public override string Description
			=> "SetAsAppBackgroundDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new("\uE91B");

		public override bool IsExecutable =>
			base.IsExecutable &&
			context.SelectedItem is not null;

		public override Task ExecuteAsync(object? parameter = null)
		{
			if (context.SelectedItem is not null)
				AppearanceSettingsService.AppThemeBackgroundImageSource = context.SelectedItem.ItemPath;

			return Task.CompletedTask;
		}
	}
}
