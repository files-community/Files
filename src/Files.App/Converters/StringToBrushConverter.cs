using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace Files.App.Converters
{
	internal sealed class StringToBrushConverter : IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is not string strValue)
				return null;

			return new SolidColorBrush(strValue.ToColor());
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
