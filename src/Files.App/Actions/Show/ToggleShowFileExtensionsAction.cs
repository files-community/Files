// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class ToggleShowFileExtensionsAction : ObservableObject, IToggleAction
	{
		private IFoldersSettingsService FoldersSettingsService { get; } = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

		public string Label
			=> "ShowFileExtensions".GetLocalizedResource();

		public string Description
			=> "ToggleShowFileExtensionsDescription".GetLocalizedResource();

		public bool IsOn
			=> FoldersSettingsService.ShowFileExtensions;

		public ToggleShowFileExtensionsAction()
		{
			FoldersSettingsService.PropertyChanged += Settings_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			FoldersSettingsService.ShowFileExtensions = !FoldersSettingsService.ShowFileExtensions;

			return Task.CompletedTask;
		}

		private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IFoldersSettingsService.ShowFileExtensions))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
