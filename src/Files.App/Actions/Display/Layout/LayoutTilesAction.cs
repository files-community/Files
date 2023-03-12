using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Windows.System;

namespace Files.App.Actions
{
	internal class LayoutTilesAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType => LayoutTypes.Tiles;

		public override string Label { get; } = "Tiles".GetLocalizedResource();

		public override RichGlyph Glyph { get; } = new("\uE15C");
		public override HotKey HotKey { get; } = new(VirtualKey.Number2, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);
	}
}
