// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;

namespace Files.App.Controls
{
	public partial class ResizeVisual
	{
		private void ApplyCurrentProperties()
		{
			OnTargetChanged(Target);
			OnCursorShapeChanged(CursorShape);
			OnOrientationChanged(Orientation);
			UpdateThumbTranslation();
		}

		private void ResizeVisual_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			UpdateThumbTranslation();
		}

		private void Target_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (!_isDragging || !FollowPointer)
				UpdateThumbTranslation();
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

		private void OuterThumb_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			if (Target is null)
				return;

			if (Orientation is Orientation.Vertical)
				Target.Height = double.NaN;
			else
				Target.Width = double.NaN;

			UpdateThumbTranslation();
			e.Handled = true;
		}

		private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
		{
			_isDragging = true;
			_pointerExited = false;
			_originalTargetWidth = Target?.Width ?? double.NaN;
			_originalTargetHeight = Target?.Height ?? double.NaN;
			DragStarted?.Invoke(this, e);
		}

		private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
		{
			if (Target is not null)
			{
				if (Orientation is Orientation.Vertical)
				{
					var delta = e.VerticalChange;
					var currentHeight = double.IsNaN(Target.Height) ? Target.ActualHeight : Target.Height;
					Target.Height = Math.Clamp(currentHeight + delta, Target.MinHeight, Target.MaxHeight);

					var transform = EnsureTranslateTransform();
					transform.Y = FollowPointer
						? transform.Y + delta
						: Target.Height - ActualHeight / 2;
				}
				else if (Orientation is Orientation.Horizontal)
				{
					var delta = e.HorizontalChange;

					if (Target.FlowDirection is FlowDirection.RightToLeft) delta = -delta;
					if (IsDragInverted) delta = -delta;

					var currentWidth = double.IsNaN(Target.Width) ? Target.ActualWidth : Target.Width;
					Target.Width = Math.Clamp(currentWidth + delta, Target.MinWidth, Target.MaxWidth);

					var transform = EnsureTranslateTransform();
					transform.X = FollowPointer
						? transform.X + delta
						: Target.Width - ActualWidth / 2;
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
			if (e.Canceled && Target is not null)
			{
				if (Orientation is Orientation.Vertical)
					Target.Height = _originalTargetHeight;
				else
					Target.Width = _originalTargetWidth;
			}

			UpdateThumbTranslation();
			DragCompleted?.Invoke(this, e);

			if (_outerThumb is not null && _pointerExited)
			{
				VisualStateManager.GoToState(this, "Collapsed", true);
				_pointerExited = false;
			}
		}

	}
}
