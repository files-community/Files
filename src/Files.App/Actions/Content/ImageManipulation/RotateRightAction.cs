// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Windows.Graphics.Imaging;

namespace Files.App.Actions
{
	internal class RotateRightAction : BaseRotateAction
	{
		public override string Label { get; } = "RotateRight".GetLocalizedResource();

		public override string Description => "RotateRightDescription".GetLocalizedResource();

		public override RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconRotateRight");

		protected override BitmapRotation Rotation => BitmapRotation.Clockwise90Degrees;
	}
}
