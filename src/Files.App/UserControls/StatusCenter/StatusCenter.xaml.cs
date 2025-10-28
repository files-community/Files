// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Files.App.Data.Contracts;
using Microsoft.UI.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Files.App.Controls;
using Microsoft.UI.Xaml.Media;
using Files.App.Views;

namespace Files.App.UserControls.StatusCenter
{
	public sealed partial class StatusCenter : UserControl
	{
		public StatusCenterViewModel ViewModel;
		private readonly IAppSettingsService AppSettingsService;
		private Point _initialPosition;
		private Size _initialSize;

		public StatusCenter()
		{
			InitializeComponent();

			ViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();
			AppSettingsService = Ioc.Default.GetRequiredService<IAppSettingsService>();
			
			// Load initial size from settings
			Loaded += StatusCenter_Loaded;
		}

		private void StatusCenter_Loaded(object sender, RoutedEventArgs e)
		{
			// Set initial size from settings
			Width = AppSettingsService.StatusCenterWidth;
			Height = AppSettingsService.StatusCenterHeight;
		}

		private void CloseAllItemsButton_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.RemoveAllCompletedItems();
		}

		private void CloseItemButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.DataContext is StatusCenterItem item)
				ViewModel.RemoveItem(item);
		}

		private void ExpandCollapseChevronItemButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.DataContext is StatusCenterItem item)
			{
				var buttonAnimatedIcon = button.FindDescendant<AnimatedIcon>();

				if (buttonAnimatedIcon is not null)
					AnimatedIcon.SetState(buttonAnimatedIcon, item.IsExpanded ? "NormalOff" : "NormalOn");

				item.IsExpanded = !item.IsExpanded;
			}
		}

		private void ResizeGrip_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
		{
			_initialPosition = e.Position;
			_initialSize = new Size(Width, Height);
			e.Handled = true;
		}

		private void ResizeGrip_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			var cumulative = e.Cumulative;
			var newWidth = _initialSize.Width - cumulative.Translation.X; // Negative because we're resizing from bottom-left
			var newHeight = _initialSize.Height + cumulative.Translation.Y; // Positive because we're resizing from bottom-left

			// Get available space constraints
			var availableWidth = GetAvailableWidth();
			var availableHeight = GetAvailableHeight();

			// Constrain to min/max bounds and available space
			newWidth = Math.Max(300, Math.Min(Math.Min(600, availableWidth), newWidth));
			newHeight = Math.Max(200, Math.Min(Math.Min(800, availableHeight), newHeight));

			// Update the StatusCenter control size directly
			Width = newWidth;
			Height = newHeight;

			e.Handled = true;
		}

		private double GetAvailableWidth()
		{
			// Get the main window bounds to determine available space
			var mainWindow = MainWindow.Instance;
			if (mainWindow != null)
			{
				// Account for flyout placement (bottom-right) and some margin
				return Math.Max(300, mainWindow.Bounds.Width * 0.8); // Max 80% of window width
			}
			return 600; // Fallback
		}

		private double GetAvailableHeight()
		{
			// Get the main window bounds to determine available space
			var mainWindow = MainWindow.Instance;
			if (mainWindow != null)
			{
				// Account for flyout placement (bottom-right) and some margin
				return Math.Max(200, mainWindow.Bounds.Height * 0.7); // Max 70% of window height
			}
			return 800; // Fallback
		}

		private void ResizeGrip_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			// Save the new dimensions to settings
			AppSettingsService.StatusCenterWidth = Width;
			AppSettingsService.StatusCenterHeight = Height;
			e.Handled = true;
		}

		private void ResizeGrip_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			// Change cursor to resize cursor
			var resizeGrip = (FrameworkElement)sender;
			resizeGrip.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeNorthwestSoutheast));
		}

		private void ResizeGrip_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			// Reset cursor to default
			var resizeGrip = (FrameworkElement)sender;
			resizeGrip.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
		}
	}
}
