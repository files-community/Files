using System;
using Windows.UI.Xaml.Data;

namespace Files.Uwp.Converters
{
    public class MiddleTextEllipsisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string text = value as string;
            int maxLength = parameter == null ? 15 : System.Convert.ToInt32(parameter);

            if (string.IsNullOrWhiteSpace(text))
            {
                return value;
            }

            if (text.Length > maxLength)
            {
                int amountToCutOff = text.Length - maxLength;
                int middleIndexInString = text.Length / 2;

                int startIndex = text.Length - (middleIndexInString + (amountToCutOff / 2));
                int endIndex = middleIndexInString + (amountToCutOff / 2);

                string newString = string.Format("{0}...{1}", text.Substring(0, startIndex), text.Substring(endIndex));

                return newString;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}