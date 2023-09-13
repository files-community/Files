// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Graphics.Imaging;

namespace Files.App.Actions
{
	/// <summary>
	/// Represents action to rotate image to the left.
	/// </summary>
	internal class RotateLeftAction : BaseRotateAction
	{
		public override string Label
			=> "RotateLeft".GetLocalizedResource();

		public override string Description
			=> "RotateLeftDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(opacityStyle: "ColorIconRotateLeft");

		protected override BitmapRotation Rotation
			=> BitmapRotation.Clockwise270Degrees;
	}
}
