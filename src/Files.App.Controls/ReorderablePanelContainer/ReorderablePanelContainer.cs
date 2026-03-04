// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Files.App.Controls
{
	public partial class ReorderablePanelContainer : ItemsControl
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

		public ReorderablePanelContainer()
		{
			DefaultStyleKey = typeof(ReorderablePanelContainer);
		}

		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			base.PrepareContainerForItemOverride(element, item);

			if (element is UIElement uiElement)
			{
				uiElement.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
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

			_isHorizontal = ItemsPanelRoot is StackPanel { Orientation: Orientation.Horizontal };
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
			if (orderChanged)
			{
				if (_logicalOrder is null)
					return;

				object[] reorderedItemsArray = [.. _logicalOrder.Select(i => Items[i])];
				Items.Clear();

				foreach (var item in reorderedItemsArray)
					Items.Add(item);
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
			_isSnapping = false;
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
