// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Globalization;

namespace Files.App.Controls
{
	public partial class TableViewDateColumn : TableViewBindableColumn
	{
		public TableViewDateColumn()
		{
			DefaultStyleKey = typeof(TableViewDateColumn);
		}

		public override FrameworkElement GenerateElement(object dataItem)
		{
			var dateValue = GetPropertyValue<DateTimeOffset?>(dataItem);
			var cellValue = dateValue?.ToString("g", CultureInfo.CurrentCulture) ?? string.Empty;

			return new TextBlock()
			{
				Style = ElementStyle,
				Text = cellValue,
			};
		}

		protected internal override bool UpdateElement(FrameworkElement element, object dataItem)
		{
			if (element is not TextBlock textBlock)
				return false;

			var dateValue = GetPropertyValue<DateTimeOffset?>(dataItem);
			textBlock.Style = ElementStyle;
			textBlock.Text = dateValue?.ToString("g", CultureInfo.CurrentCulture) ?? string.Empty;
			return true;
		}
	}
}
