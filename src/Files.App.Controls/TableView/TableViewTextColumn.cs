// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Input;
using Windows.System;

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
			if (editingElement is not TextBox textBox)
				return;

			textBox.KeyDown += EditingTextBox_KeyDown;
			if (!FocusEditingTextBox(cell, textBox))
				textBox.Loaded += EditingTextBox_Loaded;
		}

		protected internal override bool CommitCellEdit(TableViewCell cell)
		{
			if (cell.EditingElement is not TextBox textBox ||
				cell.Data is null)
				return false;

			if (SetPropertyValue(cell.Data, textBox.Text))
			{
				UnhookTextBoxEvents(textBox);

				return true;
			}
			else
			{
				textBox.DispatcherQueue.TryEnqueue(() =>
				{
					textBox.Focus(FocusState.Programmatic);
					textBox.SelectAll();
				});

				return false;
			}
		}

		protected internal override void CancelCellEdit(TableViewCell cell)
		{
			UnhookTextBoxEvents(cell.EditingElement as TextBox);
		}

		private void EditingTextBox_Loaded(object sender, RoutedEventArgs e)
		{
			if (sender is not TextBox textBox)
				return;

			textBox.Loaded -= EditingTextBox_Loaded;
			if (textBox.FindAscendant<TableViewCell>() is { } cell)
				FocusEditingTextBox(cell, textBox);
		}

		private static bool FocusEditingTextBox(TableViewCell cell, TextBox textBox)
		{
			if (!cell.IsEditing || cell.EditingElement != textBox || textBox.XamlRoot is null)
				return false;

			if (!textBox.Focus(FocusState.Programmatic))
				return false;

			textBox.SelectAll();
			return true;
		}

		private void EditingTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (sender is not TextBox textBox ||
				textBox.FindAscendant<TableViewCell>() is not { } cell)
				return;

			if (e.Key is VirtualKey.Enter)
			{
				cell.CommitEdit();
				e.Handled = true;
			}
			else if (e.Key is VirtualKey.Escape)
			{
				cell.CancelEdit();
				e.Handled = true;
			}
		}

		private void UnhookTextBoxEvents(TextBox? textBox)
		{
			if (textBox is null)
				return;

			textBox.Loaded -= EditingTextBox_Loaded;
			textBox.KeyDown -= EditingTextBox_KeyDown;
		}
	}
}
