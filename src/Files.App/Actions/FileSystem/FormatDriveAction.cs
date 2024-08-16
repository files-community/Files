// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class FormatDriveAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		private readonly IRemovableDrivesService StorageDevicesService = Ioc.Default.GetRequiredService<IRemovableDrivesService>();

		public string Label
			=> "FormatDriveText".GetLocalizedResource();

		public string Description
			=> "FormatDriveDescription".GetLocalizedResource();

		public bool IsExecutable =>
			context.HasItem &&
			!context.HasSelection &&
			(StorageDevicesService.Drives.Cast<DriveItem>().FirstOrDefault(x =>
				string.Equals(x.Path, context.Folder?.ItemPath))?.MenuOptions.ShowFormatDrive ?? false);

		public FormatDriveAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			return Win32Helper.OpenFormatDriveDialog(context.Folder?.ItemPath ?? string.Empty);
		}

		public void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasItem))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
