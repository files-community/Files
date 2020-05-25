using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Data;

namespace Files.Converters
{
    class StorageDeleteOptionToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is StorageDeleteOption option && option == StorageDeleteOption.PermanentDelete;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (value is bool bl && bl) ? StorageDeleteOption.PermanentDelete : StorageDeleteOption.Default;
        }
    }
}
