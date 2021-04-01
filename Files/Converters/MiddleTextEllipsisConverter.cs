using System;
using Windows.UI.Xaml.Data;

namespace Files.Converters
{
    public class MiddleTextEllipsisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string text = value as string;
            int count = parameter == null ? 15 : System.Convert.ToInt32(parameter);

            if (text?.Length > count)
            {
                return string.Format("{0}...{1}", text.Substring(0, 11), text.Substring(text.Length - 11));
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
