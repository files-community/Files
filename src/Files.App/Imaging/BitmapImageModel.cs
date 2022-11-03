using Files.Backend.Models.Imaging;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Generic;

namespace Files.App.Imaging
{
	internal sealed class BitmapImageModel : ImageModel
	{
		private readonly BitmapImage? _bitmapImage;

		public override IReadOnlyCollection<string> Formats { get; } = new List<string>(1) { Constants.KnownImageFormats.BITMAP_IMAGE_FORMAT };

		public BitmapImageModel(BitmapImage? bitmapImage)
		{
			this._bitmapImage = bitmapImage;
		}

		public override TImage? GetImage<TImage>()
			where TImage : class
		{
			return (TImage?)(object?)_bitmapImage;
		}
	}
}
