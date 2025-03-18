// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.Controls
{
	public partial class SidebarView
	{
		private void SidebarResizer_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			DisplayMode = DisplayMode is SidebarDisplayMode.Expanded
				? SidebarDisplayMode.Compact
				: SidebarDisplayMode.Expanded;

			e.Handled = true;
		}

		private void SidebarResizer_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			if (sender is not FrameworkElement sizer)
				return;

			sizer.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));

			VisualStateManager.GoToState(this, VisualStateName_ResizerPointerOver, true);
			e.Handled = true;
		}

		private void SidebarResizer_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			if (_draggingSidebarResizer)
				return;

			if (sender is not FrameworkElement sizer)
				return;

			sizer.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));

			VisualStateManager.GoToState(this, VisualStateName_ResizerNormal, true);

			e.Handled = true;
		}

		private void SidebarResizer_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
		{
			_draggingSidebarResizer = true;

			_preManipulationPaneWidth = _panePanel?.ActualWidth ?? throw new ArgumentNullException($"{_panePanel} is null.");

			VisualStateManager.GoToState(this, VisualStateName_ResizerPressed, true);

			e.Handled = true;
		}

		private void SidebarResizer_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			UpdateDisplayModeForPaneWidth(_preManipulationPaneWidth + e.Cumulative.Translation.X);

			e.Handled = true;
		}

		private void SidebarResizer_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			_draggingSidebarResizer = false;

			VisualStateManager.GoToState(this, VisualStateName_ResizerPressed, true);

			e.Handled = true;
		}

		private void SidebarResizer_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key is not VirtualKey.Space and
				not VirtualKey.Enter and
				not VirtualKey.Left and
				not VirtualKey.Right and
				not VirtualKey.Control)
				return;

			var primaryInvocation = e.Key is VirtualKey.Space or VirtualKey.Enter;

			if (DisplayMode is SidebarDisplayMode.Expanded)
			{
				if (primaryInvocation)
				{
					DisplayMode = SidebarDisplayMode.Compact;
					return;
				}

				var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
				var increment = ctrl.HasFlag(CoreVirtualKeyStates.Down) ? 5 : 1;

				// Left makes the pane smaller so we invert the increment
				if (e.Key is VirtualKey.Left)
					increment = -increment;

				var newWidth = OpenPaneLength + increment;
				UpdateDisplayModeForPaneWidth(newWidth);
				e.Handled = true;

				return;
			}
			else if (DisplayMode is SidebarDisplayMode.Compact)
			{
				if (primaryInvocation || e.Key is VirtualKey.Right)
				{
					DisplayMode = SidebarDisplayMode.Expanded;
					e.Handled = true;
				}
			}
		}

		private void PaneLightDismissLayer_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			IsPaneOpen = false;

			e.Handled = true;
		}

		private void PaneLightDismissLayer_Tapped(object sender, TappedRoutedEventArgs e)
		{
			IsPaneOpen = false;

			e.Handled = true;
		}

		private void PanePanel_ContextRequested(UIElement sender, ContextRequestedEventArgs e)
		{
			var newArgs = new ItemContextInvokedArgs(null, e.TryGetPosition(this, out var point) ? point : default);
			ViewModel?.HandleItemContextInvokedAsync(this, newArgs);

			e.Handled = true;
		}

		private void MenuItemsItemsRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
		{
			if (args.Element is SidebarItem sidebarItem)
				sidebarItem.HandleItemChange();
		}
	}
}
