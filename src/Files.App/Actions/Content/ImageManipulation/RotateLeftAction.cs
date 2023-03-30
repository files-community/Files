using Files.App.Commands;
using Files.App.Extensions;
using Windows.Graphics.Imaging;

namespace Files.App.Actions
{
	internal class RotateLeftAction : BaseRotateAction
	{
		public override string Label { get; } = "RotateLeft".GetLocalizedResource();

		public override string Description => "TODO: Need to be described.";

		public override RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconRotateLeft");

		protected override BitmapRotation Rotation => BitmapRotation.Clockwise270Degrees;
	}
}
