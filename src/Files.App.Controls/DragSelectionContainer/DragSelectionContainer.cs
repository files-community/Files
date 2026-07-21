// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace Files.App.Controls
{
	/// <summary>
	/// Represents a container control that enables drag-based selection of ListView and GridView.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This control supports different selection strategies through the <see cref="DragSelectionKind"/> enumeration:
	/// <list type="bullet">
	/// <item><term>Ctrl modifier pressed</term><description>it inverts the previous selection;</description></item>
	/// <item><term>Shift modifier pressed</term><description>it extends the previous selection;</description></item>
	/// <item><term>No modifiers pressed</term><description>it ignores the previous selection and starts a new selection</description></item>
	/// </list>
	/// </para>
	/// <para>
	/// Additionally, this control provides events to notify when a selection operation starts and ends:
	/// </para>
	/// </remarks>
	[ContentProperty(Name = nameof(Content))]
	public partial class DragSelectionContainer : ContentControl
	{
		private const string TemplatePartName_ContentPresenter = "PART_ContentPresenter";
		private const string TemplatePartName_SelectionRectangle = "PART_SelectionRectangle";

		private ContentPresenter? _contentPresenter;
		private Rectangle? _selectionRectangle;

		private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _dispatcherQueueTimer;
		private Point _dragStartPoint;
		private Point _dragCurrentPoint;
		private DragSelectionKind _selectionKind;
		private DragSelectionState _selectionState;
		private Dictionary<FrameworkElement, DragSelectionItemPositionCacheEntry> _positionOfItems;

		public DragSelectionContainer()
		{
			DefaultStyleKey = typeof(DragSelectionContainer);
			Targets = [];
			_dispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().CreateTimer();
			_positionOfItems = [];
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_contentPresenter = GetTemplateChild(TemplatePartName_ContentPresenter) as ContentPresenter
				?? throw new MissingFieldException($"Could not find {TemplatePartName_ContentPresenter} in the given {nameof(DragSelectionContainer)}'s style.");
			_selectionRectangle = GetTemplateChild(TemplatePartName_SelectionRectangle) as Rectangle
				?? throw new MissingFieldException($"Could not find {TemplatePartName_SelectionRectangle} in the given {nameof(DragSelectionContainer)}'s style.");

			_contentPresenter.PointerPressed += ContentPresenter_PointerPressed;
			_contentPresenter.PointerReleased += ContentPresenter_PointerReleased;
		}

		protected void DrawRectangleOnCanvas()
		{
			if (_contentPresenter is null || _selectionRectangle is null)
				return;

			if (_dragCurrentPoint.X >= _dragStartPoint.X)
			{
				double maxWidth = _contentPresenter.ActualWidth - _dragStartPoint.X;
				if (_dragCurrentPoint.Y <= _dragStartPoint.Y)
				{
					// Moved up and right
					Canvas.SetLeft(_selectionRectangle, Math.Max(0, _dragStartPoint.X));
					Canvas.SetTop(_selectionRectangle, Math.Max(0, _dragCurrentPoint.Y));
					_selectionRectangle.Width = Math.Max(0, Math.Min(_dragCurrentPoint.X - Math.Max(0, _dragStartPoint.X), maxWidth));
					_selectionRectangle.Height = Math.Max(0, _dragStartPoint.Y - Math.Max(0, _dragCurrentPoint.Y));
				}
				else
				{
					// Moved down and right
					Canvas.SetLeft(_selectionRectangle, Math.Max(0, _dragStartPoint.X));
					Canvas.SetTop(_selectionRectangle, Math.Max(0, _dragStartPoint.Y));
					_selectionRectangle.Width = Math.Max(0, Math.Min(_dragCurrentPoint.X - Math.Max(0, _dragStartPoint.X), maxWidth));
					_selectionRectangle.Height = Math.Max(0, _dragCurrentPoint.Y - Math.Max(0, _dragStartPoint.Y));
				}
			}
			else
			{
				if (_dragCurrentPoint.Y <= _dragStartPoint.Y)
				{
					// Moved up and left
					Canvas.SetLeft(_selectionRectangle, Math.Max(0, _dragCurrentPoint.X));
					Canvas.SetTop(_selectionRectangle, Math.Max(0, _dragCurrentPoint.Y));
					_selectionRectangle.Width = Math.Max(0, _dragStartPoint.X - Math.Max(0, _dragCurrentPoint.X));
					_selectionRectangle.Height = Math.Max(0, _dragStartPoint.Y - Math.Max(0, _dragCurrentPoint.Y));
				}
				else
				{
					// Moved down and left
					Canvas.SetLeft(_selectionRectangle, Math.Max(0, _dragCurrentPoint.X));
					Canvas.SetTop(_selectionRectangle, Math.Max(0, _dragStartPoint.Y));
					_selectionRectangle.Width = Math.Max(0, _dragStartPoint.X - Math.Max(0, _dragCurrentPoint.X));
					_selectionRectangle.Height = Math.Max(0, _dragCurrentPoint.Y - Math.Max(0, _dragStartPoint.Y));
				}
			}
		}

		protected void GetPositionOfItemsInViewport()
		{
			// Retrieve position of items shown in the current view
			foreach (var target in Targets)
			{
				foreach (var item in target.Target.Items)
				{
					var obj = target.Target.ContainerFromItem(item);
					if (obj is not FrameworkElement frameworkElement)
						return;

					var transform = frameworkElement.TransformToVisual(_contentPresenter);
					var itemTopLeftPoint = transform.TransformPoint(new(0, 0));
					var itemRect = new Rect(itemTopLeftPoint, new Size(frameworkElement.ActualWidth, frameworkElement.ActualHeight));

					// Save the position until the selection ends
					_positionOfItems[frameworkElement] = new(target.Target, itemRect);
				}
			}
		}
	}
}
