using Microsoft.UI.Xaml.Data;

namespace Files.App.Converters
{
	internal class UInt32ToString : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return value is not null ? value.ToString() : string.Empty;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			try
			{
				return uint.Parse(value as string);
			}
			catch (FormatException)
			{
				return null;
			}
		}
	}
}
