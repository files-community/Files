// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class OpenInNewPaneFromHomeAction : BaseOpenInNewPaneAction
	{
		public override bool IsExecutable =>
			HomePageContext.IsAnyItemRightClicked &&
			HomePageContext.RightClickedItem is not null;

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
				case nameof(IHomePageContext.IsAnyItemRightClicked):
				case nameof(IHomePageContext.RightClickedItem):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
