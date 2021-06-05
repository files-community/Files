using Files.Enums;
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
            if ((int)value != -1)
            {
                return (GroupOption)(byte)(int)value;
            }
            else
            {
                return GroupOption.None;
            }
        }
    }
}
