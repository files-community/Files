// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Input;

namespace Files.App.Controls
{
	/// <summary>
	/// Represents the control that redistributes space between columns or rows of a Grid control.
	/// </summary>
	public partial class GridSplitter : Control
	{
		internal const int GripperCustomCursorDefaultResource = -1;
		internal static readonly InputCursor ColumnsSplitterCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
		internal static readonly InputCursor RowSplitterCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);

		internal InputCursor PreviousCursor { get; set; }

		private GridResizeDirection _resizeDirection;
		private GridResizeBehavior _resizeBehavior;
		private GripperHoverWrapper _hoverWrapper;
		private TextBlock _gripperDisplay;

		private bool _pressed = false;
		private bool _dragging = false;
		private bool _pointerEntered = false;

		/// <summary>
		/// Gets the target parent grid from level
		/// </summary>
		private FrameworkElement TargetControl
		{
			get
			{
				if (ParentLevel == 0)
				{
					return this;
				}

				var parent = Parent;
				for (int i = 2; i < ParentLevel; i++)
				{
					var frameworkElement = parent as FrameworkElement;
					if (frameworkElement != null)
					{
						parent = frameworkElement.Parent;
					}
				}

				return parent as FrameworkElement;
			}
		}

		/// <summary>
		/// Gets GridSplitter Container Grid
		/// </summary>
		private Grid Resizable => TargetControl?.Parent as Grid;

		/// <summary>
		/// Gets the current Column definition of the parent Grid
		/// </summary>
		private ColumnDefinition CurrentColumn
		{
			get
			{
				if (Resizable == null)
				{
					return null;
				}

				var gridSplitterTargetedColumnIndex = GetTargetedColumn();

				if ((gridSplitterTargetedColumnIndex >= 0)
					&& (gridSplitterTargetedColumnIndex < Resizable.ColumnDefinitions.Count))
				{
					return Resizable.ColumnDefinitions[gridSplitterTargetedColumnIndex];
				}

				return null;
			}
		}

		/// <summary>
		/// Gets the Sibling Column definition of the parent Grid
		/// </summary>
		private ColumnDefinition SiblingColumn
		{
			get
			{
				if (Resizable == null)
				{
					return null;
				}

				var gridSplitterSiblingColumnIndex = GetSiblingColumn();

				if ((gridSplitterSiblingColumnIndex >= 0)
					&& (gridSplitterSiblingColumnIndex < Resizable.ColumnDefinitions.Count))
				{
					return Resizable.ColumnDefinitions[gridSplitterSiblingColumnIndex];
				}

				return null;
			}
		}

		/// <summary>
		/// Gets the current Row definition of the parent Grid
		/// </summary>
		private RowDefinition CurrentRow
		{
			get
			{
				if (Resizable == null)
				{
					return null;
				}

				var gridSplitterTargetedRowIndex = GetTargetedRow();

				if ((gridSplitterTargetedRowIndex >= 0)
					&& (gridSplitterTargetedRowIndex < Resizable.RowDefinitions.Count))
				{
					return Resizable.RowDefinitions[gridSplitterTargetedRowIndex];
				}

				return null;
			}
		}

		/// <summary>
		/// Gets the Sibling Row definition of the parent Grid
		/// </summary>
		private RowDefinition SiblingRow
		{
			get
			{
				if (Resizable == null)
				{
					return null;
				}

				var gridSplitterSiblingRowIndex = GetSiblingRow();

				if ((gridSplitterSiblingRowIndex >= 0)
					&& (gridSplitterSiblingRowIndex < Resizable.RowDefinitions.Count))
				{
					return Resizable.RowDefinitions[gridSplitterSiblingRowIndex];
				}

				return null;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GridSplitter"/> class.
		/// </summary>
		public GridSplitter()
		{
			DefaultStyleKey = typeof(GridSplitter);
			Loaded += GridSplitter_Loaded;
			AutomationProperties.SetName(this, "Grid Splitter");
		}

		/// <inheritdoc />
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			// Unhook registered events
			Loaded -= GridSplitter_Loaded;
			PointerEntered -= GridSplitter_PointerEntered;
			PointerExited -= GridSplitter_PointerExited;
			PointerPressed -= GridSplitter_PointerPressed;
			PointerReleased -= GridSplitter_PointerReleased;
			ManipulationStarted -= GridSplitter_ManipulationStarted;
			ManipulationCompleted -= GridSplitter_ManipulationCompleted;

			_hoverWrapper?.UnhookEvents();

			// Register Events
			Loaded += GridSplitter_Loaded;
			PointerEntered += GridSplitter_PointerEntered;
			PointerExited += GridSplitter_PointerExited;
			PointerPressed += GridSplitter_PointerPressed;
			PointerReleased += GridSplitter_PointerReleased;
			ManipulationStarted += GridSplitter_ManipulationStarted;
			ManipulationCompleted += GridSplitter_ManipulationCompleted;

			_hoverWrapper?.UpdateHoverElement(Element);

			ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
		}

		private void GridSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			_pressed = false;
			VisualStateManager.GoToState(this, _pointerEntered ? "PointerOver" : "Normal", true);
		}

		private void GridSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			_pressed = true;
			VisualStateManager.GoToState(this, "Pressed", true);
		}

		private void GridSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			_pointerEntered = false;

			if (!_pressed && !_dragging)
			{
				VisualStateManager.GoToState(this, "Normal", true);
			}
		}

		private void GridSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			_pointerEntered = true;

			if (!_pressed && !_dragging)
			{
				VisualStateManager.GoToState(this, "PointerOver", true);
			}
		}

		private void GridSplitter_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			_dragging = false;
			_pressed = false;
			VisualStateManager.GoToState(this, _pointerEntered ? "PointerOver" : "Normal", true);
		}

		private void GridSplitter_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
		{
			_dragging = true;
			VisualStateManager.GoToState(this, "Pressed", true);
		}
	}
}
