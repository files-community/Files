// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Controls
{
	public partial class ResizeVisual
	{
		[GeneratedDependencyProperty]
		public partial FrameworkElement? Target { get; set; }

		[GeneratedDependencyProperty]
		public partial InputSystemCursorShape CursorShape { get; set; }

		[GeneratedDependencyProperty(DefaultValue = Orientation.Vertical)]
		public partial Orientation Orientation { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsDragInverted { get; set; }

		[GeneratedDependencyProperty]
		public partial bool FollowPointer { get; set; }

		[GeneratedDependencyProperty]
		public partial double ThumbHeight { get; set; }

		[GeneratedDependencyProperty]
		public partial double ThumbWidth { get; set; }

		partial void OnTargetChanged(FrameworkElement? newValue)
		{
			RenderTransform = new TranslateTransform();

			if (Orientation is Orientation.Vertical)
			{
				((TranslateTransform)RenderTransform).Y = newValue?.Height - ActualHeight / 2 ?? 0;
			}
			else if (Orientation is Orientation.Horizontal)
			{
				((TranslateTransform)RenderTransform).X = newValue?.Width - ActualWidth / 2 ?? 0;
			}
		}

		partial void OnCursorShapeChanged(InputSystemCursorShape newValue)
		{
			if (_outerThumb is not null)
			{
				_outerThumb.ChangeCursor(InputSystemCursor.Create(newValue));
			}
		}

		partial void OnOrientationChanged(Orientation newValue)
		{
			if (newValue is Orientation.Vertical)
				CursorShape = InputSystemCursorShape.SizeNorthSouth;
			else if (newValue is Orientation.Horizontal)
				CursorShape = InputSystemCursorShape.SizeWestEast;

			// Reset the translation
			if (Target is not null && _outerThumb is not null)
			{
				_outerThumb.VerticalAlignment = VerticalAlignment.Stretch;
				_outerThumb.HorizontalAlignment = HorizontalAlignment.Stretch;

				if (newValue is Orientation.Vertical)
				{
					_outerThumb.HorizontalAlignment = HorizontalAlignment.Center;
					((TranslateTransform)RenderTransform).X = 0;
					((TranslateTransform)RenderTransform).Y = Target.Height - ActualHeight / 2;
				}
				else if (newValue is Orientation.Horizontal)
				{
					_outerThumb.VerticalAlignment = VerticalAlignment.Center;
					((TranslateTransform)RenderTransform).X = Target.Width - ActualWidth / 2;
					((TranslateTransform)RenderTransform).Y = 0;
				}
			}
		}

		partial void OnThumbHeightChanged(double newValue)
		{
			if (_outerThumb is not null)
				_outerThumb.Height = newValue;
		}

		partial void OnThumbWidthChanged(double newValue)
		{
			if (_outerThumb is not null)
				_outerThumb.Width = newValue;
		}
	}
}
