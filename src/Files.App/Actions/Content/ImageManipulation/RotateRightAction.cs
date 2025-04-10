// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Graphics.Imaging;

namespace Files.App.Actions
{
	internal sealed partial class RotateRightAction : BaseRotateAction
	{
		public override string Label
			=> Strings.RotateRight.GetLocalizedResource();

		public override string Description
			=> Strings.RotateRightDescription.GetLocalizedFormatResource(context.SelectedItems.Count);

		public override RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.ImageRotate.CW");

		protected override BitmapRotation Rotation
			=> BitmapRotation.Clockwise90Degrees;
	}
}
