using Files.App.Imaging;
using System;
using System.Linq;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;


namespace Files.App.ValueConverters
{
    internal sealed class ImageModelToImageConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is BitmapImageModel bitmapImageModel)
            {
                if (bitmapImageModel.Formats.Contains(Constants.KnownImageFormats.BITMAP_IMAGE_FORMAT))
                {
                    return bitmapImageModel.GetImage<BitmapImage>();
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
