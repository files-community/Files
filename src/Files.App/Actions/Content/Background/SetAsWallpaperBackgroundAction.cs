// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class SetAsWallpaperBackgroundAction : BaseSetAsAction
	{
		public override string Label
			=> "SetAsBackground".GetLocalizedResource();

		public override string Description
			=> "SetAsWallpaperBackgroundDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new("\uE91B");

		public override bool IsExecutable =>
			base.IsExecutable &&
			context.SelectedItem is not null;

		public override Task ExecuteAsync()
		{
			if (context.SelectedItem is not null)
				return WallpaperHelpers.SetAsBackgroundAsync(WallpaperType.Desktop, context.SelectedItem.ItemPath);

			return Task.CompletedTask;
		}
	}
}
