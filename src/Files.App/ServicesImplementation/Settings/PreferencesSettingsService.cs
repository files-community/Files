using Files.App.Serialization;
using Files.Backend.Services.Settings;
using Files.Shared.Enums;
using Files.Shared.EventArguments;
using Microsoft.AppCenter.Analytics;
using System.Collections.Generic;

namespace Files.App.ServicesImplementation.Settings
{
	internal sealed class PreferencesSettingsService : BaseObservableJsonSettings, IPreferencesSettingsService
	{
		public PreferencesSettingsService(ISettingsSharingContext settingsSharingContext)
		{
			// Register root
			RegisterSettingsContext(settingsSharingContext);
		}

		public bool ShowConfirmDeleteDialog
		{
			get => Get(true);
			set => Set(value);
		}


		public bool ShowFileExtensions
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowThumbnails
		{
			get => Get(true);
			set => Set(value);
		}

		public bool SelectFilesOnHover
		{
			get => Get(false);
			set => Set(value);
		}

		public bool SearchUnindexedItems
		{
			get => Get(false);
			set => Set(value);
		}

		public bool OpenSpecificPageOnStartup
		{
			get => Get(false);
			set => Set(value);
		}

		public string OpenSpecificPageOnStartupPath
		{
			get => Get(string.Empty);
			set => Set(value);
		}

		public bool ContinueLastSessionOnStartUp
		{
			get => Get(false);
			set => Set(value);
		}

		public bool OpenNewTabOnStartup
		{
			get => Get(true);
			set => Set(value);
		}

		public bool AlwaysOpenNewInstance
		{
			get => Get(false);
			set => Set(value);
		}

		public List<string> TabsOnStartupList
		{
			get => Get<List<string>>(null);
			set => Set(value);
		}

		public List<string> LastSessionTabList
		{
			get => Get<List<string>>(null);
			set => Set(value);
		}

		public DateTimeFormats DateTimeFormat
		{
			get => Get(DateTimeFormats.Application);
			set => Set(value);
		}

		protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			switch (e.SettingName)
			{
				case nameof(ShowConfirmDeleteDialog):
				case nameof(ShowFileExtensions):
				case nameof(SelectFilesOnHover):
				case nameof(SearchUnindexedItems):
				case nameof(OpenSpecificPageOnStartup):
				case nameof(ContinueLastSessionOnStartUp):
				case nameof(OpenNewTabOnStartup):
				case nameof(AlwaysOpenNewInstance):
					Analytics.TrackEvent($"Set {e.SettingName} to {e.NewValue}");
					break;
			}

			base.RaiseOnSettingChangedEvent(sender, e);
		}
	}
}
