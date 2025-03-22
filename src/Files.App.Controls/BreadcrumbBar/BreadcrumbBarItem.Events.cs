// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	public partial class BreadcrumbBarItem
	{
		private void ItemContentButton_Click(object sender, RoutedEventArgs e)
		{
			OnItemClicked();
		}

		private void ItemChevronButton_Click(object sender, RoutedEventArgs e)
		{
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
