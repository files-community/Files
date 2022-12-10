using Files.App.Commands;
using Files.App.DataModels;
using Files.App.Extensions;
using Files.App.ViewModels;
using Windows.System;

namespace Files.App.Actions
{
	internal class LayoutColumnsAction : LayoutAction
	{
		public override CommandCodes Code => CommandCodes.LayoutColumns;
		public override string Label => "Columns".GetLocalizedResource();

		public override IGlyph Glyph { get; } = new Glyph("\uF115") { Family = "CustomGlyph" };
		public override HotKey HotKey { get; } = new(VirtualKey.Number6, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

		public override bool IsOn => ToolbarViewModel?.IsLayoutColumnsView ?? false;

		protected override string IsOnProperty => nameof(ToolbarViewModel.IsLayoutColumnsView);

		protected override void Execute() => FolderSettingsViewModel?.ToggleLayoutModeColumnView(true);
	}
}
