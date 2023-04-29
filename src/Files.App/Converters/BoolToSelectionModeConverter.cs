// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Files.App.Converters
{
	internal class BoolToSelectionModeConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return (value as bool?) ?? false ? ListViewSelectionMode.Multiple : ListViewSelectionMode.Extended;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return ((value as ListViewSelectionMode?) ?? ListViewSelectionMode.Extended) == ListViewSelectionMode.Multiple;
		}
	}
}
