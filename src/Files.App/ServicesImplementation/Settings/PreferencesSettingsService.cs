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

		public bool IsDualPaneEnabled
		{
			get => Get(false);
			set => Set(value);
		}

		public bool AlwaysOpenDualPaneInNewTab
		{
			get => Get(false);
			set => Set(value);
		}

		public bool ShowFoldersWidget
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowRecentFilesWidget
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowDrivesWidget
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowBundlesWidget
		{
			get => Get(false);
			set => Set(value);
		}

		public bool FoldersWidgetExpanded
		{
			get => Get(true);
			set => Set(value);
		}

		public bool RecentFilesWidgetExpanded
		{
			get => Get(true);
			set => Set(value);
		}

		public bool DrivesWidgetExpanded
		{
			get => Get(true);
			set => Set(value);
		}

		public bool BundlesWidgetExpanded
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowFavoritesSection
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowLibrarySection
		{
			get => Get(false);
			set => Set(value);
		}

		public bool ShowDrivesSection
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowCloudDrivesSection
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowNetworkDrivesSection
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowWslSection
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowFileTagsSection
		{
			get => Get(true);
			set => Set(value);
		}

		protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			switch (e.SettingName)
			{
				case nameof(SearchUnindexedItems):
				case nameof(OpenSpecificPageOnStartup):
				case nameof(ContinueLastSessionOnStartUp):
				case nameof(OpenNewTabOnStartup):
				case nameof(AlwaysOpenNewInstance):
				case nameof(IsDualPaneEnabled):
				case nameof(AlwaysOpenDualPaneInNewTab):
				case nameof(ShowFoldersWidget):
				case nameof(ShowRecentFilesWidget):
				case nameof(ShowDrivesWidget):
				case nameof(ShowBundlesWidget):
				case nameof(FoldersWidgetExpanded):
				case nameof(RecentFilesWidgetExpanded):
				case nameof(BundlesWidgetExpanded):
				case nameof(DrivesWidgetExpanded):
				case nameof(ShowFavoritesSection):
				case nameof(ShowLibrarySection):
				case nameof(ShowCloudDrivesSection):
				case nameof(ShowNetworkDrivesSection):
				case nameof(ShowWslSection):
				case nameof(ShowFileTagsSection):
					Analytics.TrackEvent($"Set {e.SettingName} to {e.NewValue}");
					break;
			}

			base.RaiseOnSettingChangedEvent(sender, e);
		}
	}
}
