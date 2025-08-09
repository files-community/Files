// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Controls
{
	public partial class ResizeVisual
	{
		private void ResizeVisual_Loaded(object sender, RoutedEventArgs e)
		{
			OnTargetChanged(Target);
			OnCursorShapeChanged(CursorShape);
			OnOrientationChanged(Orientation);
			OnThumbHeightChanged(ThumbHeight);
			OnThumbWidthChanged(ThumbWidth);
		}

		private void RootGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			if (_outerThumb is not null && !_isDragging)
			{
				VisualStateManager.GoToState(this, "Visible", true);
			}
		}

		private void RootGrid_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			if (_outerThumb is not null && !_isDragging)
			{
				VisualStateManager.GoToState(this, "Collapsed", true);
			}
		}

		private void OuterThumb_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			_pointerExited = false;
		}

		private void OuterThumb_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			_pointerExited = true;
		}

		private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
		{
			_isDragging = true;
			DragStarted?.Invoke(this, e);
		}

		private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
		{
			if (Target is not null)
			{
				if (Orientation is Orientation.Vertical)
				{
					var delta = e.VerticalChange;

					if (!_reachedToBounds && IsValidHeight(Target, Target.Height + delta, ActualHeight))
					{
						Target.Height += delta;
					}
					else
					{
						// Snap to the bounds if the delta is outside the bounds
						if (!_reachedToBounds)
							Target.Height = delta < 0D ? Target.MinHeight : Target.MaxHeight;

						_reachedToBounds = true;
						_deltaOutsideBounds += delta;

						if (_deltaOutsideBounds >= 0D)
							_reachedToBounds = false;
					}

					((TranslateTransform)RenderTransform).Y = Target.Height - ActualHeight / 2;
				}
				else if (Orientation is Orientation.Horizontal)
				{
					var delta = e.HorizontalChange;

					if (Target.FlowDirection is FlowDirection.RightToLeft) delta = -delta;
					if (IsDragInverted) delta = -delta;

					if (!_reachedToBounds && IsValidWidth(Target, Target.Width + delta, ActualWidth))
					{
						Target.Width += delta;
					}
					else
					{
						// Snap to the bounds if the delta is outside the bounds
						if (!_reachedToBounds)
							Target.Width = delta < 0D ? Target.MinWidth : Target.MaxWidth;

						_reachedToBounds = true;
						_deltaOutsideBounds += delta;

						if (_deltaOutsideBounds >= 0D)
							_reachedToBounds = false;
					}

					((TranslateTransform)RenderTransform).X = Target.Width - ActualWidth / 2;
				}
				else
				{
					throw new ArgumentOutOfRangeException(nameof(Orientation), $"The value of the argument \"{Orientation}\" was invalid.");
				}
			}

			DragDelta?.Invoke(this, e);
		}

		private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
		{
			_isDragging = false;
			DragCompleted?.Invoke(this, e);

			if (_outerThumb is not null && _pointerExited)
			{
				VisualStateManager.GoToState(this, "Collapsed", true);
				_pointerExited = false;
			}
		}

		private void ResizeVisual_Unloaded(object sender, RoutedEventArgs e)
		{
			Loaded -= ResizeVisual_Loaded;
			Unloaded -= ResizeVisual_Unloaded;

			if (_rootGrid is not null)
			{
				_rootGrid.PointerEntered -= RootGrid_PointerEntered;
				_rootGrid.PointerExited -= RootGrid_PointerExited;
			}

			if (_outerThumb is not null)
			{
				_outerThumb.PointerEntered -= OuterThumb_PointerEntered;
				_outerThumb.PointerExited -= OuterThumb_PointerExited;
				_outerThumb.PointerExited -= OuterThumb_PointerExited;

				_outerThumb.DragStarted -= Thumb_DragStarted;
				_outerThumb.DragDelta -= Thumb_DragDelta;
				_outerThumb.DragCompleted -= Thumb_DragCompleted;
			}
		}
	}
}
