// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Graphics.Imaging;

namespace Files.App.Actions
{
	internal sealed partial class RotateLeftAction : BaseRotateAction
	{
		public override string Label
			=> Strings.RotateLeft.GetLocalizedResource();

		public override string Description
			=> Strings.RotateLeftDescription.GetLocalizedFormatResource(context.SelectedItems.Count);

		public override RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.ImageRotate.ACW");

		protected override BitmapRotation Rotation
			=> BitmapRotation.Clockwise270Degrees;
	}
}
