// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class ToggleDotFilesSettingAction : ObservableObject, IToggleAction
	{
		private readonly IFoldersSettingsService settings;

		public string Label
			=> "ShowDotFiles".GetLocalizedResource();

		public string Description
			=> "ToggleDotFilesSettingDescription".GetLocalizedResource();

		public bool IsOn
			=> settings.ShowDotFiles;

		public ToggleDotFilesSettingAction()
		{
			settings = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

			settings.PropertyChanged += Settings_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			settings.ShowDotFiles = !settings.ShowDotFiles;

			return Task.CompletedTask;
		}

		private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IFoldersSettingsService.ShowDotFiles))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
