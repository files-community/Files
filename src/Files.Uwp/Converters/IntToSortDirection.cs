using Microsoft.Toolkit.Uwp.UI;
using System;
using Windows.UI.Xaml.Data;

namespace Files.Uwp.Converters
{
    public class IntToSortDirection : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return System.Convert.ToInt32(value ?? SortDirection.Ascending);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if ((int)value != -1)
            {
                return (SortDirection)(int)value;
            }
            else
            {
                return SortDirection.Ascending;
            }
        }
    }
}