// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Data;

namespace Files.App.Converters
{
	internal sealed partial class DoubleToStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is not null)
			{
				return value.ToString() ?? string.Empty;
			}

			return "";
		}

		public object? ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return value is string text && double.TryParse(text, out var result)
				? result
				: null;
		}
	}
}
