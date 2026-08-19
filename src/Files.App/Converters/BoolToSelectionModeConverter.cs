// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using WinRT;

namespace Files.App.Converters
{
	internal sealed partial class BoolToSelectionModeConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return (value as bool?) ?? false ? ListViewSelectionMode.Multiple : ListViewSelectionMode.Extended;
		}

		[DynamicWindowsRuntimeCast(typeof(ListViewSelectionMode))]
		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return ((value as ListViewSelectionMode?) ?? ListViewSelectionMode.Extended) == ListViewSelectionMode.Multiple;
		}
	}
}
