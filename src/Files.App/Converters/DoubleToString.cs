using Microsoft.UI.Xaml.Data;
using System;

namespace Files.App.Converters
{
	internal class DoubleToString : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is not null)
			{
				return value.ToString();
			}

			return "";
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			try
			{
				return Double.Parse(value as string);
			}
			catch (FormatException)
			{
				return null;
			}
		}
	}
}
