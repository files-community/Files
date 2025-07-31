// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CommunityToolkit.WinUI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

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
		private bool _draggingSidebarResizer;
		private double _preManipulationSidebarWidth = 0;

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
			_draggingSidebarResizer = true;
			_preManipulationSidebarWidth = ActualWidth;
			VisualStateManager.GoToState(this, "ResizerPressed", true);
			e.Handled = true;
		}

		private void BladeResizer_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			var newWidth = _preManipulationSidebarWidth + e.Cumulative.Translation.X;
			
			Debug.WriteLine($"BladeResizer - New item width: {newWidth}");
			
			if (newWidth < MINIMUM_WIDTH)
				newWidth = MINIMUM_WIDTH;

			Width = newWidth;
			e.Handled = true;
		}

		private void BladeResizer_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			_draggingSidebarResizer = false;
			VisualStateManager.GoToState(this, "ResizerNormal", true);
			e.Handled = true;
		}

		private void BladeResizer_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			var optimalWidth = CalculateOptimalWidth();
			if (optimalWidth > 0)
			{
				Width = Math.Max(optimalWidth, MINIMUM_WIDTH);
			}
			else
			{
				// Fallback to default width if calculation fails
				Width = DEFAULT_WIDTH;
			}

			e.Handled = true;
		}

		private double CalculateOptimalWidth()
		{
			try
			{
				// Look for any ListView within this BladeItem that contains text content
				var listView = this.FindDescendant<ListView>();
				if (listView?.Items == null || !listView.Items.Any())
					return 0;

				// Calculate the maximum width needed by measuring text content
				var maxTextWidth = MeasureContentWidth(listView);

				// Add padding for icon, margins, and other UI elements
				// Icon width (32) + margins (24) + padding (24) + chevron/tags (40) = 120
				var totalPadding = 120;

				return maxTextWidth + totalPadding;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error calculating optimal width: {ex.Message}");
				return 0;
			}
		}

		private double MeasureContentWidth(ListView listView)
		{
			try
			{
				double maxWidth = 0;

				// Find all TextBlocks in the ListView using visual tree walking
				var textBlocks = GetTextBlocksFromVisualTree(listView);

				if (textBlocks.Any())
				{
					// Measure each TextBlock and find the widest one
					foreach (var textBlock in textBlocks)
					{
						if (string.IsNullOrEmpty(textBlock.Text))
							continue;

						// Create a measuring TextBlock with the same properties
						var measuringBlock = new TextBlock
						{
							Text = textBlock.Text,
							FontSize = textBlock.FontSize,
							FontFamily = textBlock.FontFamily,
							FontWeight = textBlock.FontWeight,
							FontStyle = textBlock.FontStyle
						};

						measuringBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
						maxWidth = Math.Max(maxWidth, measuringBlock.DesiredSize.Width);
					}
				}
				else
				{
					// Fallback: estimate based on item count and average text width
					var itemCount = listView.Items.Count;
					if (itemCount > 0)
					{
						// Estimate average filename length and multiply by character width
						var estimatedCharWidth = 8; // Approximate pixel width per character
						var estimatedMaxLength = Math.Min(50, Math.Max(20, itemCount * 2)); // Heuristic
						maxWidth = estimatedCharWidth * estimatedMaxLength;
					}
				}

				return maxWidth;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error measuring content width: {ex.Message}");
				// Fallback calculation
				return 200; // Default reasonable width
			}
		}
		private List<TextBlock> GetTextBlocksFromVisualTree(DependencyObject parent)
		{
			var textBlocks = new List<TextBlock>();

			if (parent == null)
				return textBlocks;

			try
			{
				var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
				for (int i = 0; i < childrenCount; i++)
				{
					var child = VisualTreeHelper.GetChild(parent, i);

					if (child is TextBlock textBlock)
					{
						textBlocks.Add(textBlock);
					}

					// Recursively search child elements
					textBlocks.AddRange(GetTextBlocksFromVisualTree(child));
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error walking visual tree: {ex.Message}");
			}

			return textBlocks;
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
			if (_draggingSidebarResizer)
				return;

			var sidebarResizer = (FrameworkElement)sender;
			sidebarResizer.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
			VisualStateManager.GoToState(this, "ResizerNormal", true);
			e.Handled = true;
		}
	}
}
