// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class FormatDriveAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();
		private DrivesViewModel DrivesViewModel { get; } = Ioc.Default.GetRequiredService<DrivesViewModel>();

		private ActionExecutableType ExecutableType { get; set; }

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
			return ExecutableType switch
			{
				ActionExecutableType.DisplayPageContext
					=> Win32API.OpenFormatDriveDialog(ContentPageContext.Folder?.ItemPath ?? string.Empty),
				ActionExecutableType.HomePageContext
					=> Win32API.OpenFormatDriveDialog(HomePageContext.RightClickedItem?.Path ?? string.Empty),
				_ => Task.CompletedTask,
			};
		}

		private bool GetIsExecutable()
		{
			var executableInDisplayPage =
				ContentPageContext.HasItem &&
				!ContentPageContext.HasSelection &&
				(DrivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(x =>
					string.Equals(x.Path, ContentPageContext.Folder?.ItemPath))?.MenuOptions.ShowFormatDrive ?? false);

			if (executableInDisplayPage)
				ExecutableType = ActionExecutableType.DisplayPageContext;

			var executableInHomePage =
				HomePageContext.IsAnyItemRightClicked &&
				(DrivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(x =>
					string.Equals(x.Path, ContentPageContext.Folder?.ItemPath))?.MenuOptions.ShowFormatDrive ?? false);

			if (executableInHomePage)
				ExecutableType = ActionExecutableType.HomePageContext;

			return executableInDisplayPage || executableInHomePage;
		}

		public void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasItem))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
