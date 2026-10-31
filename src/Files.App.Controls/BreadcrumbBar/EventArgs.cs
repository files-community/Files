// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Xaml.Input;

namespace Files.App.Controls
{
	public record class BreadcrumbBarItemClickedEventArgs(BreadcrumbBarItem Item, int Index, bool IsRootItem = false, PointerRoutedEventArgs? PointerRoutedEventArgs = null);

	public record class BreadcrumbBarItemDropDownFlyoutEventArgs(MenuFlyout Flyout, BreadcrumbBarItem? Item = null, int Index = -1, bool IsRootItem = false);
}
