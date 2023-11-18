// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Factories
{
	public static class DriveItemContextFlyoutFactory
	{
		private static ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		public static List<CustomMenuFlyoutItem> Generate(DriveCardItem target, bool isPinned)
		{
			var options = target?.Item.MenuOptions;

			return new List<CustomMenuFlyoutItem>()
			{
				new CustomMenuFlyoutItem(Commands.OpenDirectoryInNewTabAction, target),
				new CustomMenuFlyoutItem(Commands.OpenInNewWindowItemAction, target),
				new CustomMenuFlyoutItem(Commands.OpenDirectoryInNewPaneAction, target),
				new CustomMenuFlyoutItem(Commands.PinItemToFavorites, target)
				{
					IsAvailable = !isPinned
				},
				new CustomMenuFlyoutItem(Commands.UnpinItemFromFavorites, target)
				{
					IsAvailable = isPinned
				},
				new CustomMenuFlyoutItem(Commands.EjectDrive, target)
				{
					IsAvailable = options?.ShowEjectDevice ?? false
				},
				new CustomMenuFlyoutItem(Commands.FormatDrive, target)
				{
					IsAvailable = options?.ShowFormatDrive ?? false
				},
				new CustomMenuFlyoutItem(Commands.OpenProperties, target),
				new CustomMenuFlyoutItem()
				{
					Text = "TurnOnBitLocker".GetLocalizedResource(),
					Tag = "TurnOnBitLockerPlaceholder",
					IsEnabled = false
				},
				new CustomMenuFlyoutItem()
				{
					Text = "ManageBitLocker".GetLocalizedResource(),
					Tag = "ManageBitLockerPlaceholder",
					IsEnabled = false
				},
				new CustomMenuFlyoutItem()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					Tag = "OverflowSeparator",
				},
				new CustomMenuFlyoutItem()
				{
					Text = "Loading".GetLocalizedResource(),
					Glyph = "\xE712",
					Items = new List<CustomMenuFlyoutItem>(),
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					IsEnabled = false,
				},
			}.Where(x => x.IsAvailable).ToList();
		}
	}
}
