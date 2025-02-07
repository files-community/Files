// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Files.App.Controls
{
	/// <summary>
	/// Represents the control that redistributes space between columns or rows of a Grid control.
	/// </summary>
	public partial class GridSplitter
	{
		/// <summary>
		/// Enum to indicate whether GridSplitter resizes Columns or Rows
		/// </summary>
		public enum GridResizeDirection
		{
			/// <summary>
			/// Determines whether to resize rows or columns based on its Alignment and
			/// width compared to height
			/// </summary>
			Auto,

			/// <summary>
			/// Resize columns when dragging Splitter.
			/// </summary>
			Columns,

			/// <summary>
			/// Resize rows when dragging Splitter.
			/// </summary>
			Rows
		}

		/// <summary>
		/// Enum to indicate what Columns or Rows the GridSplitter resizes
		/// </summary>
		public enum GridResizeBehavior
		{
			/// <summary>
			/// Determine which columns or rows to resize based on its Alignment.
			/// </summary>
			BasedOnAlignment,

			/// <summary>
			/// Resize the current and next Columns or Rows.
			/// </summary>
			CurrentAndNext,

			/// <summary>
			/// Resize the previous and current Columns or Rows.
			/// </summary>
			PreviousAndCurrent,

			/// <summary>
			/// Resize the previous and next Columns or Rows.
			/// </summary>
			PreviousAndNext
		}

		/// <summary>
		///  Enum to indicate the supported gripper cursor types.
		/// </summary>
		public enum GripperCursorType
		{
			/// <summary>
			/// Change the cursor based on the splitter direction
			/// </summary>
			Default = -1,

			/// <summary>
			/// Standard Arrow cursor
			/// </summary>
			Arrow,

			/// <summary>
			/// Standard Cross cursor
			/// </summary>
			Cross,

			/// <summary>
			/// Standard Custom cursor
			/// </summary>
			Custom,

			/// <summary>
			/// Standard Hand cursor
			/// </summary>
			Hand,

			/// <summary>
			/// Standard Help cursor
			/// </summary>
			Help,

			/// <summary>
			/// Standard IBeam cursor
			/// </summary>
			IBeam,

			/// <summary>
			/// Standard SizeAll cursor
			/// </summary>
			SizeAll,

			/// <summary>
			/// Standard SizeNortheastSouthwest cursor
			/// </summary>
			SizeNortheastSouthwest,

			/// <summary>
			/// Standard SizeNorthSouth cursor
			/// </summary>
			SizeNorthSouth,

			/// <summary>
			/// Standard SizeNorthwestSoutheast cursor
			/// </summary>
			SizeNorthwestSoutheast,

			/// <summary>
			/// Standard SizeWestEast cursor
			/// </summary>
			SizeWestEast,

			/// <summary>
			/// Standard UniversalNo cursor
			/// </summary>
			UniversalNo,

			/// <summary>
			/// Standard UpArrow cursor
			/// </summary>
			UpArrow,

			/// <summary>
			/// Standard Wait cursor
			/// </summary>
			Wait
		}

		/// <summary>
		///  Enum to indicate the behavior of window cursor on grid splitter hover
		/// </summary>
		public enum SplitterCursorBehavior
		{
			/// <summary>
			/// Update window cursor on Grid Splitter hover
			/// </summary>
			ChangeOnSplitterHover,

			/// <summary>
			/// Update window cursor on Grid Splitter Gripper hover
			/// </summary>
			ChangeOnGripperHover
		}
	}
}
