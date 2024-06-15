﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class OpenInNewWindowFromSidebarAction : BaseOpenInNewWindowAction
	{
		public override HotKey HotKey
			=> HotKey.None;

		public override bool IsExecutable =>
			UserSettingsService.GeneralSettingsService.ShowOpenInNewWindow &&
			SidebarContext.IsItemRightClicked &&
			SidebarContext.RightClickedItem is not null &&
			SidebarContext.RightClickedItem.MenuOptions.IsLocationItem;

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
