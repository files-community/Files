// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Input;

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
			if (_observedTarget is not null)
				_observedTarget.SizeChanged -= Target_SizeChanged;

			_observedTarget = newValue;

			if (_observedTarget is not null)
				_observedTarget.SizeChanged += Target_SizeChanged;

			UpdateThumbTranslation();
		}

		partial void OnCursorShapeChanged(InputSystemCursorShape newValue)
		{
			this.ChangeCursor(InputSystemCursor.Create(newValue));
		}

		partial void OnOrientationChanged(Orientation newValue)
		{
			if (newValue is Orientation.Vertical)
				CursorShape = InputSystemCursorShape.SizeNorthSouth;
			else if (newValue is Orientation.Horizontal)
				CursorShape = InputSystemCursorShape.SizeWestEast;

			if (_outerThumb is not null)
			{
				_outerThumb.VerticalAlignment = VerticalAlignment.Stretch;
				_outerThumb.HorizontalAlignment = HorizontalAlignment.Stretch;

				if (newValue is Orientation.Vertical)
				{
					_outerThumb.HorizontalAlignment = HorizontalAlignment.Center;
				}
				else if (newValue is Orientation.Horizontal)
				{
					_outerThumb.VerticalAlignment = VerticalAlignment.Center;
				}
			}

			UpdateThumbTranslation();
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
