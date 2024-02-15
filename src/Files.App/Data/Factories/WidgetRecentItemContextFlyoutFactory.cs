// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Factories
{
	public class WidgetRecentItemContextFlyoutFactory
	{
		private static ICommandManager CommandManager { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		public static List<ContextMenuFlyoutItemViewModel> Generate()
		{
			return new List<ContextMenuFlyoutItemViewModel>()
			{
				//new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenWith).Build(),
				//new ContextMenuFlyoutItemViewModelBuilder(CommandManager.SendTo).Build(),
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
