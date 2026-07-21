// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	public partial class TableViewTextColumn : TableViewBindableColumn
	{
		public TableViewTextColumn()
		{
			DefaultStyleKey = typeof(TableViewTextColumn);
		}

		public override FrameworkElement GenerateElement(object dataItem)
		{
			var cellValue = GetPropertyValue<string>(dataItem);

			return new TextBlock()
			{
				Style = ElementStyle,
				Text = cellValue,
			};
		}

		public override FrameworkElement GenerateEditingElement(object dataItem)
		{
			var cellValue = GetPropertyValue<string>(dataItem);

			return new TextBox()
			{
				Style = EditingElementStyle,
				Text = cellValue,
			};
		}

		protected internal override bool UpdateElement(FrameworkElement element, object dataItem)
		{
			if (element is not TextBlock textBlock)
				return false;

			textBlock.Style = ElementStyle;
			textBlock.Text = GetPropertyValue<string>(dataItem);
			return true;
		}

		protected internal override bool CanEdit(object dataItem)
		{
			return !string.IsNullOrEmpty(Binding) &&
				dataItem is ITableViewCellValueEditor;
		}

		protected internal override void PrepareCellForEdit(TableViewCell cell, FrameworkElement editingElement)
		{
			TableViewCellEditingBehavior.Prepare(editingElement);
		}

		protected internal override TableViewCellEditResult CommitCellEdit(TableViewCell cell)
		{
			if (cell.EditingElement is not TextBox textBox ||
				cell.Data is null)
				return TableViewCellEditResult.Failure();

			var result = SetPropertyValue(cell.Data, textBox.Text);
			if (result.Succeeded)
			{
				TableViewCellEditingBehavior.Unhook(textBox);
			}
			else
			{
				TableViewCellEditingBehavior.Refocus(textBox);
			}

			return result;
		}

		protected internal override void CancelCellEdit(TableViewCell cell)
		{
			TableViewCellEditingBehavior.Unhook(cell.EditingElement);
		}
	}
}
