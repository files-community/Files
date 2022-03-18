using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Files.Converters
{
    public class BooleanToVisibilityInvertConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) =>
            (bool)value ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            (Visibility)value == Visibility.Collapsed;
    }
}