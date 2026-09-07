// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class OpenTerminalFromSidebarAction : OpenTerminalAction
	{
		private ISidebarContext SidebarContext { get; } = Ioc.Default.GetRequiredService<ISidebarContext>();

		public override string Label
			=> Strings.OpenTerminal.GetLocalizedResource();

		public override string Description
			=> Strings.OpenTerminalDescription.GetLocalizedResource();

		public override bool IsExecutable =>
			SidebarContext.IsItemRightClicked &&
			SidebarContext.RightClickedItem is { } item &&
			item.MenuOptions!.ShowShellItems &&
			!item.MenuOptions.ShowEmptyRecycleBin;

		public override bool IsAccessibleGlobally
			=> false;

		public override HotKey HotKey
			=> HotKey.None;

		protected override string[] GetPaths()
		{
			if (SidebarContext.IsItemRightClicked && SidebarContext.RightClickedItem is not null)
				return
				[
					SidebarContext.RightClickedItem.GetRequiredPath()
				];

			return [];
		}
	}
}
