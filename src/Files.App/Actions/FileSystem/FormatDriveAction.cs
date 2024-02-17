// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class FormatDriveAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();

		public string Label
			=> "FormatDriveText".GetLocalizedResource();

		public string Description
			=> "FormatDriveDescription".GetLocalizedResource();

		public bool IsExecutable
			=> GetIsExecutable();

		public FormatDriveAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return Win32API.OpenFormatDriveDialog(HomePageContext.RightClickedItem?.Path ?? string.Empty);
		}

		private bool GetIsExecutable()
		{
			var executableInHomePage =
				HomePageContext.IsAnyItemRightClicked &&
				HomePageContext.RightClickedItem is WidgetDriveCardItem driveCardItem &&
				driveCardItem.Item is DriveItem driveItem &&
				driveItem.MenuOptions.ShowFormatDrive;

			return executableInHomePage;
		}

		public void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasItem))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
