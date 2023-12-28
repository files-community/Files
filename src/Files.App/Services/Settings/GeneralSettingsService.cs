// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.AppCenter.Analytics;

namespace Files.App.Services.Settings
{
	internal sealed class GeneralSettingsService : BaseObservableJsonSettings, IGeneralSettingsService
	{
		public GeneralSettingsService(ISettingsSharingContext settingsSharingContext)
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
			get => Get(true);
			set => Set(value);
		}

		public bool OpenNewTabOnStartup
		{
			get => Get(false);
			set => Set(value);
		}

		public bool OpenTabInExistingInstance
		{
			get => Get(true);
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

		public List<string> LastCrashedTabList
		{
			get => Get<List<string>>(null);
			set => Set(value);
		}

		public List<string> PathHistoryList
		{
			get => Get<List<string>>(null);
			set => Set(value);
		}

		public DateTimeFormats DateTimeFormat
		{
			get => Get(DateTimeFormats.Application);
			set => Set(value);
		}

		public bool AlwaysOpenDualPaneInNewTab
		{
			get => Get(false);
			set => Set(value);
		}

		public bool ShowQuickAccessWidget
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

		public bool ShowFileTagsWidget
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

		public bool FileTagsWidgetExpanded
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

		public bool MoveShellExtensionsToSubMenu
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowEditTagsMenu
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowSendToMenu
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowOpenInNewTab
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowOpenInNewWindow
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowOpenInNewPane
		{
			get => Get(true);
			set => Set(value);
		}

		public bool LeaveAppRunning
		{
#if STORE || STABLE || PREVIEW
			get => Get(true);
#else
			get => Get(false);
#endif
			set => Set(value);
		}

		public FileNameConflictResolveOptionType ConflictsResolveOption
		{
			get => (FileNameConflictResolveOptionType)Get((long)FileNameConflictResolveOptionType.GenerateNewName);
			set => Set((long)value);
		}

		public Dictionary<string, bool> ShowHashesDictionary
		{
			get => Get<Dictionary<string, bool>>(null);
			set => Set(value);
		}

		public Dictionary<string, string> Actions
		{
			get => Get<Dictionary<string, string>>(null) ?? new();
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
				case nameof(OpenTabInExistingInstance):
				case nameof(AlwaysOpenDualPaneInNewTab):
				case nameof(ShowQuickAccessWidget):
				case nameof(ShowRecentFilesWidget):
				case nameof(ShowDrivesWidget):
				case nameof(FoldersWidgetExpanded):
				case nameof(RecentFilesWidgetExpanded):
				case nameof(DrivesWidgetExpanded):
				case nameof(ShowFavoritesSection):
				case nameof(ShowLibrarySection):
				case nameof(ShowCloudDrivesSection):
				case nameof(ShowNetworkDrivesSection):
				case nameof(ShowWslSection):
				case nameof(ShowFileTagsSection):
				case nameof(MoveShellExtensionsToSubMenu):
				case nameof(ShowEditTagsMenu):
				case nameof(ShowSendToMenu):
				case nameof(ShowOpenInNewTab):
				case nameof(ShowOpenInNewWindow):
				case nameof(ShowOpenInNewPane):
				case nameof(LeaveAppRunning):
				case nameof(ConflictsResolveOption):
				case nameof(ShowHashesDictionary):
					Analytics.TrackEvent($"Set {e.SettingName} to {e.NewValue}");
					break;
			}

			base.RaiseOnSettingChangedEvent(sender, e);
		}
	}
}
