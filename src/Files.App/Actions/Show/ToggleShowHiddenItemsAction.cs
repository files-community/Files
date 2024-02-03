// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class ToggleShowHiddenItemsAction : ObservableObject, IToggleAction
	{
		private IFoldersSettingsService FoldersSettingsService { get; } = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

		public string Label
			=> "ShowHiddenItems".GetLocalizedResource();

		public string Description
			=> "ToggleShowHiddenItemsDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.H, KeyModifiers.Ctrl);

		public bool IsOn
			=> FoldersSettingsService.ShowHiddenItems;

		public ToggleShowHiddenItemsAction()
		{
			FoldersSettingsService.PropertyChanged += Settings_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			FoldersSettingsService.ShowHiddenItems = !FoldersSettingsService.ShowHiddenItems;

			return Task.CompletedTask;
		}

		private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IFoldersSettingsService.ShowHiddenItems))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
