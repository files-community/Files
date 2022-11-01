using Files.Shared.Enums;
using System;
using Microsoft.UI.Xaml.Data;

namespace Files.App.Converters
{
	public class IntToGroupOption : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return System.Convert.ToInt32((byte)(value ?? GroupOption.None));
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			if ((int)value != -1)
			{
				return (GroupOption)(byte)(int)value;
			}
			else
			{
				return GroupOption.None;
			}
		}
	}
}