using Files.Enums;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Files.Converters
{
    public class IntToGroupOption : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return System.Convert.ToInt32((byte)(value ?? GroupOption.None));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if((int)value != -1)
            {
                return (GroupOption)(byte)(int)value;
            } else
            {
                return GroupOption.None;
            }
        }
    }
    
    public class IntToSortOption : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return System.Convert.ToInt32((byte)(value ?? SortOption.Name));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if((int)value != -1)
            {
                return (SortOption)(byte)(int)value;
            } else
            {
                return SortOption.Name;
            }
        }
    }
    
    public class IntToSortDirection : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return System.Convert.ToInt32(value ?? SortDirection.Ascending);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if((int)value != -1)
            {
                return (SortDirection)(int)value;
            } else
            {
                return SortDirection.Ascending;
            }
        }
    }
}
