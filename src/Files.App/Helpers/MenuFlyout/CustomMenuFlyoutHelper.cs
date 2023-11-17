// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Helpers
{
	public static class CustomMenuFlyoutHelper
	{
		public static void ReplacePlaceholderWithShellOption(
			CommandBarFlyout contextMenu,
			string placeholderName,
			CustomMenuFlyoutItem? replacingItem,
			int position)
		{
			// Get placeholder item
			if (contextMenu.SecondaryCommands
					.Where(x => Equals((x as AppBarButton)?.Tag, placeholderName))
					.FirstOrDefault() is not AppBarButton placeholder)
				return;

			placeholder.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

			var (_, bitLockerCommands) =
				MenuFlyoutFactory.GetAppBarItemsFromModel(
					new()
					{
						replacingItem
					});

			contextMenu.SecondaryCommands.Insert(
				position,
				bitLockerCommands.FirstOrDefault()
			);
		}
	}
}
