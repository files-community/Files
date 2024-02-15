// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Factories
{
	public static class WidgetFileTagsItemContextFlyoutFactory
	{
		private static IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private static ICommandManager CommandManager { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		public static List<ContextMenuFlyoutItemViewModel> Generate(bool isFolder = false)
		{
			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new()
				{
					Text = "OpenWith".GetLocalizedResource(),
					OpacityIcon = new() { OpacityIconStyle = "ColorIconOpenWith" },
					Tag = "OpenWithPlaceholder",
					ShowItem = !isFolder
				},
				new()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendToPlaceholder",
					ShowItem = !isFolder && UserSettingsService.GeneralSettingsService.ShowSendToMenu
				},
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenDirectoryInNewTabAction).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenInNewWindowItemAction).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenDirectoryInNewPaneAction).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.PinItemToFavorites).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.UnpinItemFromFavorites).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenFileLocation).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenProperties).Build(),
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
