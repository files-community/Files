using Files.App.Commands;
using Files.App.DataModels;
using Files.App.Extensions;
using Files.App.ViewModels;
using Windows.System;

namespace Files.App.Actions
{
	internal class LayoutGridSmallAction : LayoutAction
	{
		public override CommandCodes Code => CommandCodes.LayoutGridSmall;
		public override string Label => "SmallIcons".GetLocalizedResource();

		public override IGlyph Glyph { get; } = new Glyph("\uE80A");
		public override HotKey HotKey { get; } = new(VirtualKey.Number3, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

		public override bool IsOn => ToolbarViewModel?.IsLayoutGridViewSmall ?? false;

		protected override string IsOnProperty => nameof(ToolbarViewModel.IsLayoutGridViewSmall);

		protected override void Execute() => FolderSettingsViewModel?.ToggleLayoutModeGridViewSmall(true);
	}
}
