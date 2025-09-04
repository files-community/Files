// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class SetAsSlideshowBackgroundAction : BaseSetAsAction
	{
		private readonly IWindowsWallpaperService WindowsWallpaperService = Ioc.Default.GetRequiredService<IWindowsWallpaperService>();

		public override string Label
			=> Strings.SetAsSlideshow.GetLocalizedResource();

		public override string Description
			=> Strings.SetAsSlideshowBackgroundDescription.GetLocalizedResource();

		public override RichGlyph Glyph
			=> new("\uE91B");

		public override bool IsExecutable =>
			base.IsExecutable &&
			ContentPageContext.SelectedItems.Count > 1;

		public override Task ExecuteAsync(object? parameter = null)
		{
			if (!IsExecutable || ContentPageContext.SelectedItems.Select(item => item.ItemPath).ToArray() is not string[] paths || paths.Length is 0)
				return Task.CompletedTask;

			try
			{
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
