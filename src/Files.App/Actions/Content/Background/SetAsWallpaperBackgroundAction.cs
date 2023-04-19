using Files.App.Commands;
using Files.Shared.Enums;

namespace Files.App.Actions
{
	internal class SetAsWallpaperBackgroundAction : BaseSetAsAction
	{
		public override string Label { get; } = "SetAsBackground".GetLocalizedResource();

		public override string Description => "SetAsWallpaperBackgroundDescription".GetLocalizedResource();

		public override RichGlyph Glyph { get; } = new("\uE91B");

		public override bool IsExecutable => base.IsExecutable &&
			context.SelectedItem is not null;

		public override Task ExecuteAsync()
		{
			if (context.SelectedItem is not null)
				WallpaperHelpers.SetAsBackground(WallpaperType.Desktop, context.SelectedItem.ItemPath);

			return Task.CompletedTask;
		}
	}
}
