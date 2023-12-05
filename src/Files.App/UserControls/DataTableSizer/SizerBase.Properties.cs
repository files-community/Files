// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Collections.Specialized;
using Windows.ApplicationModel.DataTransfer;
using CursorEnum = Microsoft.UI.Input.InputSystemCursorShape;

namespace Files.App.UserControls.DataTableSizer
{
	public partial class SizerBase
	{
		public CursorEnum Cursor
		{
			get { return (CursorEnum)GetValue(CursorProperty); }
			set { SetValue(CursorProperty, value); }
		}

		public static readonly DependencyProperty CursorProperty =
			DependencyProperty.Register(nameof(Cursor), typeof(CursorEnum), typeof(SizerBase), new PropertyMetadata(null, OnOrientationPropertyChanged));

		public double DragIncrement
		{
			get { return (double)GetValue(DragIncrementProperty); }
			set { SetValue(DragIncrementProperty, value); }
		}

		public static readonly DependencyProperty DragIncrementProperty =
			DependencyProperty.Register(nameof(DragIncrement), typeof(double), typeof(SizerBase), new PropertyMetadata(1d));

		public double KeyboardIncrement
		{
			get { return (double)GetValue(KeyboardIncrementProperty); }
			set { SetValue(KeyboardIncrementProperty, value); }
		}

		public static readonly DependencyProperty KeyboardIncrementProperty =
			DependencyProperty.Register(nameof(KeyboardIncrement), typeof(double), typeof(SizerBase), new PropertyMetadata(8d));

		public Orientation Orientation
		{
			get { return (Orientation)GetValue(OrientationProperty); }
			set { SetValue(OrientationProperty, value); }
		}

		public static readonly DependencyProperty OrientationProperty =
			DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(SizerBase), new PropertyMetadata(Orientation.Vertical, OnOrientationPropertyChanged));

		public bool IsThumbVisible
		{
			get { return (bool)GetValue(IsThumbVisibleProperty); }
			set { SetValue(IsThumbVisibleProperty, value); }
		}

		public static readonly DependencyProperty IsThumbVisibleProperty =
			DependencyProperty.Register(nameof(IsThumbVisible), typeof(bool), typeof(SizerBase), new PropertyMetadata(true, OnIsThumbVisiblePropertyChanged));


		private static void OnOrientationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is SizerBase gripper)
			{
				VisualStateManager.GoToState(gripper, gripper.Orientation == Orientation.Vertical ? VerticalState : HorizontalState, true);

				CursorEnum cursorByOrientation = gripper.Orientation == Orientation.Vertical ? CursorEnum.SizeWestEast : CursorEnum.SizeNorthSouth;

				// See if there's been a cursor override, otherwise we'll pick
				var cursor = gripper.ReadLocalValue(CursorProperty);
				if (cursor == DependencyProperty.UnsetValue || cursor == null)
				{
					cursor = cursorByOrientation;
				}

				// TODO: [UNO] Only supported on certain platforms
				// See ProtectedCursor here: https://github.com/unoplatform/uno/blob/3fe3862b270b99dbec4d830b547942af61b1a1d9/src/Uno.UI/UI/Xaml/UIElement.cs#L1015-L1023
				// Need to wait until we're at least applying template step of loading before setting Cursor
				// See https://github.com/microsoft/microsoft-ui-xaml/issues/7062
				if (gripper._appliedTemplate &&
					cursor is CursorEnum cursorValue &&
					(gripper.ProtectedCursor == null ||
						(gripper.ProtectedCursor is InputSystemCursor current &&
						 current.CursorShape != cursorValue)))
				{
					gripper.ProtectedCursor = InputSystemCursor.Create(cursorValue);
				}
			}
		}

		private static void OnIsThumbVisiblePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is SizerBase gripper)
			{
				VisualStateManager.GoToState(gripper, gripper.IsThumbVisible ? VisibleState : CollapsedState, true);
			}
		}
	}
}
