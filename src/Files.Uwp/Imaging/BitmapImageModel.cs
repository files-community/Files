using Files.Backend.Models.Imaging;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Uwp.Imaging
{
    internal sealed class BitmapImageModel : ImageModel
    {
        private readonly BitmapImage? _bitmapImage;

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
