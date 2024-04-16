using Files.Backend.Models.Imaging;
using System.Collections.Generic;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Uwp.Imaging
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
