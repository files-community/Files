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
				"DetailsViewSizeKind" => Enum.Parse<DetailsViewSizeKind>(stringValue).GetDescription().GetLocalizedResource(),
				"ListViewSizeKind" => Enum.Parse<ListViewSizeKind>(stringValue).GetDescription().GetLocalizedResource(),
				"TilesViewSizeKind" => Enum.Parse<TilesViewSizeKind>(stringValue).GetDescription().GetLocalizedResource(),
				"GridViewSizeKind" => Enum.Parse<GridViewSizeKind>(stringValue).GetDescription().GetLocalizedResource(),
				"ColumnsViewSizeKind" => Enum.Parse<ColumnsViewSizeKind>(stringValue).GetDescription().GetLocalizedResource(),
				_ => string.Empty,
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
