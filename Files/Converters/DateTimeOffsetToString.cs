using Files.Enums;
using System;
using Windows.Storage;
using Windows.UI.Xaml.Data;

namespace Files.Converters
{
    internal class DateTimeOffsetToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null)
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[Constants.LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";
                return (Extensions.DateTimeExtensions.GetFriendlyDateFromFormat((DateTimeOffset)value, returnformat, true));
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