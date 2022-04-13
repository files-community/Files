using System;
using Windows.UI.Xaml.Data;

namespace Files.Uwp.ValueConverters
{
    internal sealed class BoolInvertConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not bool boolVal)
            {
                return false;
            }

            if (parameter is string strParam && strParam.ToLower() == "invert")
            {
                return boolVal;
            }

            return !boolVal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
