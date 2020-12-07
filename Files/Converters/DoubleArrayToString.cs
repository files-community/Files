using System;
using Windows.UI.Xaml.Data;

namespace Files.Converters
{
    internal class DoubleArrayToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var array = value as double[];

            if (array == null)
            {
                return "";
            }

            var str = "";

            foreach (var i in array)
            {
                str += string.Format("{0}; ", i);
            }

            return str;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            var strArray = (value as string).Split("; ");
            var array = new double[strArray.Length];
            for (int i = 0; i < strArray.Length; i++)
            {
                try
                {
                    array[i] = double.Parse(strArray[i]);
                }
                catch (Exception)
                {
                }
            }
            return array;
        }
    }
}