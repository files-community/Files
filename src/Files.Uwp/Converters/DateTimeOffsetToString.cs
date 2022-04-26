using System;
using Windows.UI.Xaml.Data;

namespace Files.Uwp.Converters
{
    internal class DateTimeOffsetToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is null)
            {
                return string.Empty;
            }
            return (Extensions.DateTimeExtensions.GetFriendlyDateFromFormat((DateTimeOffset)value, true));
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