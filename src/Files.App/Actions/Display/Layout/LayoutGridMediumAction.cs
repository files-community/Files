using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Windows.System;

namespace Files.App.Actions
{
	internal class LayoutGridMediumAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType => LayoutTypes.GridMedium;

		public override string Label { get; } = "MediumIcons".GetLocalizedResource();

		public override RichGlyph Glyph { get; } = new("\uF0E2");
		public override HotKey HotKey { get; } = new(VirtualKey.Number4, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);
	}
}
