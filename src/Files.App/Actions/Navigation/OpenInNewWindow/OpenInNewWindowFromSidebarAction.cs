// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class OpenInNewWindowFromSidebarAction : BaseOpenInNewWindowAction
	{
		public override HotKey HotKey
			=> HotKey.None;

		public override bool IsExecutable =>
			SidebarContext.IsItemRightClicked &&
			SidebarContext.RightClickedItem is { } item &&
			item.MenuOptions!.IsLocationItem;

		public override bool IsAccessibleGlobally
			=> false;

		public override async Task ExecuteAsync(object? parameter = null)
		{
			await NavigationHelpers.OpenPathInNewWindowAsync(SidebarContext.RightClickedItem!.Path ?? string.Empty);
		}

		protected override void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(ISidebarContext.IsItemRightClicked):
				case nameof(ISidebarContext.RightClickedItem):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
