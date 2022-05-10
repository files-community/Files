using Files.Shared.Enums;
using Files.Uwp.ViewModels;
using System;
using Windows.UI.Xaml.Data;

namespace Files.Uwp.Converters
{
    public class LayoutModeToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (App.Current.Resources[parameter as string] is FolderLayoutInformation param
                && value is FolderLayoutInformation layoutModeValue)
            {
                if (param.Mode == FolderLayoutModes.Adaptive)
                {
                    return layoutModeValue.IsAdaptive;
                }
                else if (layoutModeValue.Mode != FolderLayoutModes.GridView)
                {
                    return layoutModeValue.Mode == param.Mode
                        && !layoutModeValue.IsAdaptive;
                }
                else
                {
                    return layoutModeValue.Mode == param.Mode
                        && param.SizeKind == layoutModeValue.SizeKind
                        && !layoutModeValue.IsAdaptive;
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