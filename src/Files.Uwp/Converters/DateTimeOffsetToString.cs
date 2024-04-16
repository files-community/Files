using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Shared.Services.DateTimeFormatter;
using System;
using Windows.UI.Xaml.Data;

namespace Files.Uwp.Converters
{
    internal class DateTimeOffsetToString : IValueConverter
    {
        private static readonly IDateTimeFormatter formatter = Ioc.Default.GetService<IDateTimeFormatter>();

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is null)
            {
                return string.Empty;
            }
            return formatter.ToLongLabel((DateTimeOffset)value);
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