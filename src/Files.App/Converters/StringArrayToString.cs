using Microsoft.UI.Xaml.Data;
using System;
using System.Text;

namespace Files.App.Converters
{
	internal class StringArrayToString : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var array = value as string[];

			if (array is null || !(array is string[]))
				return string.Empty;

			var str = new StringBuilder();
			foreach (var i in array)
			{
				str.Append(string.Format("{0}; ", i));
			}

			return str.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return (value as string).Split("; ");
		}
	}
}
