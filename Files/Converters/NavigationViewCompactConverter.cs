using Microsoft.UI.Xaml.Controls;
using System;
using Windows.UI.Xaml.Data;

namespace Files.Converters
{
    public class NavigationViewCompactConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (value as bool?) ?? false ? NavigationViewPaneDisplayMode.LeftCompact : NavigationViewPaneDisplayMode.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
