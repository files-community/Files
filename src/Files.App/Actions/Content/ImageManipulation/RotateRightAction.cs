// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Graphics.Imaging;

namespace Files.App.Actions
{
	internal class RotateRightAction : BaseRotateAction
	{
		public override string Label
			=> "RotateRight".GetLocalizedResource();

		public override string Description
			=> "RotateRightDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(opacityStyle: "ColorIconRotateRight");

		protected override BitmapRotation Rotation
			=> BitmapRotation.Clockwise90Degrees;
	}
}
