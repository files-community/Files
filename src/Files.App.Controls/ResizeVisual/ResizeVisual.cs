// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Input;

namespace Files.App.Controls
{
	/// <summary>
	/// Represents a control that provides visual resizing handles and events for interactive resizing operations.
	/// </summary>
	/// <remarks>
	/// This control is typically used as part of resizable content users to resize elements by dragging <see cref="Thumb"/>.
	/// It directly updates <see cref="FrameworkElement.ActualWidth"/> or <see cref="FrameworkElement.ActualHeight"/> of <see cref="Target"/> and raises events during the dragging to allow customization or handling of resizing logic.
	/// </remarks>
	public partial class ResizeVisual : Control
	{
		private Grid? _rootGrid;
		private Thumb? _outerThumb;

		private bool _isDragging;
		private bool _pointerExited;
		private bool _reachedToBounds;
		private double _deltaOutsideBounds;

		/// <summary>Fires when a Thumb control receives logical focus and mouse capture.</summary>
		public event DragStartedEventHandler? DragStarted;

		/// <summary>Fires one or more times as the mouse pointer is moved when a Thumb control has logical focus and mouse capture.</summary>
		public event DragDeltaEventHandler? DragDelta;

		/// <summary>Fires when the Thumb control loses mouse capture.</summary>
		public event DragCompletedEventHandler? DragCompleted;

		public ResizeVisual()
		{
			DefaultStyleKey = typeof(ResizeVisual);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_rootGrid = GetTemplateChild("PART_RootGrid") as Grid
				?? throw new MissingFieldException($"Could not find {"PART_RootGrid"} in the given {nameof(ResizeVisual)}'s style.");
			_outerThumb = GetTemplateChild("PART_Thumb") as Thumb
				?? throw new MissingFieldException($"Could not find {"PART_Thumb"} in the given {nameof(ResizeVisual)}'s style.");

			Loaded += ResizeVisual_Loaded;

			_rootGrid.PointerEntered += RootGrid_PointerEntered;
			_rootGrid.PointerExited += RootGrid_PointerExited;

			_outerThumb.PointerEntered += OuterThumb_PointerEntered;
			_outerThumb.PointerExited += OuterThumb_PointerExited;

			_outerThumb.DragStarted += Thumb_DragStarted;
			_outerThumb.DragDelta += Thumb_DragDelta;
			_outerThumb.DragCompleted += Thumb_DragCompleted;

			Unloaded += ResizeVisual_Unloaded;
		}

		protected static bool IsValidHeight(FrameworkElement target, double newHeight, double thisActualHeight)
		{
			if (newHeight < (double.IsNaN(target.MinHeight) ? 0 : target.MinHeight) ||
				newHeight > (double.IsNaN(target.MaxHeight) ? double.PositiveInfinity : target.MaxHeight) ||
				newHeight <= thisActualHeight)
				return false;

			return true;
		}

		protected static bool IsValidWidth(FrameworkElement target, double newWidth, double thisActualWidth)
		{
			if (newWidth < (double.IsNaN(target.MinWidth) ? 0 : target.MinWidth) ||
				newWidth > (double.IsNaN(target.MaxWidth) ? double.PositiveInfinity : target.MaxWidth) ||
				newWidth <= thisActualWidth)
				return false;

			return true;
		}
	}
}
