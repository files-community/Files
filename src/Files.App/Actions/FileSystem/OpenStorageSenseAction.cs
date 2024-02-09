// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class OpenStorageSenseAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context;

		private readonly DrivesViewModel drivesViewModel;

		public string Label
			=> "OpenStorageSense".GetLocalizedResource();

		public string Description
			=> "OpenStorageSense".GetLocalizedResource();

		public RichGlyph Glyph
			=> new();

		public override bool IsExecutable =>
			context.HasItem &&
			!context.HasSelection &&
			(drivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(x =>
				string.Equals(x.Path, context.Folder?.ItemPath))?.MenuOptions.ShowStorageSense ?? false);

		public OpenStorageSenseAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			await StorageSenseHelper.OpenStorageSenseAsync(context.Folder?.ItemPath ?? string.Empty);
		}

		public void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasItem))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
