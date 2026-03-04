// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Files.App.Controls
{
	public partial class ReorderableStackPanelContainer : ItemsControl
	{
		private bool _isDragging;
		private bool _isSnapping;
		private UIElement? _dragItem;
		private int _dragItemOriginalIndex = -1;
		private TranslateTransform? _dragItemTransform;

		private double[]? _originalPositions;
		private double[]? _itemWidths;
		private TranslateTransform?[]? _itemTransforms;
		private List<int>? _logicalOrder;
		private Storyboard?[]? _displacementStoryboards;
		private double[]? _displacementTargets;

		public ReorderableStackPanelContainer()
		{
			DefaultStyleKey = typeof(ReorderableStackPanelContainer);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
		}

		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			base.PrepareContainerForItemOverride(element, item);

			if (element is UIElement uiElement)
			{
				uiElement.ManipulationMode = ManipulationModes.TranslateX;
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

			_dragItem = dragElement;
			_dragItemOriginalIndex = IndexFromContainer(dragElement);
			_isDragging = true;

			_originalPositions = new double[itemCount];
			_itemWidths = new double[itemCount];
			_itemTransforms = new TranslateTransform[itemCount];
			_logicalOrder = Enumerable.Range(0, itemCount).ToList();
			_displacementStoryboards = new Storyboard[itemCount];
			_displacementTargets = new double[itemCount];

			double x = 0;
			for (var i = 0; i < itemCount; i++)
			{
				var container = ContainerFromIndex(i) as UIElement;
				_originalPositions[i] = x;
				_itemWidths[i] = container?.ActualSize.X ?? 0;

				var transform = new TranslateTransform();
				_itemTransforms[i] = transform;
				if (container is not null)
					container.RenderTransform = transform;

				x += _itemWidths[i];
			}

			_dragItemTransform = _itemTransforms[_dragItemOriginalIndex];
			Canvas.SetZIndex(_dragItem, 100);

			e.Handled = true;
		}

		private void OnItemManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			if (!_isDragging || _dragItemTransform is null || _logicalOrder is null ||
				_originalPositions is null || _itemWidths is null || _itemTransforms is null)
				return;

			_dragItemTransform.X += e.Delta.Translation.X;

			var dragCenter =
				_originalPositions[_dragItemOriginalIndex] +
				_itemWidths[_dragItemOriginalIndex] / 2.0 +
				_dragItemTransform.X;

			var anySwap = false;
			bool swapped;

			do
			{
				swapped = false;
				var curPos = _logicalOrder.IndexOf(_dragItemOriginalIndex);
				var targetPositions = ComputeTargetPositions();

				if (curPos < _logicalOrder.Count - 1)
				{
					var rightItemIdx = _logicalOrder[curPos + 1];
					var rightMidpoint = targetPositions[rightItemIdx] + _itemWidths[rightItemIdx] / 2.0;

					if (dragCenter > rightMidpoint)
					{
						_logicalOrder[curPos] = rightItemIdx;
						_logicalOrder[curPos + 1] = _dragItemOriginalIndex;
						swapped = true;
						anySwap = true;
					}
				}

				if (!swapped && curPos > 0)
				{
					var leftItemIdx = _logicalOrder[curPos - 1];
					var leftMidpoint = targetPositions[leftItemIdx] + _itemWidths[leftItemIdx] / 2.0;

					if (dragCenter < leftMidpoint)
					{
						_logicalOrder[curPos] = leftItemIdx;
						_logicalOrder[curPos - 1] = _dragItemOriginalIndex;
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
				_originalPositions is null || _itemWidths is null)
				return;

			_isDragging = false;
			_isSnapping = true;

			var targetPositions = ComputeTargetPositions();
			var targetX = targetPositions[_dragItemOriginalIndex] - _originalPositions[_dragItemOriginalIndex];

			var snapAnimation = new DoubleAnimation
			{
				To = targetX,
				Duration = new Duration(TimeSpan.FromSeconds(0.25)),
				EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut }
			};

			var storyboard = new Storyboard();
			Storyboard.SetTarget(snapAnimation, _dragItemTransform);
			Storyboard.SetTargetProperty(snapAnimation, "X");
			storyboard.Children.Add(snapAnimation);
			storyboard.Completed += OnSnapAnimationCompleted;
			storyboard.Begin();

			e.Handled = true;
		}

		private void OnSnapAnimationCompleted(object? sender, object e)
		{
			if (_dragItem is not null)
				Canvas.SetZIndex(_dragItem, 0);

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

			if (_displacementStoryboards is not null)
			{
				foreach (var sb in _displacementStoryboards)
					sb?.Stop();
			}

			for (var i = 0; i < Items.Count; i++)
			{
				if (ContainerFromIndex(i) is UIElement container)
					container.RenderTransform = null;
			}

			if (orderChanged)
				CommitReorder();

			_dragItem = null;
			_dragItemOriginalIndex = -1;
			_dragItemTransform = null;
			_originalPositions = null;
			_itemWidths = null;
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
			double x = 0;

			for (var i = 0; i < count; i++)
			{
				var itemIdx = _logicalOrder[i];
				positions[itemIdx] = x;
				x += _itemWidths![itemIdx];
			}

			return positions;
		}

		private void UpdateDisplacedItemTransforms()
		{
			var targetPositions = ComputeTargetPositions();

			for (var i = 0; i < _logicalOrder!.Count; i++)
			{
				var itemIdx = _logicalOrder[i];
				if (itemIdx == _dragItemOriginalIndex)
					continue;

				var transform = _itemTransforms![itemIdx];
				if (transform is null)
					continue;

				var targetX = targetPositions[itemIdx] - _originalPositions![itemIdx];

				if (_displacementTargets![itemIdx] == targetX)
					continue;

				var prevSb = _displacementStoryboards![itemIdx];
				if (prevSb is not null)
				{
					prevSb.Stop();
					transform.X = _displacementTargets[itemIdx];
				}

				_displacementTargets[itemIdx] = targetX;

				var animation = new DoubleAnimation
				{
					To = targetX,
					Duration = new Duration(TimeSpan.FromSeconds(0.25)),
					EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut }
				};

				var storyboard = new Storyboard();
				Storyboard.SetTarget(animation, transform);
				Storyboard.SetTargetProperty(animation, "X");
				storyboard.Children.Add(animation);
				_displacementStoryboards[itemIdx] = storyboard;
				storyboard.Begin();
			}
		}

		private void CommitReorder()
		{
			if (_logicalOrder is null)
				return;

			var reordered = _logicalOrder.Select(i => Items[i]).ToArray();
			Items.Clear();

			foreach (var item in reordered)
				Items.Add(item);
		}
	}
}
