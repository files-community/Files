// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	public partial class TableViewTextColumn : TableViewBindableColumn
	{
		public TableViewTextColumn()
		{
			DefaultStyleKey = typeof(TableViewColumn);
		}

		public override FrameworkElement GenerateElement(object dataItem)
		{
			if (string.IsNullOrEmpty(Binding) ||
				dataItem is not ITableViewCellValueProvider cellValueProvider ||
				cellValueProvider?.GetValue(Binding) is not string cellValue)
				throw new ArgumentException("The type of the argument was invalid.", $"{dataItem}");

			var textBlock = new TextBlock
			{
				Style = ElementStyle,
				Text = cellValue
			};

			return textBlock;
		}

		public override FrameworkElement GenerateEditingElement(object dataItem)
		{
			if (string.IsNullOrEmpty(Binding) ||
				dataItem is not ITableViewCellValueProvider cellValueProvider ||
				cellValueProvider?.GetValue(Binding) is not string cellValue)
				throw new ArgumentException("The type of the argument was invalid.", $"{dataItem}");

			var textBox = new TextBox
			{
				Style = EditingElementStyle,
				Text = cellValue
			};

			return textBox;
		}
	}
}
