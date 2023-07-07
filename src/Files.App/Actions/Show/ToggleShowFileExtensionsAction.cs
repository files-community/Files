﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class ToggleShowFileExtensionsAction : ObservableObject, IToggleAction
	{
		private readonly IFoldersSettingsService settings;

		public string Label
			=> "ShowFileExtensions".GetLocalizedResource();

		public string Description
			=> "ToggleShowFileExtensionsDescription".GetLocalizedResource();

		public bool IsOn
			=> settings.ShowFileExtensions;

		public ToggleShowFileExtensionsAction()
		{
			settings = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

			settings.PropertyChanged += Settings_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			settings.ShowFileExtensions = !settings.ShowFileExtensions;

			return Task.CompletedTask;
		}

		private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IFoldersSettingsService.ShowFileExtensions))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
