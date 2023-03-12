using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Windows.System;

namespace Files.App.Actions
{
	internal class LayoutDetailsAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType => LayoutTypes.Details;

		public override string Label { get; } = "Details".GetLocalizedResource();

		public override RichGlyph Glyph { get; } = new("\uE179");
		public override HotKey HotKey { get; } = new(VirtualKey.Number1, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);
	}
}
