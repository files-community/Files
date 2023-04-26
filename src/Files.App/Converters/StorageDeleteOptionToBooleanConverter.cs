// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Data;
using Windows.Storage;

namespace Files.App.Converters
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
