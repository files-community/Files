// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.System;

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

		public DragSelectionContainer()
		{
			DefaultStyleKey = typeof(DragSelectionContainer);
			Targets = [];
			_dispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().CreateTimer();
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
				var modifiers when modifiers.HasFlag(VirtualKeyModifiers.Control) => DragSelectionKind.InvertPreviousSelection,
				var modifiers when modifiers.HasFlag(VirtualKeyModifiers.Shift) => DragSelectionKind.ExtendPreviousSelection,
				_ => DragSelectionKind.IgnorePreviousSelection,
			};

			_contentPresenter.PointerMoved -= ContentPresenter_PointerMoved;
			_contentPresenter.PointerMoved += ContentPresenter_PointerMoved;

			//if (_selectionKind is DragSelectionKind.IgnorePreviousSelection)
			//{
			//	foreach (var target in Targets)
			//		target.Target.SelectedItems.Clear();
			//}

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

			// When the drag just started, ignore small movements to avoid accidental selections
			if (_selectionState is DragSelectionState.Started &&
				(Math.Abs(_dragStartPoint.X - _dragCurrentPoint.X) < 5 ||
				Math.Abs(_dragStartPoint.Y - _dragCurrentPoint.Y) < 5))
				return;

			_selectionState = DragSelectionState.Moving;

			// Draw a rectangle visual
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

			e.Handled = true;
		}
	}
}
