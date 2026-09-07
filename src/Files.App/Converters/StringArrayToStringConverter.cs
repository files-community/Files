// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Data;
using System.Text;

namespace Files.App.Converters
{
	internal sealed partial class StringArrayToStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var array = value as string[];

			if (array is null || !(array is string[]))
				return string.Empty;

			var str = new StringBuilder();
			foreach (var s in array)
			{
				str.Append($"{s}; ");
			}

			return str.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			var text = value as string ?? throw new ArgumentException("The value must be a string.", nameof(value));
			return text.Split("; ");
		}
	}
}
