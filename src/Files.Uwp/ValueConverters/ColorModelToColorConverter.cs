using Files.Backend.Models.Coloring;
using Files.Uwp.Helpers;
using System;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

#nullable enable

namespace Files.Uwp.ValueConverters
{
    public sealed class ColorModelToColorConverter : IValueConverter
    {
        public static Brush? Convert(object value)
        {
            if (value is SolidBrushColorModel solidBrushModel)
            {
                if (solidBrushModel.IsFromResource)
                {
                    return (Brush)App.Current.Resources[solidBrushModel.ColorCode];
                }

                return new SolidColorBrush(ColorHelpers.FromHex(solidBrushModel.ColorCode));
            }
            else
            {
                return null;
            }
        }

        public object? Convert(object value, Type targetType, object parameter, string language)
            => Convert(value);

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
