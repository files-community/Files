using Files.Enums;
using Files.ViewModels;
using System;
using Windows.UI.Xaml.Data;

namespace Files.Converters
{
    public class LayoutModeToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if ((App.Current.Resources[parameter as string] as FolderLayoutInformation) is FolderLayoutInformation param
                && value is FolderLayoutInformation layoutModeValue && layoutModeValue.Mode == param.Mode)
            {
                if (layoutModeValue.Mode != FolderLayoutModes.GridView)
                {
                    return true;
                }
                else
                {
                    return param.SizeKind == layoutModeValue.SizeKind;
                }
            }
            else
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}