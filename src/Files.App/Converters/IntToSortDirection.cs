using CommunityToolkit.WinUI.UI;
using System;
using Microsoft.UI.Xaml.Data;

namespace Files.App.Converters
{
	public class IntToSortDirection : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return System.Convert.ToInt32(value ?? SortDirection.Ascending);
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			if ((int)value != -1)
			{
				return (SortDirection)(int)value;
			}
			else
			{
				return SortDirection.Ascending;
			}
		}
	}
}