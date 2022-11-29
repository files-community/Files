using Microsoft.UI.Xaml.Data;
using System;

namespace Files.App.Converters
{
	internal class UInt32ToString : IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, string language)
		{
			return value is not null ? value.ToString() : string.Empty;
		}

		public object? ConvertBack(object value, Type targetType, object parameter, string language)
		{
			try
			{
				return UInt32.Parse((string) value);
			}
			catch (FormatException)
			{
				return null;
			}
		}
	}
}