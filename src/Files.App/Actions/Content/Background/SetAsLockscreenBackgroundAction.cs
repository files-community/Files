// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class SetAsLockscreenBackgroundAction : BaseSetAsAction
	{
		public override string Label
			=> "SetAsLockscreen".GetLocalizedResource();

		public override string Description
			=> "SetAsLockscreenBackgroundDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new("\uEE3F");

		public override bool IsExecutable =>
			base.IsExecutable &&
			context.SelectedItem is not null;

		public override Task ExecuteAsync(object? parameter = null)
		{
			if (context.SelectedItem is not null)
				return WallpaperHelpers.SetAsBackgroundAsync(WallpaperType.LockScreen, context.SelectedItem.ItemPath);

			return Task.CompletedTask;
		}
	}
}
