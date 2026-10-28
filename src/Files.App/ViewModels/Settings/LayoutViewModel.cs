// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.ViewModels.Settings
{
	public sealed partial class LayoutViewModel : ObservableObject
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private ILayoutTemplatesService LayoutTemplatesService { get; } = Ioc.Default.GetRequiredService<ILayoutTemplatesService>();

		public LayoutViewModel()
		{
			DefaultTemplate = new(LayoutTemplatesService, LayoutTemplatesService.DefaultTemplate.Id);
		}

		internal void StartListening()
		{
			UserSettingsService.OnSettingChangedEvent -= OnSharedSettingChanged;
			UserSettingsService.OnSettingChangedEvent += OnSharedSettingChanged;
			RefreshSettings();
		}

		internal void StopListening() => UserSettingsService.OnSettingChangedEvent -= OnSharedSettingChanged;

		private void OnSharedSettingChanged(object? sender, SettingChangedEventArgs e)
		{
			if (e.NewValue is JsonElement)
				RefreshSettings(e.SettingName);
		}

		private void RefreshSettings(string? name = null)
		{
			if (name is null or nameof(ILayoutSettingsService.LayoutTemplates))
				DefaultTemplate.Refresh();
			if (name is null or nameof(ILayoutSettingsService.SyncFolderPreferencesAcrossDirectories))
				OnPropertyChanged(nameof(SyncFolderPreferencesAcrossDirectories));
			if (name is null or nameof(ILayoutSettingsService.AutoSizeColumnsInDetailsLayout))
				OnPropertyChanged(nameof(AutoSizeColumnsInDetailsLayout));
		}

		// Properties

		public bool SyncFolderPreferencesAcrossDirectories
		{
			get => UserSettingsService.LayoutSettingsService.SyncFolderPreferencesAcrossDirectories;
			set
			{
				if (value != UserSettingsService.LayoutSettingsService.SyncFolderPreferencesAcrossDirectories)
				{
					UserSettingsService.LayoutSettingsService.SyncFolderPreferencesAcrossDirectories = value;

					ResetLayoutPreferences();
					OnPropertyChanged();
				}
			}
		}

		/// <summary>
		/// Gets the template of folders that no rule matches.
		/// </summary>
		public LayoutTemplateViewModel DefaultTemplate { get; }

		// Details view

		public bool AutoSizeColumnsInDetailsLayout
		{
			get => UserSettingsService.LayoutSettingsService.AutoSizeColumnsInDetailsLayout;
			set
			{
				if (value != UserSettingsService.LayoutSettingsService.AutoSizeColumnsInDetailsLayout)
				{
					UserSettingsService.LayoutSettingsService.AutoSizeColumnsInDetailsLayout = value;

					OnPropertyChanged();
				}
			}
		}

		// Methods

		public void ResetLayoutPreferences()
		{
			// Is this proper practice?
			var dbInstance = LayoutPreferencesManager.GetDatabaseManagerInstance();

			dbInstance.ResetAll();
		}
	}
}
