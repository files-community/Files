// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.Widgets;

namespace Files.App.Data.Factories
{
	public static class SideBarDriveItemContextMenuFactory
	{
		private static IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private static ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		public static List<ContextMenuFlyoutItemViewModel> Generate(DriveCardItem target, WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			var options = target?.Item.MenuOptions;

			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModelBuilder(Commands.OpenDirectoryInNewTabAction)
				{
					CommandParameter = item,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.OpenInNewWindowItemAction)
				{
					CommandParameter = item,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.OpenDirectoryInNewPaneAction)
				{
					CommandParameter = item,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.PinItemToFavorites)
				{
					CommandParameter = item,
					IsVisible = !isPinned
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.UnpinItemFromFavorites)
				{
					CommandParameter = item,
					IsVisible = isPinned
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.EjectDrive)
				{
					CommandParameter = item,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.FormatDrive)
				{
					CommandParameter = item,
					IsVisible = options?.ShowFormatDrive ?? false
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.OpenProperties)
				{
					CommandParameter = item
				}.Build(),
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "TurnOnBitLocker".GetLocalizedResource(),
					Tag = "TurnOnBitLockerPlaceholder",
					IsEnabled = false
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "ManageBitLocker".GetLocalizedResource(),
					Tag = "ManageBitLockerPlaceholder",
					IsEnabled = false
				},
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					Tag = "OverflowSeparator",
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Loading".GetLocalizedResource(),
					Glyph = "\xE712",
					Items = new List<ContextMenuFlyoutItemViewModel>(),
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					IsEnabled = false,
				},
			}.Where(x => x.ShowItem).ToList();
		}
	}
}
