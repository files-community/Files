// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class SetAsWallpaperBackgroundAction : BaseSetAsAction
	{
		public override string Label
			=> "SetAsBackground".GetLocalizedResource();

		public override string Description
			=> "SetAsWallpaperBackgroundDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new("\uE91B");

		public override bool IsExecutable =>
			base.IsExecutable &&
			ContentPageContext.SelectedItem is not null;

		public override Task ExecuteAsync()
		{
			if (ContentPageContext.SelectedItem is not null)
				return WallpaperHelpers.SetAsBackgroundAsync(WallpaperType.Desktop, ContentPageContext.SelectedItem.ItemPath);

			return Task.CompletedTask;
		}
	}
}
