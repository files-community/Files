// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.EventArguments
{
	public sealed class SidebarRightClickedItemChangedEventArgs
	{
		public INavigationControlItem? Item { get; set; }

		public CommandBarFlyout? Flyout { get; set; }

		public SidebarRightClickedItemChangedEventArgs(INavigationControlItem? item = null, CommandBarFlyout? flyout = null)
		{
			Item = item;
			Flyout = flyout;
		}
	}
}
