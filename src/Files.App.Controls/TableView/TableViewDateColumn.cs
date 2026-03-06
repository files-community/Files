// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Globalization;
using System.Runtime.InteropServices;
using Windows.System;

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

		public override FrameworkElement GenerateEditingElement(object dataItem)
		{
			var parsedDate = GetPropertyValue<DateTimeOffset?>(dataItem) ?? DateTimeOffset.Now;

			return new DatePicker()
			{
				Style = EditingElementStyle,
				Date = parsedDate,
			};
		}

		protected internal override bool CanEdit(object dataItem)
		{
			return !string.IsNullOrEmpty(Binding) &&
				dataItem is ITableViewCellValueEditor;
		}

		protected internal override void PrepareCellForEdit(TableViewCell cell, FrameworkElement editingElement)
		{
			if (editingElement is not DatePicker datePicker)
				return;

			datePicker.Loaded += EditingDatePicker_Loaded;
			datePicker.LostFocus += EditingDatePicker_LostFocus;
			datePicker.KeyDown += EditingDatePicker_KeyDown;
		}

		protected internal override bool CommitCellEdit(TableViewCell cell)
		{
			if (cell.EditingElement is not DatePicker datePicker ||
				cell.Data is null)
				return false;

			if (SetPropertyValue(cell.Data, (DateTimeOffset?)datePicker.Date))
			{
				UnhookDatePickerEvents(datePicker);

				return true;
			}
			else
			{
				datePicker.DispatcherQueue.TryEnqueue(() =>
				{
					datePicker.Focus(FocusState.Programmatic);
				});

				return false;
			}
		}

		protected internal override void CancelCellEdit(TableViewCell cell)
		{
			UnhookDatePickerEvents(cell.EditingElement as DatePicker);
		}

		private void UnhookDatePickerEvents(DatePicker? datePicker)
		{
			if (datePicker is null)
				return;

			datePicker.Loaded -= EditingDatePicker_Loaded;
			datePicker.LostFocus -= EditingDatePicker_LostFocus;
			datePicker.KeyDown -= EditingDatePicker_KeyDown;
		}

		private void EditingDatePicker_Loaded(object sender, RoutedEventArgs e)
		{
			if (sender is not DatePicker datePicker)
				return;

			datePicker.Loaded -= EditingDatePicker_Loaded;
			datePicker.DispatcherQueue.TryEnqueue(() =>
			{
				if (datePicker.FindAscendant<TableViewCell>() is not { IsEditing: true } cell ||
					cell.EditingElement != datePicker)
					return;

				datePicker.Focus(FocusState.Programmatic);
			});
		}

		private void EditingDatePicker_LostFocus(object sender, RoutedEventArgs e)
		{
			if (sender is not DatePicker datePicker)
				return;

			datePicker.DispatcherQueue.TryEnqueue(() =>
			{
				try
				{
					if (datePicker.FindAscendant<TableViewCell>() is not { IsEditing: true } cell ||
						cell.EditingElement != datePicker)
						return;

					var xamlRoot = datePicker.XamlRoot;
					if (xamlRoot is null)
						return;

					foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot))
					{
						if (popup.Child is not DatePickerFlyoutPresenter presenter)
							continue;

						void DatePickerFlyoutPresenter_Unloaded(object sender, RoutedEventArgs args)
						{
							presenter.Unloaded -= DatePickerFlyoutPresenter_Unloaded;

							datePicker.DispatcherQueue.TryEnqueue(() =>
							{
								if (datePicker.FindAscendant<TableViewCell>() is not { IsEditing: true } cell ||
									cell.EditingElement != datePicker)
									return;

								cell.CommitEdit();
							});
						}

						presenter.Unloaded += DatePickerFlyoutPresenter_Unloaded;
						return;
					}

					var focusedElement = FocusManager.GetFocusedElement(xamlRoot);
					if (focusedElement is null) return;

					// True when the currently focused element is part of the same DatePicker editing UI.
					// This happens if the focus is:
					// - the DatePicker itself
					// - any element inside that DatePicker
					// - inside a Popup (e.g. the DatePickerFlyout calendar)
					// In these cases we should not commit the edit.
					if (focusedElement is DependencyObject d &&
						(d == datePicker ||
						(d.FindAscendant<DatePicker>() is { } focusedDatePicker &&
						focusedDatePicker == datePicker) ||
						d.FindAscendant<Popup>() is not null))
						return;

					cell.CommitEdit();
				}
				catch (COMException)
				{
				}
			});
		}

		private void EditingDatePicker_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (sender is not DatePicker datePicker ||
				datePicker.FindAscendant<TableViewCell>() is not { } cell)
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
	}
}
