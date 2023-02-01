﻿using Files.Backend.Models;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.App.AppModels
{
	/// <inheritdoc cref="IImageModel"/>
	internal sealed class BitmapImageModel : IImageModel
	{
		public BitmapImage Image { get; }

		public BitmapImageModel(BitmapImage image)
		{
			Image = image;
		}
	}
}
