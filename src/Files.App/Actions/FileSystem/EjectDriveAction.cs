// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class EjectDriveAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();
		private DrivesViewModel DrivesViewModel { get; } = Ioc.Default.GetRequiredService<DrivesViewModel>();

		public string Label
			=> "FormatDriveText".GetLocalizedResource();

		public string Description
			=> "FormatDriveDescription".GetLocalizedResource();

		public bool IsExecutable
			=> GetIsExecutable();

		public EjectDriveAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			var result = await DriveHelpers.EjectDeviceAsync(HomePageContext.RightClickedItem?.Path ?? string.Empty);
			await UIHelpers.ShowDeviceEjectResultAsync((HomePageContext.RightClickedItem?.Item as WidgetDriveCardItem)!.Item.Type, result);
		}

		private bool GetIsExecutable()
		{
			var executableInHomePage =
				HomePageContext.IsAnyItemRightClicked &&
				HomePageContext.RightClickedItem?.Item is WidgetDriveCardItem &&
				(DrivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(x =>
					string.Equals(x.Path, HomePageContext.RightClickedItem?.Path))?.MenuOptions.ShowFormatDrive ?? false);

			return executableInHomePage;
		}

		public void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasItem))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
