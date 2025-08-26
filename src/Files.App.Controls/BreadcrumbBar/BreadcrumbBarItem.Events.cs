// Copyright (c) Files Community
// Licensed under the MIT License.
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Microsoft.UI.Input;

namespace Files.App.Controls
{
	public partial class BreadcrumbBarItem
	{
		private void ItemContentButton_Click(object sender, RoutedEventArgs e)
		{
			OnItemClicked();
		}

		private void ItemContentButton_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			// Check for middle mouse button click
			if (e.GetCurrentPoint(sender as FrameworkElement).Properties.IsMiddleButtonPressed)
			{
				OnItemMiddleClicked();
				e.Handled = true;
			}
		}

		private void ItemChevronButton_Click(object sender, RoutedEventArgs e)
		{
			FlyoutBase.ShowAttachedFlyout(_itemChevronButton);
		}

		private void ItemChevronButton_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Down)
				FlyoutBase.ShowAttachedFlyout(_itemChevronButton);
		}

		private void ItemContentButton_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Down)
				FlyoutBase.ShowAttachedFlyout(_itemChevronButton);
		}

		private void ChevronDropDownMenuFlyout_Opening(object? sender, object e)
		{
			if (_ownerRef is null ||
				_ownerRef.TryGetTarget(out var breadcrumbBar) is false ||
				sender is not MenuFlyout flyout)
				return;

			breadcrumbBar.RaiseItemDropDownFlyoutOpening(this, flyout);
		}

		private void ChevronDropDownMenuFlyout_Opened(object? sender, object e)
		{
			VisualStateManager.GoToState(this, "ChevronNormalOn", true);
		}

		private void ChevronDropDownMenuFlyout_Closed(object? sender, object e)
		{
			if (_ownerRef is null ||
				_ownerRef.TryGetTarget(out var breadcrumbBar) is false ||
				sender is not MenuFlyout flyout)
				return;

			breadcrumbBar.RaiseItemDropDownFlyoutClosed(this, flyout);
			VisualStateManager.GoToState(this, "ChevronNormalOff", true);
			VisualStateManager.GoToState(this, "PointerNormal", true);
		}
	}
}
