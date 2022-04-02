using Files.Backend.Models.Imaging;
using System;
using System.Linq;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

#nullable enable

namespace Files.Uwp.ValueConverters
{
    internal sealed class ImageModelToImageConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is BitmapImageModel bitmapImageModel)
            {
                if (bitmapImageModel.Formats.Contains(Backend.Constants.KnownImageFormats.BITMAP_IMAGE_FORMAT))
                {
                    return bitmapImageModel.GetImage() as BitmapImage;
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
