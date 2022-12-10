using Files.App.Commands;
using Files.App.DataModels;
using Files.App.Extensions;
using Files.App.ViewModels;
using Windows.System;

namespace Files.App.Actions
{
	internal class LayoutDetailsAction : LayoutAction
	{
		public override CommandCodes Code => CommandCodes.LayoutDetails;
		public override string Label => "Details".GetLocalizedResource();

		public override IGlyph Glyph { get; } = new Glyph("\uE179");
		public override HotKey HotKey { get; } = new(VirtualKey.Number1, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

		public override bool IsOn => ToolbarViewModel?.IsLayoutDetailsView ?? false;

		protected override string IsOnProperty => nameof(ToolbarViewModel.IsLayoutDetailsView);

		protected override void Execute() => FolderSettingsViewModel?.ToggleLayoutModeDetailsView(true);
	}
}
