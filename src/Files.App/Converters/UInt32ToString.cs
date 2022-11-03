using System;
using Microsoft.UI.Xaml.Data;

namespace Files.App.Converters
{
	internal class UInt32ToString : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return value is not null ? value.ToString() : "";
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			try
			{
				return UInt32.Parse(value as string);
			}
			catch (FormatException)
			{
				return null;
			}
		}
	}
}