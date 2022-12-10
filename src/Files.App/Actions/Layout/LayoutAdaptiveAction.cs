using Files.App.Commands;
using Files.App.DataModels;
using Files.App.Extensions;
using Files.App.ViewModels;
using Windows.System;

namespace Files.App.Actions
{
	internal class LayoutAdaptiveAction : LayoutAction
	{
		public override CommandCodes Code => CommandCodes.LayoutAdaptive;
		public override string Label => "Adaptive".GetLocalizedResource();

		public override IGlyph Glyph { get; } = new Glyph("\uF576");
		public override HotKey HotKey { get; } = new(VirtualKey.Number7, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

		public override bool IsOn => ToolbarViewModel?.IsLayoutAdaptive ?? false;
		public override bool IsExecutable => ToolbarViewModel?.IsAdaptiveLayoutEnabled ?? false;

		protected override string IsOnProperty => nameof(ToolbarViewModel.IsLayoutDetailsView);

		protected override void Execute() => FolderSettingsViewModel?.ToggleLayoutModeAdaptive();
	}
}
