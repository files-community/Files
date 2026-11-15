using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Files.App.Converters
{
	internal sealed partial class TerminalHeightConverter : IValueConverter
	{
		public object Convert(object value, Type targetType,
			object parameter, string language)
		{
			return (bool)value
				? new GridLength(0.5, GridUnitType.Star)
				: new GridLength(0);
		}

		public object ConvertBack(object value, Type targetType,
			object parameter, string language)
		{
			throw new NotSupportedException();
		}
	}
}
