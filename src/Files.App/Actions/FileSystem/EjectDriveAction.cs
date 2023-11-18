// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.Widgets;

namespace Files.App.Actions
{
	internal class EjectDriveAction : ObservableObject, IExtendedAction
	{
		private IContentPageContext PageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private DrivesViewModel DrivesViewModel { get; } = Ioc.Default.GetRequiredService<DrivesViewModel>();

		public string Label
			=> "Eject".GetLocalizedResource();

		public string Description
			=> "EjectDescription".GetLocalizedResource();

		public bool IsExecutable =>
			(PageContext.HasItem &&
			!PageContext.HasSelection &&
			(DrivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(x =>
				string.Equals(x.Path, PageContext.Folder?.ItemPath))?.MenuOptions.ShowEjectDevice ?? false)) ||
			Parameter is not null;

		public object? Parameter { get; set; }

		public EjectDriveAction()
		{
			PageContext.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			if (Parameter is null || Parameter is not WidgetCardItem item)
				return;

			var result = await DriveHelpers.EjectDeviceAsync(item.Path);

			await UIHelpers.ShowDeviceEjectResultAsync(((DriveCardItem)item).Item.Type, result);
		}

		public void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasItem))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
