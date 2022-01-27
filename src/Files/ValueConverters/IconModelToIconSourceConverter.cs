using System;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;
using Files.Backend.Models.Icons;
using Microsoft.UI.Xaml.Controls;

namespace Files.ValueConverters
{
    internal sealed class IconModelToIconSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is UriIconModel uriIconModel)
            {
                return new ImageIconSource()
                {
                    ImageSource = new BitmapImage(uriIconModel.UriSource)
                };
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
