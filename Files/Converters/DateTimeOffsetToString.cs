using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Files.Converters
{
    class DateTimeOffsetToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null)
                return ((DateTimeOffset)value).ToLocalTime().ToString();
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            try
            {
                return DateTimeOffset.Parse(value as string);
            }
            catch (FormatException e)
            {
                return null;
            }
        }
    }
}
