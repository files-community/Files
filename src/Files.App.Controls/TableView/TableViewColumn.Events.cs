// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Files.App.Controls
{
	public partial class TableViewColumn
	{
		private void TableViewColumn_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			UpdateEnabledVisualState(true);
		}

		private void TableViewColumn_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (!e.NewSize.Width.Equals(e.PreviousSize.Width) && _owner is not null && _owner.TryGetTarget(out var owner))
				owner.InvalidateLayoutOfAllRows();
		}

		private void TableViewColumn_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key is VirtualKey.Enter or VirtualKey.Space)
			{
				RequestSort();
				e.Handled = true;
			}
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
			UpdateEnabledVisualState(true);
		}

		private void RootGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (!IsEnabled || e.GetCurrentPoint(this).Properties.PointerUpdateKind is not PointerUpdateKind.LeftButtonPressed)
				return;

			VisualStateManager.GoToState(this, TemplateVisualStateName_ColumnPressed, true);
		}

		private void RootGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			if (!IsEnabled || e.GetCurrentPoint(this).Properties.PointerUpdateKind is not PointerUpdateKind.LeftButtonReleased)
				return;

			VisualStateManager.GoToState(this, TemplateVisualStateName_ColumnPointerOver, true);
		}

		private void RootGrid_Tapped(object sender, TappedRoutedEventArgs e)
		{
			RequestSort();
		}
	}
}
