// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.System;
using WinRT;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;

namespace Files.App.UserControls.Selection
{
	public sealed class RectangleSelection_ListViewBase : RectangleSelection
	{
		private ListViewBase uiElement;
		private ScrollViewer? scrollViewer;
		private SelectionChangedEventHandler? selectionChanged;
		private DispatcherQueueTimer? timer;
		private Point originDragPoint;
		private Dictionary<object, System.Drawing.Rectangle> itemsPosition;
		private List<object>? prevSelectedItems;
		private List<object>? prevSelectedItemsDrag;
		private ItemSelectionStrategy? selectionStrategy;

		public RectangleSelection_ListViewBase(ListViewBase uiElement, Rectangle selectionRectangle, SelectionChangedEventHandler? selectionChanged = null)
			: base(selectionRectangle)
		{
			this.uiElement = uiElement;
			this.selectionChanged = selectionChanged;
			itemsPosition = [];
			InitEvents(null, null);
		}

		private void RectangleSelection_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
		{
			if (scrollViewer is null || selectionState is SelectionState.Inactive)
				return;

			var currentPoint = e.GetCurrentPoint(uiElement);

			// rebuild item bounds on Ctrl+wheel
			if (e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control))
			{
				DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
				{
					if (selectionState is SelectionState.Inactive)
						return;

					itemsPosition.Clear();
					scrollViewer.UpdateLayout();
					FetchItemsPosition();

					if (selectionState is SelectionState.Active)
						UpdateSelectionFromPointer(sender, currentPoint, scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
				});
				return;
			}

			var delta = currentPoint.Properties.MouseWheelDelta;
			if (delta == 0)
				return;

			var horizontalOffset = scrollViewer.HorizontalOffset;
			var verticalOffset = scrollViewer.VerticalOffset;
			var newHorizontalOffset = horizontalOffset;
			var newVerticalOffset = verticalOffset;
			if (scrollViewer.ScrollableHeight > 0)
				newVerticalOffset = Math.Clamp(verticalOffset - delta, 0, scrollViewer.ScrollableHeight);
			else if (scrollViewer.ScrollableWidth > 0)
				newHorizontalOffset = Math.Clamp(horizontalOffset - delta, 0, scrollViewer.ScrollableWidth);

			if (newHorizontalOffset != horizontalOffset || newVerticalOffset != verticalOffset)
			{
				scrollViewer.ChangeView(newHorizontalOffset, newVerticalOffset, null, true);
				scrollViewer.UpdateLayout();
				FetchItemsPosition();
			}

			if (selectionState is SelectionState.Starting)
				ActivateSelection();

			UpdateSelectionFromPointer(sender, currentPoint, newHorizontalOffset, newVerticalOffset);

			e.Handled = true;
		}

		private void RectangleSelection_PointerMoved(object sender, PointerRoutedEventArgs e)
		{
			if (scrollViewer is null)
			{
				return;
			}

			var currentPoint = e.GetCurrentPoint(uiElement);
			var horizontalOffset = scrollViewer.HorizontalOffset;
			var verticalOffset = scrollViewer.VerticalOffset;
			if (selectionState == SelectionState.Starting)
			{
				if (!HasMovedMinimalDelta(originDragPoint.X - horizontalOffset, originDragPoint.Y - verticalOffset, currentPoint.Position.X, currentPoint.Position.Y))
				{
					return;
				}

				ActivateSelection();
			}
			if (currentPoint.Properties.IsLeftButtonPressed)
			{
				UpdateSelectionFromPointer(sender, currentPoint, horizontalOffset, verticalOffset);

				var newHorizontalOffset = horizontalOffset;
				var newVerticalOffset = verticalOffset;
				if (currentPoint.Position.Y > uiElement.ActualHeight - 20)
				{
					newVerticalOffset += Math.Min(currentPoint.Position.Y - (uiElement.ActualHeight - 20), 40);
				}
				else if (currentPoint.Position.Y < 20)
				{
					newVerticalOffset -= Math.Min(20 - currentPoint.Position.Y, 40);
				}

				if (currentPoint.Position.X > uiElement.ActualWidth - 20)
				{
					newHorizontalOffset += Math.Min(currentPoint.Position.X - (uiElement.ActualWidth - 20), 40);
				}
				else if (currentPoint.Position.X < 20)
				{
					newHorizontalOffset -= Math.Min(20 - currentPoint.Position.X, 40);
				}

				newHorizontalOffset = Math.Clamp(newHorizontalOffset, 0, scrollViewer.ScrollableWidth);
				newVerticalOffset = Math.Clamp(newVerticalOffset, 0, scrollViewer.ScrollableHeight);
				if (newHorizontalOffset != horizontalOffset || newVerticalOffset != verticalOffset)
					scrollViewer.ChangeView(newHorizontalOffset, newVerticalOffset, null, false);
			}
		}

		private void ActivateSelection()
		{
			selectionStrategy!.StartSelection();
			OnSelectionStarted();
			selectionState = SelectionState.Active;
		}

		private void UpdateSelectionFromPointer(object sender, PointerPoint currentPoint, double horizontalOffset, double verticalOffset)
		{
			var originDragPointShifted = new Point(originDragPoint.X - horizontalOffset, originDragPoint.Y - verticalOffset); // Initial drag point relative to the topleft corner
			DrawRectangle(currentPoint, originDragPointShifted, uiElement);
			var currentX = currentPoint.Position.X + horizontalOffset;
			var currentY = currentPoint.Position.Y + verticalOffset;
			// Selected area considering scrolled offset
			var rect = new System.Drawing.Rectangle((int)Math.Min(originDragPoint.X, currentX),(int)Math.Min(originDragPoint.Y, currentY), (int)Math.Abs(originDragPoint.X - currentX), (int)Math.Abs(originDragPoint.Y - currentY));

			var selectedItemsBeforeChange = uiElement.SelectedItems.ToArray();

			foreach (var item in itemsPosition.ToList())
			{
				try
				{
					if (rect.IntersectsWith(item.Value))
					{
						selectionStrategy!.HandleIntersectionWithItem(item.Key);
					}
					else
					{
						selectionStrategy!.HandleNoIntersectionWithItem(item.Key);
					}
				}
				catch (ArgumentException)
				{
					// Item is not present in the ItemsSource
					itemsPosition.Remove(item);
				}
			}

			if (selectionChanged is not null)
			{
				var currentSelectedItemsDrag = uiElement.SelectedItems.Cast<object>().ToList();
				if (prevSelectedItemsDrag is null || !prevSelectedItemsDrag.SequenceEqual(currentSelectedItemsDrag))
				{
					// Trigger SelectionChanged event if the selection has changed
					var removedItems = selectedItemsBeforeChange.Except(currentSelectedItemsDrag).ToList();
					selectionChanged(sender, new SelectionChangedEventArgs(removedItems, currentSelectedItemsDrag));
					prevSelectedItemsDrag = currentSelectedItemsDrag;
				}
			}
		}

		private void RectangleSelection_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (scrollViewer is null)
			{
				return;
			}

			itemsPosition.Clear();

			scrollViewer.ViewChanged -= ScrollViewer_ViewChanged;
			scrollViewer.ViewChanged += ScrollViewer_ViewChanged;

			var currentPoint = e.GetCurrentPoint(uiElement);
			originDragPoint = new Point(currentPoint.Position.X, currentPoint.Position.Y); // Initial drag point relative to the topleft corner
			prevSelectedItems = uiElement.SelectedItems.Cast<object>().ToList(); // Save current selected items

			originDragPoint.X += scrollViewer.HorizontalOffset;
			originDragPoint.Y += scrollViewer.VerticalOffset; // Initial drag point relative to the top of the list (considering scrolled offset)
			if (!currentPoint.Properties.IsLeftButtonPressed || e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Touch)
			{
				// Trigger only on left click, do not trigger with touch
				return;
			}

			FetchItemsPosition();

			selectionStrategy = e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control) ?
					new InvertPreviousItemSelectionStrategy(uiElement.SelectedItems, prevSelectedItems) :
					e.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift) ?
						new ExtendPreviousItemSelectionStrategy(uiElement.SelectedItems, prevSelectedItems) :
						new IgnorePreviousItemSelectionStrategy(uiElement.SelectedItems);

			selectionStrategy.HandleNoItemSelected();

			uiElement.PointerMoved -= RectangleSelection_PointerMoved;
			uiElement.PointerMoved += RectangleSelection_PointerMoved;
			uiElement.PointerWheelChanged -= RectangleSelection_PointerWheelChanged;
			uiElement.PointerWheelChanged += RectangleSelection_PointerWheelChanged;
			if (selectionChanged is not null)
			{
				// Unsunscribe from SelectionChanged event for performance
				uiElement.SelectionChanged -= selectionChanged;
			}
			uiElement.CapturePointer(e.Pointer);
			selectionState = SelectionState.Starting;
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void FetchItemsPosition()
		{
			var horizontalOffset = scrollViewer!.HorizontalOffset;
			var verticalOffset = scrollViewer.VerticalOffset;
			foreach (var item in uiElement.Items.ToList().Except(itemsPosition.Keys))
			{
				var listViewItem = (FrameworkElement)uiElement.ContainerFromItem(item); // Get ListViewItem
				if (listViewItem is null)
				{
					continue; // Element is not loaded (virtualized list)
				}

				var gt = listViewItem.TransformToVisual(uiElement);
				var itemStartPoint = gt.TransformPoint(new Point(horizontalOffset, verticalOffset)); // Get item position relative to the top of the list (considering scrolled offset)
				var itemRect = new System.Drawing.Rectangle((int)itemStartPoint.X, (int)itemStartPoint.Y, (int)listViewItem.ActualWidth, (int)listViewItem.ActualHeight);
				itemsPosition[item] = itemRect;
			}
		}

		private void ScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
		{
			if (timer is null)
			{
				timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
			}
			if (!timer.IsRunning)
			{
				timer.Debounce(FetchItemsPosition, TimeSpan.FromMilliseconds(1000));
			}
		}

		[DynamicWindowsRuntimeCast(typeof(ListViewBase))]
		private void RectangleSelection_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			if (scrollViewer is null) return;
			Canvas.SetLeft(selectionRectangle, 0);
			Canvas.SetTop(selectionRectangle, 0);
			selectionRectangle.Width = 0;
			selectionRectangle.Height = 0;
			uiElement.PointerMoved -= RectangleSelection_PointerMoved;
			uiElement.PointerWheelChanged -= RectangleSelection_PointerWheelChanged;

			scrollViewer.ViewChanged -= ScrollViewer_ViewChanged;
			uiElement.ReleasePointerCapture(e.Pointer);
			if (selectionChanged is not null)
			{
				// Restore and trigger SelectionChanged event
				uiElement.SelectionChanged -= selectionChanged;
				uiElement.SelectionChanged += selectionChanged;
				if (prevSelectedItems is null || !uiElement.SelectedItems.SequenceEqual(prevSelectedItems))
				{
					// Trigger SelectionChanged event if the selection has changed
					selectionChanged(sender, null);
				}
			}
			if (selectionState == SelectionState.Active || e.OriginalSource is ListViewBase)
			{
				// Always trigger SelectionEnded to focus the file list when clicking on the empty space (#2977)
				OnSelectionEnded();
			}

			selectionStrategy = null;
			selectionState = SelectionState.Inactive;

			prevSelectedItemsDrag = null;

			e.Handled = true;
		}

		private void RectangleSelection_SizeChanged(object sender, object e)
		{
			scrollViewer ??= DependencyObjectHelpers.FindChild<ScrollViewer>(uiElement, sv => sv.VerticalScrollMode != ScrollMode.Disabled);

			if (scrollViewer is not null)
			{
				uiElement.SizeChanged -= RectangleSelection_SizeChanged;
			}
		}

		private void InitEvents(object? sender, RoutedEventArgs? e)
		{
			if (!uiElement.IsLoaded)
			{
				uiElement.Loaded += InitEvents;
			}
			else
			{
				uiElement.Loaded -= InitEvents;
				uiElement.PointerPressed += RectangleSelection_PointerPressed;
				uiElement.PointerReleased += RectangleSelection_PointerReleased;
				uiElement.PointerCaptureLost += RectangleSelection_PointerReleased;
				uiElement.PointerCanceled += RectangleSelection_PointerReleased;

				scrollViewer = DependencyObjectHelpers.FindChild<ScrollViewer>(uiElement, sv => sv.VerticalScrollMode != ScrollMode.Disabled);
				if (scrollViewer is null)
				{
					uiElement.SizeChanged += RectangleSelection_SizeChanged;
				}
			}
		}
	}
}
