// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.Controls
{
	/// <summary>
	/// Represents the control that redistributes space between columns or rows of a Grid control.
	/// </summary>
	public partial class GridSplitter
	{
		// Symbols for GripperBar in Segoe MDL2 Assets
		private const string GripperBarVertical = "\xE784";
		private const string GripperBarHorizontal = "\xE76F";
		private const string GripperDisplayFont = "Segoe MDL2 Assets";

		private void GridSplitter_Loaded(object sender, RoutedEventArgs e)
		{
			_resizeDirection = GetResizeDirection();
			_resizeBehavior = GetResizeBehavior();

			// Adding Grip to Grid Splitter
			if (Element == default(UIElement))
			{
				CreateGripperDisplay();
				Element = _gripperDisplay;
			}

			if (_hoverWrapper == null)
			{
				var hoverWrapper = new GripperHoverWrapper(
					CursorBehavior == SplitterCursorBehavior.ChangeOnSplitterHover
					? this
					: Element,
					_resizeDirection,
					GripperCursor,
					GripperCustomCursorResource);
				ManipulationStarted += hoverWrapper.SplitterManipulationStarted;
				ManipulationCompleted += hoverWrapper.SplitterManipulationCompleted;

				_hoverWrapper = hoverWrapper;
			}
		}

		private void CreateGripperDisplay()
		{
			if (_gripperDisplay == null)
			{
				_gripperDisplay = new TextBlock
				{
					FontFamily = new FontFamily(GripperDisplayFont),
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
					Foreground = GripperForeground,
					Text = _resizeDirection == GridResizeDirection.Columns ? GripperBarVertical : GripperBarHorizontal
				};
				_gripperDisplay.SetValue(
					Microsoft.UI.Xaml.Automation.AutomationProperties.AccessibilityViewProperty,
					Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
			}
		}

		private bool IsCtrlDown()
		{
			if (Window.Current == null)
			{
				return false;
			}

			var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
			return ctrl.HasFlag(CoreVirtualKeyStates.Down);
		}

		/// <inheritdoc />
		protected override void OnKeyDown(KeyRoutedEventArgs e)
		{
			var step = 1;
			if (IsCtrlDown())
			{
				step = 5;
			}

			if (_resizeDirection == GridResizeDirection.Columns)
			{
				if (e.Key == VirtualKey.Left)
				{
					HorizontalMove(-step);
				}
				else if (e.Key == VirtualKey.Right)
				{
					HorizontalMove(step);
				}
				else
				{
					return;
				}

				e.Handled = true;
				return;
			}

			if (_resizeDirection == GridResizeDirection.Rows)
			{
				if (e.Key == VirtualKey.Up)
				{
					VerticalMove(-step);
				}
				else if (e.Key == VirtualKey.Down)
				{
					VerticalMove(step);
				}
				else
				{
					return;
				}

				e.Handled = true;
			}

			base.OnKeyDown(e);
		}

		/// <inheritdoc />
		protected override void OnManipulationStarted(ManipulationStartedRoutedEventArgs e)
		{
			// saving the previous state
			PreviousCursor = ProtectedCursor;
			if (PreviousCursor == null)
			{
				PreviousCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
			}

			_resizeDirection = GetResizeDirection();
			_resizeBehavior = GetResizeBehavior();

			if (_resizeDirection == GridResizeDirection.Columns)
			{
				ProtectedCursor = ColumnsSplitterCursor;
			}
			else if (_resizeDirection == GridResizeDirection.Rows)
			{
				ProtectedCursor = RowSplitterCursor;
			}

			base.OnManipulationStarted(e);
		}

		/// <inheritdoc />
		protected override void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs e)
		{
			ProtectedCursor = PreviousCursor;

			base.OnManipulationCompleted(e);
		}

		/// <inheritdoc />
		protected override void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e)
		{
			var horizontalChange = e.Delta.Translation.X;
			var verticalChange = e.Delta.Translation.Y;

			if (this.FlowDirection == FlowDirection.RightToLeft)
			{
				horizontalChange *= -1;
			}

			if (_resizeDirection == GridResizeDirection.Columns)
			{
				if (HorizontalMove(horizontalChange))
				{
					return;
				}
			}
			else if (_resizeDirection == GridResizeDirection.Rows)
			{
				if (VerticalMove(verticalChange))
				{
					return;
				}
			}

			base.OnManipulationDelta(e);
		}

		private bool VerticalMove(double verticalChange)
		{
			if (CurrentRow == null || SiblingRow == null)
			{
				return true;
			}

			// if current row has fixed height then resize it
			if (!IsStarRow(CurrentRow))
			{
				// No need to check for the row Min height because it is automatically respected
				if (!SetRowHeight(CurrentRow, verticalChange, GridUnitType.Pixel))
				{
					return true;
				}
			}

			// if sibling row has fixed width then resize it
			else if (!IsStarRow(SiblingRow))
			{
				// Would adding to this column make the current column violate the MinWidth?
				if (IsValidRowHeight(CurrentRow, verticalChange) == false)
				{
					return false;
				}

				if (!SetRowHeight(SiblingRow, verticalChange * -1, GridUnitType.Pixel))
				{
					return true;
				}
			}

			// if both row haven't fixed height (auto *)
			else
			{
				// change current row height to the new height with respecting the auto
				// change sibling row height to the new height relative to current row
				// respect the other star row height by setting it's height to it's actual height with stars

				// We need to validate current and sibling height to not cause any unexpected behavior
				if (!IsValidRowHeight(CurrentRow, verticalChange) ||
					!IsValidRowHeight(SiblingRow, verticalChange * -1))
				{
					return true;
				}

				foreach (var rowDefinition in Resizable.RowDefinitions)
				{
					if (rowDefinition == CurrentRow)
					{
						SetRowHeight(CurrentRow, verticalChange, GridUnitType.Star);
					}
					else if (rowDefinition == SiblingRow)
					{
						SetRowHeight(SiblingRow, verticalChange * -1, GridUnitType.Star);
					}
					else if (IsStarRow(rowDefinition))
					{
						rowDefinition.Height = new GridLength(rowDefinition.ActualHeight, GridUnitType.Star);
					}
				}
			}

			return false;
		}

		private bool HorizontalMove(double horizontalChange)
		{
			if (CurrentColumn == null || SiblingColumn == null)
			{
				return true;
			}

			// if current column has fixed width then resize it
			if (!IsStarColumn(CurrentColumn))
			{
				// No need to check for the Column Min width because it is automatically respected
				if (!SetColumnWidth(CurrentColumn, horizontalChange, GridUnitType.Pixel))
				{
					return true;
				}
			}

			// if sibling column has fixed width then resize it
			else if (!IsStarColumn(SiblingColumn))
			{
				// Would adding to this column make the current column violate the MinWidth?
				if (IsValidColumnWidth(CurrentColumn, horizontalChange) == false)
				{
					return false;
				}

				if (!SetColumnWidth(SiblingColumn, horizontalChange * -1, GridUnitType.Pixel))
				{
					return true;
				}
			}

			// if both column haven't fixed width (auto *)
			else
			{
				// change current column width to the new width with respecting the auto
				// change sibling column width to the new width relative to current column
				// respect the other star column width by setting it's width to it's actual width with stars

				// We need to validate current and sibling width to not cause any unexpected behavior
				if (!IsValidColumnWidth(CurrentColumn, horizontalChange) ||
					!IsValidColumnWidth(SiblingColumn, horizontalChange * -1))
				{
					return true;
				}

				foreach (var columnDefinition in Resizable.ColumnDefinitions)
				{
					if (columnDefinition == CurrentColumn)
					{
						SetColumnWidth(CurrentColumn, horizontalChange, GridUnitType.Star);
					}
					else if (columnDefinition == SiblingColumn)
					{
						SetColumnWidth(SiblingColumn, horizontalChange * -1, GridUnitType.Star);
					}
					else if (IsStarColumn(columnDefinition))
					{
						columnDefinition.Width = new GridLength(columnDefinition.ActualWidth, GridUnitType.Star);
					}
				}
			}

			return false;
		}
	}
}
