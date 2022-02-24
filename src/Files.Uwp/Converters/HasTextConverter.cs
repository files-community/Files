using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Files.Converters
{
    public class HasTextConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool b = !Invert ? ToBool(value) : !ToBool(value);

            if (targetType == typeof(Visibility))
            {
                return b ? Visibility.Visible : Visibility.Collapsed;
            }
            return b;

            static bool ToBool(object value) => value is string s && !string.IsNullOrEmpty(s);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
