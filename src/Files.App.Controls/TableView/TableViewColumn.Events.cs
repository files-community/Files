// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Input;

namespace Files.App.Controls
{
	public partial class TableViewColumn
	{
		private void TableViewColumn_Loaded(object sender, RoutedEventArgs e)
		{
			// We use "*" and thus Width is NaN but we want to set ActualWidth to Width for the proper resizing behavior
			Width = ActualWidth;
		}

		private void RootGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			_pointerEnteredToColumnVisual = true;
			VisualStateManager.GoToState(this, TemplateVisualStateName_ColumnPointerOver, true);
		}

		private void RootGrid_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			_pointerEnteredToColumnVisual = false;
			ResetPointerEventVisual();
		}

		private void ColumnVisualBorder_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			Debug.WriteLine("ColumnVisualBorder_PointerEntered");

			if (_owner is not null && _owner.TryGetTarget(out var owner) && owner.IsColumnResizing)
			{
				// Mouse pointer moved faster than the resize operation, revert to normal state in case of anything unexpected
				ResetPointerEventVisual();
				return;
			}

			VisualStateManager.GoToState(this, TemplateVisualStateName_ColumnPointerOver, true);
		}

		private void ColumnVisualBorder_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			Debug.WriteLine("ColumnVisualBorder_PointerExited");

			//ResetPointerEventVisual();
		}

		private void ColumnVisualBorder_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			Debug.WriteLine("ColumnVisualBorder_PointerPressed");

			_pointerEnteredToFilterButtonVisual = true;

			VisualStateManager.GoToState(this, TemplateVisualStateName_ColumnPressed, true);
		}

		private void ColumnVisualBorder_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			Debug.WriteLine("ColumnVisualBorder_PointerReleased");

			VisualStateManager.GoToState(this, TemplateVisualStateName_ColumnPointerOver, true);

			if (_pointerEnteredToFilterButtonVisual)
			{
				SortDirection = SortDirection is null or ListSortDirection.Descending
					? ListSortDirection.Ascending
					: SortDirection is ListSortDirection.Ascending
						? ListSortDirection.Descending
						: null;

				var visualStateName = SortDirection switch
				{
					ListSortDirection.Ascending => TemplateVisualStateName_SortOrderAscending,
					ListSortDirection.Descending => TemplateVisualStateName_SortOrderDescending,
					_ => TemplateVisualStateName_SortOrderNone,
				};

				if (SortDirection is not null && (_owner?.TryGetTarget(out var owner) ?? false) && owner.SortedColumn != this)
				{
					owner.SortedColumn?.SortDirection = null;
					owner.SortedColumn = this;
				}

				VisualStateManager.GoToState(this, visualStateName, true);
			}
		}

		private void FilterBorder_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			Debug.WriteLine("FilterBorder_PointerEntered");

			if (_owner is not null && _owner.TryGetTarget(out var owner) && owner.IsColumnResizing)
			{
				// Mouse pointer moved faster than the resize operation, revert to normal state in case of anything unexpected
				ResetPointerEventVisual();
				return;
			}

			VisualStateManager.GoToState(this, TemplateVisualStateName_FilterPointerOver, true);
		}

		private void FilterBorder_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			Debug.WriteLine("FilterBorder_PointerExited");

			_pointerEnteredToFilterButtonVisual = false;

			ResetPointerEventVisual();
		}

		private void FilterBorder_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			Debug.WriteLine("FilterBorder_PointerPressed");

			VisualStateManager.GoToState(this, TemplateVisualStateName_FilterPressed, true);
		}

		private void FilterBorder_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			Debug.WriteLine("FilterBorder_PointerReleased");

			VisualStateManager.GoToState(this, TemplateVisualStateName_FilterPointerOver, true);
		}
	}
}
