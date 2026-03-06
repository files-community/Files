// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using Windows.System;

namespace Files.App.Controls
{
	public partial class DragSelectionContainer
	{
		private void ContentPresenter_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (_contentPresenter is null)
				return;

			var currentPointerPointInfo = e.GetCurrentPoint(_contentPresenter);
			if (currentPointerPointInfo.Properties.IsLeftButtonPressed &&
				e.Pointer.PointerDeviceType is Microsoft.UI.Input.PointerDeviceType.Touch)
				return;

			_dragStartPoint = currentPointerPointInfo.Position;
			_selectionKind = e.KeyModifiers switch
			{
				VirtualKeyModifiers.Control => DragSelectionKind.InvertPreviousSelection,
				VirtualKeyModifiers.Shift => DragSelectionKind.ExtendPreviousSelection,
				_ => DragSelectionKind.IgnorePreviousSelection,
			};

			_contentPresenter.PointerMoved -= ContentPresenter_PointerMoved;
			_contentPresenter.PointerMoved += ContentPresenter_PointerMoved;

			if (_selectionKind is DragSelectionKind.IgnorePreviousSelection)
			{
				foreach (var target in Targets)
				{
					if (target.Target.ItemsSource is not null)
						target.Target.DeselectAll();
				}
			}

			GetPositionOfItemsInViewport();

			_contentPresenter.CapturePointer(e.Pointer);
			_selectionState = DragSelectionState.Started;

			e.Handled = true;
		}

		private void ContentPresenter_PointerMoved(object sender, PointerRoutedEventArgs e)
		{
			var currentPointerPointInfo = e.GetCurrentPoint(_contentPresenter);
			if (!currentPointerPointInfo.Properties.IsLeftButtonPressed ||
				_contentPresenter is null || _selectionRectangle is null)
				return;

			_dragCurrentPoint = currentPointerPointInfo.Position;

			if (_selectionState is DragSelectionState.Started &&
				(Math.Abs(_dragStartPoint.X - _dragCurrentPoint.X) < 5 ||
				Math.Abs(_dragStartPoint.Y - _dragCurrentPoint.Y) < 5))
				return;

			_selectionState = DragSelectionState.Moving;

			var rect = new Rect(
				(int)Math.Min(_dragStartPoint.X, _dragCurrentPoint.X),
				(int)Math.Min(_dragStartPoint.Y, _dragCurrentPoint.Y),
				(int)Math.Abs(_dragStartPoint.X - _dragCurrentPoint.X),
				(int)Math.Abs(_dragStartPoint.Y - _dragCurrentPoint.Y));

			// Draw a rectangle visual
			DrawRectangleOnCanvas();

			// Calculate the intersection between the selection rectangle and the visual of the items in the viewport
			foreach (var positionOfItem in _positionOfItems)
			{
				// Check if the item exists in the ListViewBase.ItemsSource before de/selecting it
				if (!positionOfItem.Value.ListViewBase.Items.Contains(positionOfItem.Key) ||
					positionOfItem.Value.ListViewBase.SelectionMode is ListViewSelectionMode.None or ListViewSelectionMode.Single)
					return;

				if (rect.IntersectsWith(positionOfItem.Value.Rect))
				{
					// The item should be selected
					if (!positionOfItem.Value.ListViewBase.SelectedItems.Contains(positionOfItem.Key))
						positionOfItem.Value.ListViewBase.SelectedItems.Add(positionOfItem.Key);
				}
				else
				{
					// The item should be either disselected or remain selected based on the selection kind
					positionOfItem.Value.ListViewBase.SelectedItems.Remove(positionOfItem.Key);
				}
			}

			e.Handled = true;
		}

		private void ContentPresenter_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			if (_selectionRectangle is null || _contentPresenter is null)
				return;

			Canvas.SetLeft(_selectionRectangle, 0);
			Canvas.SetTop(_selectionRectangle, 0);
			_selectionRectangle.Width = 0;
			_selectionRectangle.Height = 0;

			_contentPresenter.PointerMoved -= ContentPresenter_PointerMoved;
			_contentPresenter.ReleasePointerCapture(e.Pointer);

			_selectionState = DragSelectionState.Ended;

			// Fire selection changed event

			e.Handled = true;
		}
	}
}
