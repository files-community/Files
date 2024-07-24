﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Graphics.Imaging;

namespace Files.App.Actions
{
	internal sealed class RotateLeftAction : BaseRotateAction
	{
		public override string Label
			=> "RotateLeft".GetLocalizedResource();

		public override string Description
			=> "RotateLeftDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.ImageRotate.ACW");

		protected override BitmapRotation Rotation
			=> BitmapRotation.Clockwise270Degrees;
	}
}
