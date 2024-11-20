// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;

namespace Files.App.Actions
{
	internal sealed class SetAsSlideshowBackgroundAction : BaseSetAsAction
	{
		private readonly IWindowsWallpaperService WindowsWallpaperService = Ioc.Default.GetRequiredService<IWindowsWallpaperService>();

		public override string Label
			=> "SetAsSlideshow".GetLocalizedResource();

		public override string Description
			=> "SetAsSlideshowBackgroundDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new("\uE91B");

		public override bool IsExecutable =>
			base.IsExecutable &&
			ContentPageContext.SelectedItems.Count > 1;

		public override Task ExecuteAsync(object? parameter = null)
		{
			if (!IsExecutable)
				return Task.CompletedTask;

			try
			{
				var paths = ContentPageContext.SelectedItems.Select(item => item.ItemPath).ToArray();
				WindowsWallpaperService.SetDesktopSlideshow(paths);
			}
			catch (Exception ex)
			{
				ShowErrorDialog(ex.Message);
			}

			return Task.CompletedTask;
		}
	}
}
