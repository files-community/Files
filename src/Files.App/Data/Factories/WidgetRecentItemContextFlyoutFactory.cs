// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Factories
{
	public class WidgetRecentItemContextFlyoutFactory
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
				//new ContextMenuFlyoutItemViewModelBuilder(CommandManager.RemoveRecentItem).Build(),
				//new ContextMenuFlyoutItemViewModelBuilder(CommandManager.ClearRecentItem).Build(),
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
