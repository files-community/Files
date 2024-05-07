// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class OpenInNewPaneFromSidebarAction : BaseOpenInNewPaneAction
	{
		public override bool IsExecutable =>
			SidebarContext.IsItemRightClicked &&
			SidebarContext.RightClickedItem is not null;

		public override async Task ExecuteAsync()
		{
			if (await DriveHelpers.CheckEmptyDrive(HomePageContext.RightClickedItem!.Path))
				return;

			ContentPageContext.ShellPage!.PaneHolder?.OpenPathInNewPane(HomePageContext.RightClickedItem!.Path ?? string.Empty);
		}

		protected override void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(ISidebarContext.IsAnyItemRightClicked):
				case nameof(ISidebarContext.RightClickedItem):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
