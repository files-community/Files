﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class SetAsSlideshowBackgroundAction : BaseSetAsAction
	{
		public override string Label
			=> "SetAsSlideshow".GetLocalizedResource();

		public override string Description
			=> "SetAsSlideshowBackgroundDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new("\uE91B");

		public override bool IsExecutable =>
			base.IsExecutable &&
			context.SelectedItems.Count > 1;

		public override Task ExecuteAsync()
		{
			var paths = context.SelectedItems.Select(item => item.ItemPath).ToArray();
			WallpaperHelpers.SetSlideshow(paths);

			return Task.CompletedTask;
		}
	}
}
