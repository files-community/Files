using System;
using Microsoft.UI.Xaml.Data;

namespace Files.App.Converters
{
	internal class StringArrayToString : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var array = value as string[];

			if (array is null || !(array is string[]))
			{
				return "";
			}

			var str = "";
			foreach (var i in array)
			{
				str += string.Format("{0}; ", i);
			}

			return str;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return (value as string).Split("; ");
		}
	}
}