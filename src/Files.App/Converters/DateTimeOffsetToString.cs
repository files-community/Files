using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Core.Services.DateTimeFormatter;
using Microsoft.UI.Xaml.Data;
using System;

namespace Files.App.Converters
{
	internal class DateTimeOffsetToString : IValueConverter
	{
		private static readonly IDateTimeFormatter formatter = Ioc.Default.GetService<IDateTimeFormatter>();

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return value is null
				? string.Empty
				: formatter.ToLongLabel((DateTimeOffset)value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			try
			{
				return DateTimeOffset.Parse(value as string);
			}
			catch (FormatException)
			{
				return null;
			}
		}
	}
}
