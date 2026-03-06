// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;

namespace Files.App.Controls
{
	public partial class ReorderableItemsControl : ItemsControl
	{
		private bool _isDragging;
		private bool _isSnapping;
		private UIElement? _dragItem;
		private int _dragItemOriginalIndex = -1;
		private TranslateTransform? _dragItemTransform;

		private bool _isHorizontal;
		private double[]? _originalPositions;
		private double[]? _itemExtents;
		private TranslateTransform?[]? _itemTransforms;
		private List<int>? _logicalOrder;
		private Storyboard?[]? _displacementStoryboards;
		private double[]? _displacementTargets;
		private ScrollViewer? _ancestorScrollViewer;

		private const double AutoScrollActivationMargin = 40;
		private const double AutoScrollMaxStep = 18;

		public event EventHandler<ReorderedItemsEventArgs>? Reordered;

		public ReorderableItemsControl()
		{
			DefaultStyleKey = typeof(ReorderableItemsControl);
		}

		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			base.PrepareContainerForItemOverride(element, item);

			if (element is UIElement uiElement)
			{
				uiElement.ManipulationMode = ManipulationModes.System | ManipulationModes.TranslateX | ManipulationModes.TranslateY;
				uiElement.ManipulationStarting += OnItemManipulationStarting;
				uiElement.ManipulationDelta += OnItemManipulationDelta;
				uiElement.ManipulationCompleted += OnItemManipulationCompleted;
			}
		}

		protected override void ClearContainerForItemOverride(DependencyObject element, object item)
		{
			if (element is UIElement uiElement)
			{
				uiElement.ManipulationStarting -= OnItemManipulationStarting;
				uiElement.ManipulationDelta -= OnItemManipulationDelta;
				uiElement.ManipulationCompleted -= OnItemManipulationCompleted;
			}

			base.ClearContainerForItemOverride(element, item);
		}

		private void OnItemManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
		{
			if (sender is not UIElement dragElement || _isSnapping)
				return;

			var itemCount = Items.Count;
			if (itemCount < 2)
				return;

			_ancestorScrollViewer ??= TryFindAncestorScrollViewer(dragElement);

			_isHorizontal = ItemsPanelRoot switch
				{
					StackPanel sp => sp.Orientation is Orientation.Horizontal,
					ResizablePanel rp => rp.Orientation is Orientation.Horizontal,
					_ => false,
				};
				e.Mode = _isHorizontal ? ManipulationModes.TranslateX : ManipulationModes.TranslateY;

			_dragItem = dragElement;
			_dragItemOriginalIndex = IndexFromContainer(dragElement);
			_isDragging = true;

			_originalPositions = new double[itemCount];
			_itemExtents = new double[itemCount];
			_itemTransforms = new TranslateTransform[itemCount];
			_logicalOrder = [.. Enumerable.Range(0, itemCount)];
			_displacementStoryboards = new Storyboard[itemCount];
			_displacementTargets = new double[itemCount];

			double position = 0;
			for (var i = 0; i < itemCount; i++)
			{
				var container = ContainerFromIndex(i) as UIElement;
				_originalPositions[i] = position;
				_itemExtents[i] = _isHorizontal
					? container?.ActualSize.X ?? 0
					: container?.ActualSize.Y ?? 0;

				var transform = new TranslateTransform();
				_itemTransforms[i] = transform;
				container?.RenderTransform = transform;

				position += _itemExtents[i];
			}

			_dragItemTransform = _itemTransforms[_dragItemOriginalIndex];
			Canvas.SetZIndex(_dragItem, 100);

			e.Handled = true;
		}

		private void OnItemManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			if (!_isDragging || _dragItemTransform is null || _logicalOrder is null ||
				_originalPositions is null || _itemExtents is null || _itemTransforms is null)
				return;

			_ = _isHorizontal
				? _dragItemTransform.X += e.Delta.Translation.X
				: _dragItemTransform.Y += e.Delta.Translation.Y;

			if (sender is UIElement senderElement)
				TryAutoScrollDuringDrag(senderElement, e.Position);

			var dragCenterPosition =
				_originalPositions[_dragItemOriginalIndex] +
				_itemExtents[_dragItemOriginalIndex] / 2.0 +
				(_isHorizontal ? _dragItemTransform.X : _dragItemTransform.Y);

			var anySwap = false;
			bool swapped;

			do
			{
				swapped = false;
				var currentPosition = _logicalOrder.IndexOf(_dragItemOriginalIndex);
				var targetPositions = ComputeTargetPositions();

				if (currentPosition < _logicalOrder.Count - 1)
				{
					var nextItemIdx = _logicalOrder[currentPosition + 1];
					var nextMidpoint = targetPositions[nextItemIdx] + _itemExtents[nextItemIdx] / 2.0;

					if (dragCenterPosition > nextMidpoint)
					{
						_logicalOrder[currentPosition] = nextItemIdx;
						_logicalOrder[currentPosition + 1] = _dragItemOriginalIndex;
						swapped = true;
						anySwap = true;
					}
				}

				if (!swapped && currentPosition > 0)
				{
					var previousIndex = _logicalOrder[currentPosition - 1];
					var previousCenterPosition = targetPositions[previousIndex] + _itemExtents[previousIndex] / 2.0;

					if (dragCenterPosition < previousCenterPosition)
					{
						_logicalOrder[currentPosition] = previousIndex;
						_logicalOrder[currentPosition - 1] = _dragItemOriginalIndex;
						swapped = true;
						anySwap = true;
					}
				}
			}
			while (swapped);

			if (anySwap)
				UpdateDisplacedItemTransforms();

			e.Handled = true;
		}

		private void OnItemManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			if (!_isDragging || _dragItemTransform is null || _logicalOrder is null ||
				_originalPositions is null || _itemExtents is null)
				return;

			_isDragging = false;
			_isSnapping = true;

			var targetPositions = ComputeTargetPositions();
			var snapTarget = targetPositions[_dragItemOriginalIndex] - _originalPositions[_dragItemOriginalIndex];

			var snapAnimation = new DoubleAnimation()
			{
				To = snapTarget,
				Duration = new Duration(TimeSpan.FromSeconds(0.25)),
				EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseOut }
			};

			// Exponentially increase the moving speed of the dragged item to its final position
			var storyboard = new Storyboard();
			Storyboard.SetTarget(snapAnimation, _dragItemTransform);
			Storyboard.SetTargetProperty(snapAnimation, _isHorizontal ? "X" : "Y");
			storyboard.Children.Add(snapAnimation);
			storyboard.Completed += OnSnapAnimationCompleted;
			storyboard.Begin();

			e.Handled = true;
		}

		private void OnSnapAnimationCompleted(object? sender, object e)
		{
			// Reset ZIndex of the item that was being dragged
			if (_dragItem is not null)
				Canvas.SetZIndex(_dragItem, 0);

			// Check if the order changed
			var orderChanged = false;
			if (_logicalOrder is not null)
			{
				for (var i = 0; i < _logicalOrder.Count; i++)
				{
					if (_logicalOrder[i] != i)
					{
						orderChanged = true;
						break;
					}
				}
			}

			// Stop any other ongoing displacement animations
			if (_displacementStoryboards is not null)
			{
				foreach (var sb in _displacementStoryboards)
					sb?.Stop();
			}

			// Clear translate transforms
			for (var i = 0; i < Items.Count; i++)
			{
				if (ContainerFromIndex(i) is UIElement container)
					container.RenderTransform = null;
			}

			// Commit reordering if it changed
			if (orderChanged && _logicalOrder is not null)
			{
				int[] reorderedIndexMap = [.. _logicalOrder];
				var committed = ItemsSource is not null
					? TryCommitReorderToItemsSource(reorderedIndexMap)
					: TryCommitReorderToItems(reorderedIndexMap);

				if (committed)
				{
					// If using ResizablePanel, force it to regenerate auto-generated ResizeVisuals
					// for the reordered containers
					if (ItemsPanelRoot is ResizablePanel resizablePanel)
						resizablePanel.InvalidateAutoGeneration();

					Reordered?.Invoke(this, new ReorderedItemsEventArgs(reorderedIndexMap));
				}
			}

			// Reset state
			_dragItem = null;
			_dragItemOriginalIndex = -1;
			_dragItemTransform = null;
			_originalPositions = null;
			_itemExtents = null;
			_itemTransforms = null;
			_logicalOrder = null;
			_displacementStoryboards = null;
			_displacementTargets = null;
			_ancestorScrollViewer = null;
			_isSnapping = false;
		}

		[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Move is discovered dynamically to support ObservableCollection<T> ItemsSource.")]
		private bool TryCommitReorderToItemsSource(int[] reorderedIndexMap)
		{
			if (ItemsSource is not IList itemsSource ||
				itemsSource.IsReadOnly ||
				itemsSource.IsFixedSize ||
				itemsSource.Count != reorderedIndexMap.Length)
				return false;

			// TODO: This uses reflection, consider root the Move method
			var moveMethod = itemsSource.GetType().GetMethod(
				"Move",
				BindingFlags.Instance | BindingFlags.Public,
				null,
				[typeof(int), typeof(int)],
				null);

			var currentOrder = Enumerable.Range(0, reorderedIndexMap.Length).ToList();

			try
			{
				for (var targetIndex = 0; targetIndex < reorderedIndexMap.Length; targetIndex++)
				{
					var desiredOldIndex = reorderedIndexMap[targetIndex];
					var currentIndex = currentOrder.IndexOf(desiredOldIndex);
					if (currentIndex == targetIndex)
						continue;

					if (moveMethod is not null)
					{
						_ = moveMethod.Invoke(itemsSource, [currentIndex, targetIndex]);
					}
					else
					{
						var movedItem = itemsSource[currentIndex];
						itemsSource.RemoveAt(currentIndex);
						itemsSource.Insert(targetIndex, movedItem);
					}

					currentOrder.RemoveAt(currentIndex);
					currentOrder.Insert(targetIndex, desiredOldIndex);
				}

				return true;
			}
			catch (NotSupportedException)
			{
				return false;
			}
			catch (TargetInvocationException ex) when (ex.InnerException is NotSupportedException)
			{
				return false;
			}
		}

		private bool TryCommitReorderToItems(int[] reorderedIndexMap)
		{
			if (ItemsSource is not null || Items.Count != reorderedIndexMap.Length)
				return false;

			var reorderedItems = new object[reorderedIndexMap.Length];
			for (var i = 0; i < reorderedIndexMap.Length; i++)
				reorderedItems[i] = Items[reorderedIndexMap[i]]!;

			Items.Clear();
			foreach (var item in reorderedItems)
				Items.Add(item);

			return true;
		}

		private ScrollViewer? TryFindAncestorScrollViewer(DependencyObject? element)
		{
			while (element is not null)
			{
				if (element is ScrollViewer scrollViewer)
					return scrollViewer;

				element = VisualTreeHelper.GetParent(element);
			}

			return null;
		}

		private void TryAutoScrollDuringDrag(UIElement senderElement, Point pointerPositionInSender)
		{
			if (_ancestorScrollViewer is null || _dragItemTransform is null)
				return;

			var transformToScrollViewer = senderElement.TransformToVisual(_ancestorScrollViewer);
			var pointerInViewport = transformToScrollViewer.TransformPoint(pointerPositionInSender);

			if (_isHorizontal)
			{
				var horizontalDelta = ComputeAutoScrollDelta(
					pointerInViewport.X,
					_ancestorScrollViewer.ViewportWidth,
					_ancestorScrollViewer.HorizontalOffset,
					_ancestorScrollViewer.ScrollableWidth);

				if (horizontalDelta == 0)
					return;

				var newHorizontalOffset = _ancestorScrollViewer.HorizontalOffset + horizontalDelta;
				var didScroll = _ancestorScrollViewer.ChangeView(newHorizontalOffset, null, null, true);
				if (didScroll)
					_dragItemTransform.X += horizontalDelta;
			}
			else
			{
				var verticalDelta = ComputeAutoScrollDelta(
					pointerInViewport.Y,
					_ancestorScrollViewer.ViewportHeight,
					_ancestorScrollViewer.VerticalOffset,
					_ancestorScrollViewer.ScrollableHeight);

				if (verticalDelta == 0)
					return;

				var newVerticalOffset = _ancestorScrollViewer.VerticalOffset + verticalDelta;
				var didScroll = _ancestorScrollViewer.ChangeView(null, newVerticalOffset, null, true);
				if (didScroll)
					_dragItemTransform.Y += verticalDelta;
			}
		}

		private static double ComputeAutoScrollDelta(double pointerPosition, double viewportSize, double currentOffset, double scrollableSize)
		{
			if (viewportSize <= 0 || scrollableSize <= 0)
				return 0;

			double delta = 0;

			if (pointerPosition < AutoScrollActivationMargin && currentOffset > 0)
			{
				var overscroll = AutoScrollActivationMargin - pointerPosition;
				var strength = Math.Clamp(overscroll / AutoScrollActivationMargin, 0, 1);
				delta = -Math.Clamp(strength * AutoScrollMaxStep, 1, AutoScrollMaxStep);
			}
			else if (pointerPosition > viewportSize - AutoScrollActivationMargin && currentOffset < scrollableSize)
			{
				var overscroll = pointerPosition - (viewportSize - AutoScrollActivationMargin);
				var strength = Math.Clamp(overscroll / AutoScrollActivationMargin, 0, 1);
				delta = Math.Clamp(strength * AutoScrollMaxStep, 1, AutoScrollMaxStep);
			}

			if (delta is 0)
				return 0;

			var targetOffset = Math.Clamp(currentOffset + delta, 0, scrollableSize);
			return targetOffset - currentOffset;
		}

		private double[] ComputeTargetPositions()
		{
			var count = _logicalOrder!.Count;
			var positions = new double[count];
			double position = 0;

			for (var i = 0; i < count; i++)
			{
				var itemIndex = _logicalOrder[i];
				positions[itemIndex] = position;
				position += _itemExtents![itemIndex];
			}

			return positions;
		}

		private void UpdateDisplacedItemTransforms()
		{
			var targetPositions = ComputeTargetPositions();

			for (var i = 0; i < _logicalOrder!.Count; i++)
			{
				var itemIndex = _logicalOrder[i];
				if (itemIndex == _dragItemOriginalIndex)
					continue;

				var transform = _itemTransforms![itemIndex];
				if (transform is null)
					continue;

				var target = targetPositions[itemIndex] - _originalPositions![itemIndex];

				if (_displacementTargets![itemIndex] == target)
					continue;

				var previousStoryboard = _displacementStoryboards![itemIndex];
				if (previousStoryboard is not null)
				{
					previousStoryboard.Stop();

					var delta = _displacementTargets[itemIndex];
					_ = _isHorizontal ? transform.X = delta : transform.Y = delta;
				}

				_displacementTargets[itemIndex] = target;

				var animation = new DoubleAnimation()
				{
					To = target,
					Duration = new Duration(TimeSpan.FromSeconds(0.25)),
					EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseOut }
				};

				var storyboard = new Storyboard();
				Storyboard.SetTarget(animation, transform);
				Storyboard.SetTargetProperty(animation, _isHorizontal ? "X" : "Y");
				storyboard.Children.Add(animation);
				_displacementStoryboards[itemIndex] = storyboard;
				storyboard.Begin();
			}
		}
	}
}
