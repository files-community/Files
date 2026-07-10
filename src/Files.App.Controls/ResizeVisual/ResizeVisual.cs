// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

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
		private FrameworkElement? _observedTarget;
		private TranslateTransform? _translateTransform;

		private bool _isDragging;
		private bool _pointerExited;
		private double _originalTargetWidth;
		private double _originalTargetHeight;

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

		private TranslateTransform EnsureTranslateTransform()
		{
			if (RenderTransform is TranslateTransform existingTransform)
			{
				_translateTransform = existingTransform;
				return existingTransform;
			}

			_translateTransform = new TranslateTransform();
			RenderTransform = _translateTransform;
			return _translateTransform;
		}

		private void UpdateThumbTranslation()
		{
			if (Target is null)
				return;

			var transform = EnsureTranslateTransform();

			if (Orientation is Orientation.Vertical)
			{
				transform.X = 0;
				transform.Y = Target.ActualHeight - ActualHeight / 2;
			}
			else if (Orientation is Orientation.Horizontal)
			{
				transform.X = Target.ActualWidth - ActualWidth / 2;
				transform.Y = 0;
			}
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			UnhookTemplateParts();
			_rootGrid = GetTemplateChild("PART_RootGrid") as Grid
				?? throw new MissingFieldException($"Could not find {"PART_RootGrid"} in the given {nameof(ResizeVisual)}'s style.");
			_outerThumb = GetTemplateChild("PART_Thumb") as Thumb
				?? throw new MissingFieldException($"Could not find {"PART_Thumb"} in the given {nameof(ResizeVisual)}'s style.");

			_rootGrid.PointerEntered += RootGrid_PointerEntered;
			_rootGrid.PointerExited += RootGrid_PointerExited;

			_outerThumb.PointerEntered += OuterThumb_PointerEntered;
			_outerThumb.PointerExited += OuterThumb_PointerExited;

			_outerThumb.DragStarted += Thumb_DragStarted;
			_outerThumb.DragDelta += Thumb_DragDelta;
			_outerThumb.DragCompleted += Thumb_DragCompleted;
			_outerThumb.DoubleTapped += OuterThumb_DoubleTapped;

			SizeChanged += ResizeVisual_SizeChanged;
			ApplyCurrentProperties();
		}

		private void UnhookTemplateParts()
		{
			SizeChanged -= ResizeVisual_SizeChanged;

			if (_rootGrid is not null)
			{
				_rootGrid.PointerEntered -= RootGrid_PointerEntered;
				_rootGrid.PointerExited -= RootGrid_PointerExited;
			}

			if (_outerThumb is not null)
			{
				_outerThumb.PointerEntered -= OuterThumb_PointerEntered;
				_outerThumb.PointerExited -= OuterThumb_PointerExited;
				_outerThumb.DragStarted -= Thumb_DragStarted;
				_outerThumb.DragDelta -= Thumb_DragDelta;
				_outerThumb.DragCompleted -= Thumb_DragCompleted;
				_outerThumb.DoubleTapped -= OuterThumb_DoubleTapped;
			}

			_rootGrid = null;
			_outerThumb = null;
		}
	}
}
