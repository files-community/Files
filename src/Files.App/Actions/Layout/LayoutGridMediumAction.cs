using Files.App.Commands;
using Files.App.DataModels;
using Files.App.Extensions;
using Files.App.ViewModels;
using Windows.System;

namespace Files.App.Actions
{
	internal class LayoutGridMediumAction : LayoutAction
	{
		public override CommandCodes Code => CommandCodes.LayoutGridMedium;
		public override string Label => "MediumIcons".GetLocalizedResource();

		public override IGlyph Glyph { get; } = new Glyph("\uF0E2");
		public override HotKey HotKey { get; } = new(VirtualKey.Number4, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

		public override bool IsOn => ToolbarViewModel?.IsLayoutGridViewMedium ?? false;

		protected override string IsOnProperty => nameof(ToolbarViewModel.IsLayoutGridViewMedium);

		protected override void Execute() => FolderSettingsViewModel?.ToggleLayoutModeGridViewMedium(true);
	}
}
