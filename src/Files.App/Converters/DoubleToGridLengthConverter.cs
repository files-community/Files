// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Files.App.Converters
{
	internal sealed class DoubleToGridLengthConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is not double doubleValue)
				return new GridLength(0);

			return new GridLength(doubleValue);
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			if (value is not GridLength length)
				return 0;

			return length.Value;
		}
	}
}
