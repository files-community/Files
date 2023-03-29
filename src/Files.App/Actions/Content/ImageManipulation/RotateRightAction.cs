using Files.App.Commands;
using Files.App.Extensions;
using Windows.Graphics.Imaging;

namespace Files.App.Actions
{
	internal class RotateRightAction : BaseRotateAction
	{
		public override string Label { get; } = "RotateRight".GetLocalizedResource();

		public override string Description => "TODO: Need to be described.";

		public override RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconRotateRight");

		protected override BitmapRotation Rotation => BitmapRotation.Clockwise90Degrees;
	}
}
