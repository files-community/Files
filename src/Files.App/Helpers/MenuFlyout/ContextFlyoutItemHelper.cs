// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Helpers
{
	/// <summary>
	/// Used to create lists of ContextMenuFlyoutItemViewModels that can be used by ItemModelListToContextFlyoutHelper to create context
	/// menus and toolbars for the user.
	/// <see cref="ContextMenuFlyoutItemViewModel"/>
	/// <see cref="ItemModelListToContextFlyoutHelper"/>
	/// </summary>
	public static class ContextFlyoutItemHelper
	{
		public static void ReplacePlaceholderWithShellOption(
			CommandBarFlyout contextMenu,
			string placeholderName,
			ContextMenuFlyoutItemViewModel? replacingItem,
			int position)
		{
			// Get placeholder item
			if (contextMenu.SecondaryCommands
					.Where(x => Equals((x as AppBarButton)?.Tag, placeholderName))
					.FirstOrDefault() is not AppBarButton placeholder)
				return;

			placeholder.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

			var (_, bitLockerCommands) =
				ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(
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
