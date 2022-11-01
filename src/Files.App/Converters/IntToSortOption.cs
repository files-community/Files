using Files.Shared.Enums;
using System;
using Microsoft.UI.Xaml.Data;

namespace Files.App.Converters
{
	public class IntToSortOption : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return System.Convert.ToInt32((byte)(value ?? SortOption.Name));
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			if ((int)value != -1)
			{
				return (SortOption)(byte)(int)value;
			}
			else
			{
				return SortOption.Name;
			}
		}
	}
}