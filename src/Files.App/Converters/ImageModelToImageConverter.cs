using Files.App.DataModels;
using Microsoft.UI.Xaml.Data;
using System;

namespace Files.App.Converters
{
	internal sealed class ImageModelToImageConverter : IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is BitmapImageModel bitmapImageModel)
				return bitmapImageModel.Image;

			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
