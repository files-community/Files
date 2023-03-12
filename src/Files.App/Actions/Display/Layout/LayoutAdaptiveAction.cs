using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Windows.System;

namespace Files.App.Actions
{
	internal class LayoutAdaptiveAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType => LayoutTypes.Adaptive;

		public override string Label { get; } = "Adaptive".GetLocalizedResource();

		public override bool IsExecutable => Context.IsLayoutAdaptiveEnabled;

		public override RichGlyph Glyph { get; } = new("\uF576");
		public override HotKey HotKey { get; } = new(VirtualKey.Number7, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

		protected override void OnContextChanged(string propertyName)
		{
			if (propertyName is nameof(IDisplayPageContext.IsLayoutAdaptiveEnabled))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
