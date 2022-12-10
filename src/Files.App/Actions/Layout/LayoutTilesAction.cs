using Files.App.Commands;
using Files.App.DataModels;
using Files.App.Extensions;
using Files.App.ViewModels;
using Windows.System;

namespace Files.App.Actions
{
	internal class LayoutTilesAction : LayoutAction
	{
		public override CommandCodes Code => CommandCodes.LayoutTiles;
		public override string Label => "Tiles".GetLocalizedResource();

		public override IGlyph Glyph { get; } = new Glyph("\uE15C");
		public override HotKey HotKey { get; } = new(VirtualKey.Number2, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

		public override bool IsOn => ToolbarViewModel?.IsLayoutTilesView ?? false;

		protected override string IsOnProperty => nameof(ToolbarViewModel.IsLayoutTilesView);

		protected override void Execute() => FolderSettingsViewModel?.ToggleLayoutModeTiles(true);
	}
}
