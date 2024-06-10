// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Services.Settings
{
	internal sealed class GeneralSettingsService : BaseObservableJsonSettings, IGeneralSettingsService
	{
		public GeneralSettingsService(ISettingsSharingContext settingsSharingContext)
		{
			// Register root
			RegisterSettingsContext(settingsSharingContext);
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

		public bool ShowNetworkLocationsWidget
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

		public bool NetworkLocationsWidgetExpanded
		{
			get => Get(false);
			set => Set(value);
		}

		public bool FileTagsWidgetExpanded
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowPinnedSection
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

		public bool ShowNetworkSection
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

		public bool ShowCompressionOptions
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

		public bool ShowCopyPath
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowCreateFolderWithSelection
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowCreateShortcut
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

		public string UserId
		{
			get => Get(Guid.NewGuid().ToString());
			set => Set(value);
		}

		protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			base.RaiseOnSettingChangedEvent(sender, e);
		}
	}
}
