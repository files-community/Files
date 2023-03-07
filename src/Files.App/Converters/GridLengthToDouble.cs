using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Files.App.Converters
{
	public class GridLengthToDouble : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return value is GridLength { IsAbsolute: true } gridLength 
				? gridLength.Value 
				: throw new InvalidCastException("GridLength with a \"Star\"('*') or \"Auto\" value cannot be converted to double");
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return new GridLength((double)value);
		}
	}
}