﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class OpenInNewWindowFromHomeAction : BaseOpenInNewWindowAction
	{
		public override HotKey HotKey
			=> HotKey.None;

		public override bool IsExecutable =>
			UserSettingsService.GeneralSettingsService.ShowOpenInNewWindow &&
			HomePageContext.IsAnyItemRightClicked &&
			HomePageContext.RightClickedItem is not null &&
			(HomePageContext.RightClickedItem is WidgetFileTagCardItem fileTagItem
				? fileTagItem.IsFolder
				: true);

		public override bool IsAccessibleGlobally
			=> false;

		public override Task ExecuteAsync(object? parameter = null)
		{
			return NavigationHelpers.OpenPathInNewWindowAsync(HomePageContext.RightClickedItem!.Path ?? string.Empty);
		}

		protected override void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IHomePageContext.IsAnyItemRightClicked):
				case nameof(IHomePageContext.RightClickedItem):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
