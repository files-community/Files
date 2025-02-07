// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Controls
{
	/// <summary>
	/// Represents the control that redistributes space between columns or rows of a Grid control.
	/// </summary>
	public partial class GridSplitter
	{
		/// <summary>
		/// Identifies the <see cref="Element"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ElementProperty
			= DependencyProperty.Register(
				nameof(Element),
				typeof(UIElement),
				typeof(GridSplitter),
				new PropertyMetadata(default(UIElement), OnElementPropertyChanged));

		/// <summary>
		/// Identifies the <see cref="ResizeDirection"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ResizeDirectionProperty
			= DependencyProperty.Register(
				nameof(ResizeDirection),
				typeof(GridResizeDirection),
				typeof(GridSplitter),
				new PropertyMetadata(GridResizeDirection.Auto));

		/// <summary>
		/// Identifies the <see cref="ResizeBehavior"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ResizeBehaviorProperty
			= DependencyProperty.Register(
				nameof(ResizeBehavior),
				typeof(GridResizeBehavior),
				typeof(GridSplitter),
				new PropertyMetadata(GridResizeBehavior.BasedOnAlignment));

		/// <summary>
		/// Identifies the <see cref="GripperForeground"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty GripperForegroundProperty
			= DependencyProperty.Register(
				nameof(GripperForeground),
				typeof(Brush),
				typeof(GridSplitter),
				new PropertyMetadata(default(Brush), OnGripperForegroundPropertyChanged));

		/// <summary>
		/// Identifies the <see cref="ParentLevel"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ParentLevelProperty
			= DependencyProperty.Register(
				nameof(ParentLevel),
				typeof(int),
				typeof(GridSplitter),
				new PropertyMetadata(default(int)));

		/// <summary>
		/// Identifies the <see cref="GripperCursor"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty GripperCursorProperty =
			DependencyProperty.RegisterAttached(
				nameof(GripperCursor),
				typeof(InputSystemCursorShape?),
				typeof(GridSplitter),
				new PropertyMetadata(GripperCursorType.Default, OnGripperCursorPropertyChanged));

		/// <summary>
		/// Identifies the <see cref="GripperCustomCursorResource"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty GripperCustomCursorResourceProperty =
			DependencyProperty.RegisterAttached(
				nameof(GripperCustomCursorResource),
				typeof(uint),
				typeof(GridSplitter),
				new PropertyMetadata(GripperCustomCursorDefaultResource, GripperCustomCursorResourcePropertyChanged));

		/// <summary>
		/// Identifies the <see cref="CursorBehavior"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty CursorBehaviorProperty =
			DependencyProperty.RegisterAttached(
				nameof(CursorBehavior),
				typeof(SplitterCursorBehavior),
				typeof(GridSplitter),
				new PropertyMetadata(SplitterCursorBehavior.ChangeOnSplitterHover, CursorBehaviorPropertyChanged));

		/// <summary>
		/// Gets or sets the visual content of this Grid Splitter
		/// </summary>
		public UIElement Element
		{
			get { return (UIElement)GetValue(ElementProperty); }
			set { SetValue(ElementProperty, value); }
		}

		/// <summary>
		/// Gets or sets whether the Splitter resizes the Columns, Rows, or Both.
		/// </summary>
		public GridResizeDirection ResizeDirection
		{
			get { return (GridResizeDirection)GetValue(ResizeDirectionProperty); }

			set { SetValue(ResizeDirectionProperty, value); }
		}

		/// <summary>
		/// Gets or sets which Columns or Rows the Splitter resizes.
		/// </summary>
		public GridResizeBehavior ResizeBehavior
		{
			get { return (GridResizeBehavior)GetValue(ResizeBehaviorProperty); }

			set { SetValue(ResizeBehaviorProperty, value); }
		}

		/// <summary>
		/// Gets or sets the foreground color of grid splitter grip
		/// </summary>
		public Brush GripperForeground
		{
			get { return (Brush)GetValue(GripperForegroundProperty); }

			set { SetValue(GripperForegroundProperty, value); }
		}

		/// <summary>
		/// Gets or sets the level of the parent grid to resize
		/// </summary>
		public int ParentLevel
		{
			get { return (int)GetValue(ParentLevelProperty); }

			set { SetValue(ParentLevelProperty, value); }
		}

		/// <summary>
		/// Gets or sets the gripper Cursor type
		/// </summary>
		public GripperCursorType GripperCursor
		{
			get { return (GripperCursorType)GetValue(GripperCursorProperty); }
			set { SetValue(GripperCursorProperty, value); }
		}

		/// <summary>
		/// Gets or sets the gripper Custom Cursor resource number
		/// </summary>
		public int GripperCustomCursorResource
		{
			get { return (int)GetValue(GripperCustomCursorResourceProperty); }
			set { SetValue(GripperCustomCursorResourceProperty, value); }
		}

		/// <summary>
		/// Gets or sets splitter cursor on hover behavior
		/// </summary>
		public SplitterCursorBehavior CursorBehavior
		{
			get { return (SplitterCursorBehavior)GetValue(CursorBehaviorProperty); }
			set { SetValue(CursorBehaviorProperty, value); }
		}

		private static void OnGripperForegroundPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var gridSplitter = (GridSplitter)d;

			if (gridSplitter._gripperDisplay == null)
			{
				return;
			}

			gridSplitter._gripperDisplay.Foreground = gridSplitter.GripperForeground;
		}

		private static void OnGripperCursorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var gridSplitter = (GridSplitter)d;

			if (gridSplitter._hoverWrapper == null)
			{
				return;
			}

			gridSplitter._hoverWrapper.GripperCursor = gridSplitter.GripperCursor;
		}

		private static void GripperCustomCursorResourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var gridSplitter = (GridSplitter)d;

			if (gridSplitter._hoverWrapper == null)
			{
				return;
			}

			gridSplitter._hoverWrapper.GripperCustomCursorResource = gridSplitter.GripperCustomCursorResource;
		}

		private static void CursorBehaviorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var gridSplitter = (GridSplitter)d;

			gridSplitter._hoverWrapper?.UpdateHoverElement(gridSplitter.CursorBehavior ==
				SplitterCursorBehavior.ChangeOnSplitterHover
				? gridSplitter
				: gridSplitter.Element);
		}

		private static void OnElementPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var gridSplitter = (GridSplitter)d;

			gridSplitter._hoverWrapper?.UpdateHoverElement(gridSplitter.CursorBehavior ==
				SplitterCursorBehavior.ChangeOnSplitterHover
				? gridSplitter
				: gridSplitter.Element);
		}
	}
}
