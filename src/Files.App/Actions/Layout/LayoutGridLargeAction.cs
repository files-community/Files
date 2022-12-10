using Files.App.Commands;
using Files.App.DataModels;
using Files.App.Extensions;
using Files.App.ViewModels;
using Windows.System;

namespace Files.App.Actions
{
	internal class LayoutGridLargeAction : LayoutAction
	{
		public override CommandCodes Code => CommandCodes.LayoutGridLarge;
		public override string Label => "LargeIcons".GetLocalizedResource();

		public override IGlyph Glyph { get; } = new Glyph("\uE739");
		public override HotKey HotKey { get; } = new(VirtualKey.Number5, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

		public override bool IsOn => ToolbarViewModel?.IsLayoutGridViewLarge ?? false;

		protected override string IsOnProperty => nameof(ToolbarViewModel.IsLayoutGridViewLarge);

		protected override void Execute() => FolderSettingsViewModel?.ToggleLayoutModeGridViewLarge(true);
	}
}
