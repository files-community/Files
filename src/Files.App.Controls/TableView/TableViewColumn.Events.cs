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
			if (_owner is not null && _owner.TryGetTarget(out var owner) && owner.IsColumnResizing)
			{
				// Mouse pointer moved faster than the resize operation, revert to normal state in case of anything unexpected
				ResetPointerEventVisual();
				return;
			}

			VisualStateManager.GoToState(this, TemplateVisualStateName_ColumnPointerOver, true);
		}

		private void RootGrid_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, TemplateVisualStateName_ColumnNormal, true);
		}

		private void RootGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, TemplateVisualStateName_ColumnPressed, true);
		}

		private void RootGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, TemplateVisualStateName_ColumnPointerOver, true);

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
}
