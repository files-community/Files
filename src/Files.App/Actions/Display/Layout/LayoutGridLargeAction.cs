using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Windows.System;

namespace Files.App.Actions
{
	internal class LayoutGridLargeAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType => LayoutTypes.GridLarge;

		public override string Label { get; } = "LargeIcons".GetLocalizedResource();

		public override RichGlyph Glyph { get; } = new("\uE739");
		public override HotKey HotKey { get; } = new(VirtualKey.Number5, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);
	}
}
