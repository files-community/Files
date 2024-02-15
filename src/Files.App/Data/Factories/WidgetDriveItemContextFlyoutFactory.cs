// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Factories
{
	public static class WidgetDriveItemContextFlyoutFactory
	{
		private static ICommandManager CommandManager { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		public static List<ContextMenuFlyoutItemViewModel> Generate()
		{
			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenDirectoryInNewTabAction).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenInNewWindowItemAction).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenDirectoryInNewPaneAction).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.PinItemToFavorites).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.UnpinItemFromFavorites).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.EjectDrive).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.FormatDrive).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenProperties).Build(),
				new()
				{
					Text = "TurnOnBitLocker".GetLocalizedResource(),
					Tag = "TurnOnBitLockerPlaceholder",
					CollapseLabel = true,
					IsEnabled = false,
				},
				new()
				{
					Text = "ManageBitLocker".GetLocalizedResource(),
					Tag = "ManageBitLockerPlaceholder",
					CollapseLabel = true,
					IsEnabled = false
				},
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					Tag = "OverflowSeparator",
				},
				new()
				{
					Text = "Loading".GetLocalizedResource(),
					Glyph = "\xE712",
					Items = [],
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					IsEnabled = false,
				}
			}.Where(x => x.ShowItem).ToList();
		}
	}
}
