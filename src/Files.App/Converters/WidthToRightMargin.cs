using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Files.App.Converters
{
	internal class WidthToRightMargin : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return new Thickness(0, 0, (double)value, 0);
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return null;
		}
	}
}