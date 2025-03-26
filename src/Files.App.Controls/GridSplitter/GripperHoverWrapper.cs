// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Files.App.Controls
{
	internal class GripperHoverWrapper
	{
		private readonly GridSplitter.GridResizeDirection _gridSplitterDirection;

		private InputCursor _splitterPreviousPointer;
		private InputCursor _previousCursor;
		private GridSplitter.GripperCursorType _gripperCursor;
		private int _gripperCustomCursorResource;
		private bool _isDragging;
		private UIElement _element;

		internal GridSplitter.GripperCursorType GripperCursor
		{
			get
			{
				return _gripperCursor;
			}

			set
			{
				_gripperCursor = value;
			}
		}

		internal int GripperCustomCursorResource
		{
			get
			{
				return _gripperCustomCursorResource;
			}

			set
			{
				_gripperCustomCursorResource = value;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GripperHoverWrapper"/> class that add cursor change on hover functionality for GridSplitter.
		/// </summary>
		/// <param name="element">UI element to apply cursor change on hover</param>
		/// <param name="gridSplitterDirection">GridSplitter resize direction</param>
		/// <param name="gripperCursor">GridSplitter gripper on hover cursor type</param>
		/// <param name="gripperCustomCursorResource">GridSplitter gripper custom cursor resource number</param>
		internal GripperHoverWrapper(UIElement element, GridSplitter.GridResizeDirection gridSplitterDirection, GridSplitter.GripperCursorType gripperCursor, int gripperCustomCursorResource)
		{
			_gridSplitterDirection = gridSplitterDirection;
			_gripperCursor = gripperCursor;
			_gripperCustomCursorResource = gripperCustomCursorResource;
			_element = element;
			UnhookEvents();
			_element.PointerEntered += Element_PointerEntered;
			_element.PointerExited += Element_PointerExited;
		}

		internal void UpdateHoverElement(UIElement element)
		{
			UnhookEvents();
			_element = element;
			_element.PointerEntered += Element_PointerEntered;
			_element.PointerExited += Element_PointerExited;
		}

		private void Element_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			if (_isDragging)
			{
				// if dragging don't update the cursor just update the splitter cursor with the last window cursor,
				// because the splitter is still using the arrow cursor and will revert to original case when drag completes
				_splitterPreviousPointer = _previousCursor;
			}
			else
			{
				if (Window.Current != null)
				{
					// Window.Current.CoreWindow.PointerCursor = _previousCursor;
				}
			}
		}

		private void Element_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			// if not dragging
			if (!_isDragging)
			{
				if (Window.Current != null)
				{
					// _previousCursor = _splitterPreviousPointer = Window.Current.CoreWindow.PointerCursor;
				}

				UpdateDisplayCursor();
			}

			// if dragging
			else
			{
				_previousCursor = _splitterPreviousPointer;
			}
		}

		private void UpdateDisplayCursor()
		{
			if (Window.Current == null)
			{
				return;
			}

			if (_gripperCursor == GridSplitter.GripperCursorType.Default)
			{
				if (_gridSplitterDirection == GridSplitter.GridResizeDirection.Columns)
				{
					// Window.Current.CoreWindow.PointerCursor = GridSplitter.ColumnsSplitterCursor;
				}
				else if (_gridSplitterDirection == GridSplitter.GridResizeDirection.Rows)
				{
					// Window.Current.CoreWindow.PointerCursor = GridSplitter.RowSplitterCursor;
				}
			}
			else
			{
				var inputSystemCursorShape = (InputSystemCursorShape)((int)_gripperCursor);
				if (_gripperCursor == GridSplitter.GripperCursorType.Custom)
				{
					if (_gripperCustomCursorResource > GridSplitter.GripperCustomCursorDefaultResource)
					{
						// Window.Current.CoreWindow.PointerCursor = InputDesktopResourceCursor.Create((uint)_gripperCustomCursorResource);
					}
				}
				else
				{
					// Window.Current.CoreWindow.PointerCursor = InputSystemCursor.Create(inputSystemCursorShape);
				}
			}
		}

		internal void SplitterManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
		{
			var splitter = sender as GridSplitter;
			if (splitter == null)
			{
				return;
			}

			_splitterPreviousPointer = splitter.PreviousCursor;
			_isDragging = true;
		}

		internal void SplitterManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			var splitter = sender as GridSplitter;
			if (splitter == null)
			{
				return;
			}

			if (Window.Current != null)
			{
				// Window.Current.CoreWindow.PointerCursor = splitter.PreviousCursor = _splitterPreviousPointer;
			}

			_isDragging = false;
		}

		internal void UnhookEvents()
		{
			if (_element == null)
			{
				return;
			}

			_element.PointerEntered -= Element_PointerEntered;
			_element.PointerExited -= Element_PointerExited;
		}
	}
}
