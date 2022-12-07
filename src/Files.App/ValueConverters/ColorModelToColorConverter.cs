using Files.App.Helpers;
using Files.Backend.Models.Coloring;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace Files.App.ValueConverters
{
    public sealed class ColorModelToColorConverter : IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, string language)
        {
            return value switch
            {
                SolidBrushColorModel solidBrushModel => GetBrush(solidBrushModel),
                _ => null
            };
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}

        private static Brush GetBrush(SolidBrushColorModel solidBrushModel)
        {
			return solidBrushModel.IsFromResource
                ? (Brush)App.Current.Resources[solidBrushModel.ColorCode]
                : new SolidColorBrush(ColorHelpers.FromHex(solidBrushModel.ColorCode));
        }
	}
}
