// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Files.App.Controls
{
	public partial class SnapPanelContainer : ItemsControl
	{
		private const string TemplatePartName_RootGrid = "PART_RootGrid";
		private const string TemplatePartName_ItemsPresenter = "PART_ItemsPresenter";

		private static readonly DoubleAnimation _snapAnimation = new() { To = 0, Duration = TimeSpan.FromMilliseconds(150), EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut } };

		private Grid? _rootGrid;
		private ItemsPresenter? _itemsPresenter;

		private UIElement? _dragItem;
		private int _dragItemIndex = -1;
		private TranslateTransform? _dragItemTransform;
		private double _dragPointerOffsetX;
		private bool _isDragging;
		private int _vacantItemIndex = -1;

		public SnapPanelContainer()
		{
			DefaultStyleKey = typeof(SnapPanelContainer);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_rootGrid = GetTemplateChild(TemplatePartName_RootGrid) as Grid
				?? throw new MissingFieldException($"Could not find {TemplatePartName_RootGrid} in the given {nameof(SnapPanelContainer)}'s style.");
			_itemsPresenter = GetTemplateChild(TemplatePartName_ItemsPresenter) as ItemsPresenter
				?? throw new MissingFieldException($"Could not find {TemplatePartName_ItemsPresenter} in the given {nameof(SnapPanelContainer)}'s style.");
		}

		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			base.PrepareContainerForItemOverride(element, item);

			if (element is UIElement uiElement)
			{
				uiElement.ManipulationMode = ManipulationModes.TranslateX;
				uiElement.ManipulationStarting += ContentPresenter_ManipulationStarting;
				uiElement.ManipulationDelta += ContentPresenter_ManipulationDelta;
				uiElement.ManipulationCompleted += ContentPresenter_ManipulationCompleted;
				uiElement.PointerPressed += Item_PointerPressed;
			}
		}

		protected override void ClearContainerForItemOverride(DependencyObject element, object item)
		{
			if (element is UIElement uiElement)
			{
				uiElement.ManipulationStarting -= ContentPresenter_ManipulationStarting;
				uiElement.ManipulationDelta -= ContentPresenter_ManipulationDelta;
				uiElement.ManipulationCompleted -= ContentPresenter_ManipulationCompleted;
			}

			base.ClearContainerForItemOverride(element, item);
		}

		private void ContentPresenter_ManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
		{
			if (sender is not UIElement uiElement)
				return;

			if (_dragItem is not null)
				Canvas.SetZIndex(_dragItem, 0);

			_dragItem = uiElement;
			_dragItemIndex = IndexFromContainer(uiElement);
			_vacantItemIndex = _dragItemIndex;

			_dragItemTransform = new();
			uiElement.RenderTransform = _dragItemTransform;

			Canvas.SetZIndex(_dragItem, 100);

			_isDragging = true;

			e.Handled = true;
		}

		private void ContentPresenter_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			if (_dragItem is null || _dragItemIndex is -1 || _dragItemTransform is null)
				return;

			_dragItemTransform.X += e.Delta.Translation.X;

			if (e.Cumulative.Translation.X > 0)
			{
				if (e.Cumulative.Translation.X >= (_dragItem.ActualSize.X - _dragPointerOffsetX))
				{
					// Move the right side item to the position of the item being dragged
					var rightItem = ContainerFromIndex(_dragItemIndex + 1) as UIElement;
					if (rightItem is null)
						return;

					var rightItemTransform = new TranslateTransform();
					rightItem?.RenderTransform = rightItemTransform;
					rightItemTransform.X = -rightItem!.ActualSize.X;

					//_vacantItemIndex++;
				}
			}
			else
			{

			}

			e.Handled = true;
		}

		private void ContentPresenter_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			_isDragging = false;

			e.Handled = true;
		}

		private void Item_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			_dragPointerOffsetX = e.GetCurrentPoint((UIElement)sender).Position.X;
		}
	}
}
