// Copyright (c) Files Community
// Licensed under the MIT License.

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
			SidebarContext.RightClickedItem is not null &&
			!string.IsNullOrEmpty(SidebarContext.RightClickedItem.Path) &&
			SidebarContext.RightClickedItem.MenuOptions.ShowShellItems &&
			!SidebarContext.RightClickedItem.MenuOptions.ShowEmptyRecycleBin;

		public override bool IsAccessibleGlobally
			=> false;

		public override HotKey HotKey
			=> HotKey.None;

		protected override string[] GetPaths()
		{
			if (SidebarContext.IsItemRightClicked &&
				SidebarContext.RightClickedItem?.Path is { Length: > 0 } path)
			{
				return [path];
			}

			return [];
		}
	}
}
