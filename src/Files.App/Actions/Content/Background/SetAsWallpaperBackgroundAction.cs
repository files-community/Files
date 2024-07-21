// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class SetAsWallpaperBackgroundAction : BaseSetAsAction
	{
		private readonly IWindowsWallpaperService WindowsWallpaperService = Ioc.Default.GetRequiredService<IWindowsWallpaperService>();

		public override string Label
			=> "SetAsBackground".GetLocalizedResource();

		public override string Description
			=> "SetAsWallpaperBackgroundDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new("\uE91B");

		public override bool IsExecutable =>
			base.IsExecutable &&
			context.SelectedItem is not null;

		public override Task ExecuteAsync(object? parameter = null)
		{
			if (context.SelectedItem is null)
				return Task.CompletedTask;

			try
			{
				WindowsWallpaperService.SetDesktopWallpaper(context.SelectedItem.ItemPath);
			}
			catch (Exception ex)
			{
				ShowErrorDialog(ex.Message);
			}

			return Task.CompletedTask;
		}
	}
}
