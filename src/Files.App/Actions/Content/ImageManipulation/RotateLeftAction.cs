// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Extensions;
using Windows.Graphics.Imaging;

namespace Files.App.Actions
{
	internal class RotateLeftAction : BaseRotateAction
	{
		public override string Label { get; } = "RotateLeft".GetLocalizedResource();

		public override string Description => "RotateLeftDescription".GetLocalizedResource();

		public override RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconRotateLeft");

		protected override BitmapRotation Rotation => BitmapRotation.Clockwise270Degrees;
	}
}
