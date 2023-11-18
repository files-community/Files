// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.Widgets;

namespace Files.App.Actions
{
	internal class FormatDriveAction : ObservableObject, IExtendedAction
	{
		private readonly IContentPageContext context;

		private readonly DrivesViewModel drivesViewModel;

		public string Label
			=> "FormatDriveText".GetLocalizedResource();

		public string Description
			=> "FormatDriveDescription".GetLocalizedResource();

		public bool IsExecutable =>
			(context.HasItem &&
			!context.HasSelection &&
			(drivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(x =>
				string.Equals(x.Path, context.Folder?.ItemPath))?.MenuOptions.ShowFormatDrive ?? false)) ||
			Parameter is not null;

		public object? Parameter { get; set; }

		public FormatDriveAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			if (Parameter is not null && Parameter is WidgetCardItem item)
			{
				return Win32API.OpenFormatDriveDialog(item.Path ?? string.Empty);
			}
			else
			{
				return Win32API.OpenFormatDriveDialog(context.Folder?.ItemPath ?? string.Empty);
			}
		}

		public void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasItem))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
