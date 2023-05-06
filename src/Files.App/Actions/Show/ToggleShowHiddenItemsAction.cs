// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;

namespace Files.App.Actions
{
	internal class ToggleShowHiddenItemsAction : ObservableObject, IToggleAction
	{
		private readonly IFoldersSettingsService settings = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

		public string Label { get; } = "ShowHiddenItems".GetLocalizedResource();

		public string Description => "ToggleShowHiddenItemsDescription".GetLocalizedResource();

		public HotKey HotKey { get; } = new(Keys.H, KeyModifiers.Ctrl);

		public bool IsOn => settings.ShowHiddenItems;

		public ToggleShowHiddenItemsAction() => settings.PropertyChanged += Settings_PropertyChanged;

		public Task ExecuteAsync()
		{
			settings.ShowHiddenItems = !settings.ShowHiddenItems;
			return Task.CompletedTask;
		}

		private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IFoldersSettingsService.ShowHiddenItems))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
