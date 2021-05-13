using System;
using Windows.UI.Xaml.Data;

namespace Files.Converters
{
    internal class DateTimeOffsetToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null)
            {
                return ((DateTimeOffset)value).ToLocalTime().ToString("D");
            }

            return "";
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