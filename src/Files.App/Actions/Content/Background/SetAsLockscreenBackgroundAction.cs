// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class SetAsLockscreenBackgroundAction : BaseSetAsAction
	{
		public override string Label
			=> Strings.Lockscreen.GetLocalizedResource();

		public override string ExtendedLabel
			=> Strings.SetAsLockscreen.GetLocalizedResource();

		public override string Description
			=> Strings.SetAsLockscreenBackgroundDescription.GetLocalizedResource();

		public string AccessKey
			=> "L";

		public override RichGlyph Glyph
			=> new("\uEE3F");

		public override bool IsExecutable =>
			base.IsExecutable &&
			ContentPageContext.SelectedItem is not null;

		public override Task ExecuteAsync(object? parameter = null)
		{
			if (!IsExecutable || ContentPageContext.SelectedItem is not ListedItem selectedItem)
				return Task.CompletedTask;

			try
			{
				return WindowsWallpaperService.SetLockScreenWallpaper(selectedItem.ItemPath);
			}
			catch (Exception ex)
			{
				ShowErrorDialog(ex.Message);

				return Task.CompletedTask;
			}
		}
	}
}
