// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Input;

namespace Files.App.Controls
{
	/// <summary>
	/// The Blade is used as a child in the BladeView
	/// </summary>
	[TemplatePart(Name = "CloseButton", Type = typeof(Button))]
	public partial class BladeItem : ContentControl
	{
		private const double MINIMUM_WIDTH = 150;
		private const double DEFAULT_WIDTH = 300; // Default width for the blade item

		private Button _closeButton;
		private Border _bladeResizer;
		private bool draggingSidebarResizer;
		private double preManipulationSidebarWidth = 0;

		/// <summary>
		/// Initializes a new instance of the <see cref="BladeItem"/> class.
		/// </summary>
		public BladeItem()
		{
			DefaultStyleKey = typeof(BladeItem);
		}

		/// <summary>
		/// Override default OnApplyTemplate to capture child controls
		/// </summary>
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_closeButton = GetTemplateChild("CloseButton") as Button;

			if (_closeButton == null)
			{
				return;
			}

			_closeButton.Click -= CloseButton_Click;
			_closeButton.Click += CloseButton_Click;

			_bladeResizer = GetTemplateChild("BladeResizer") as Border;

			if (_bladeResizer != null)
			{
				_bladeResizer.ManipulationStarted -= BladeResizer_ManipulationStarted;
				_bladeResizer.ManipulationStarted += BladeResizer_ManipulationStarted;

				_bladeResizer.ManipulationDelta -= BladeResizer_ManipulationDelta;
				_bladeResizer.ManipulationDelta += BladeResizer_ManipulationDelta;

				_bladeResizer.ManipulationCompleted -= BladeResizer_ManipulationCompleted;
				_bladeResizer.ManipulationCompleted += BladeResizer_ManipulationCompleted;

				_bladeResizer.PointerEntered -= BladeResizer_PointerEntered;
				_bladeResizer.PointerEntered += BladeResizer_PointerEntered;

				_bladeResizer.PointerExited -= BladeResizer_PointerExited;
				_bladeResizer.PointerExited += BladeResizer_PointerExited;

				_bladeResizer.DoubleTapped -= BladeResizer_DoubleTapped;
				_bladeResizer.DoubleTapped += BladeResizer_DoubleTapped;
			}
		}

		/// <summary>
		/// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
		/// </summary>
		/// <returns>An automation peer for this <see cref="BladeItem"/>.</returns>
		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new BladeItemAutomationPeer(this);
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			IsOpen = false;
		}
		private void BladeResizer_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
		{
			draggingSidebarResizer = true;
			preManipulationSidebarWidth = ActualWidth;
			VisualStateManager.GoToState(this, "ResizerPressed", true);
			e.Handled = true;
		}

		private void BladeResizer_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			var newWidth = preManipulationSidebarWidth + e.Cumulative.Translation.X;
			
			Debug.WriteLine($"BladeResizer - New item width: {newWidth}");
			
			if (newWidth < MINIMUM_WIDTH)
				newWidth = MINIMUM_WIDTH;

			Width = newWidth;
			e.Handled = true;
		}

		private void BladeResizer_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			draggingSidebarResizer = false;
			VisualStateManager.GoToState(this, "ResizerNormal", true);
			e.Handled = true;
		}

		private void BladeResizer_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			Width = DEFAULT_WIDTH;
			e.Handled = true;
		}

		private void BladeResizer_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			var sidebarResizer = (FrameworkElement)sender;
			sidebarResizer.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
			VisualStateManager.GoToState(this, "ResizerPointerOver", true);
			e.Handled = true;
		}

		private void BladeResizer_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			if (draggingSidebarResizer)
				return;

			var sidebarResizer = (FrameworkElement)sender;
			sidebarResizer.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
			VisualStateManager.GoToState(this, "ResizerNormal", true);
			e.Handled = true;
		}
	}
}
