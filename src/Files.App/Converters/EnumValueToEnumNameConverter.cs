// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Data;

namespace Files.App.Converters
{
	internal sealed class EnumValueToEnumNameConverter : IValueConverter
	{
		public string EnumTypeName { get; set; } = string.Empty;

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var stringValue = value.ToString() ?? string.Empty;

			return EnumTypeName switch
			{
				"DetailsViewSizeKind" => Enum.Parse<DetailsViewSizeKind>(stringValue).ToString(),
				"ListViewSizeKind" => Enum.Parse<ListViewSizeKind>(stringValue).ToString(),
				"TilesViewSizeKind" => Enum.Parse<TilesViewSizeKind>(stringValue).ToString(),
				"GridViewSizeKind" => Enum.Parse<GridViewSizeKind>(stringValue).ToString(),
				"ColumnsViewSizeKind" => Enum.Parse<ColumnsViewSizeKind>(stringValue).ToString(),
				_ => string.Empty,
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
