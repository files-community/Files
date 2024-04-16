using System;
using Windows.Storage;
using Windows.UI.Xaml.Data;

namespace Files.Uwp.Converters
{
    internal class StorageDeleteOptionToBooleanConverter : IValueConverter
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