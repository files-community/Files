using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Files.App.Converters
{
	public class VisibilityInvertConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is bool isVisible)
			{
				return isVisible ? Visibility.Collapsed : Visibility.Visible;
			}

			return (Visibility)value == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
