// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Settings;

public sealed partial class Settings
{
	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool OpenSpecificPageOnStartup { get; set; }

	[GeneratedSettingsProperty(DefaultValue = "")]
	public partial string OpenSpecificPageOnStartupPath { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ContinueLastSessionOnStartUp { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool OpenNewTabOnStartup { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool OpenTabInExistingInstance { get; set; }

	[GeneratedSettingsProperty]
	public partial List<string>? TabsOnStartupList { get; set; }

	[GeneratedSettingsProperty]
	public partial List<string>? LastSessionTabList { get; set; }

	[GeneratedSettingsProperty]
	public partial List<string>? LastCrashedTabList { get; set; }

	[GeneratedSettingsProperty]
	public partial List<string>? PathHistoryList { get; set; }

	[GeneratedSettingsProperty]
	public partial List<string>? PreviousSearchQueriesList { get; set; }

	[GeneratedSettingsProperty]
	public partial List<string>? PreviousArchiveExtractionLocations { get; set; }

	[GeneratedSettingsProperty(DefaultValue = DateTimeFormats.Application)]
	public partial DateTimeFormats DateTimeFormat { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool AlwaysOpenDualPaneInNewTab { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool AlwaysSwitchToNewlyOpenedTab { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowQuickAccessWidget { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowRecentFilesWidget { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowDrivesWidget { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowNetworkLocationsWidget { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool ShowFileTagsWidget { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool FoldersWidgetExpanded { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool RecentFilesWidgetExpanded { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool DrivesWidgetExpanded { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool NetworkLocationsWidgetExpanded { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool FileTagsWidgetExpanded { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowPinnedSection { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool IsPinnedSectionExpanded { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool ShowLibrarySection { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool IsLibrarySectionExpanded { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowDrivesSection { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool IsDriveSectionExpanded { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowCloudDrivesSection { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool IsCloudDriveSectionExpanded { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowNetworkSection { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool IsNetworkSectionExpanded { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowWslSection { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool IsWslSectionExpanded { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowFileTagsSection { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool IsFileTagsSectionExpanded { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool MoveShellExtensionsToSubMenu { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowPinToSideBar { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowPinToStart { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowEditTagsMenu { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowCompressionOptions { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool ShowFlattenOptions { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowSendToMenu { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowOpenInNewTab { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowOpenInNewWindow { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowOpenInNewPane { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowOpenTerminal { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowCopyPath { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowCreateFolderWithSelection { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool ShowCreateAlternateDataStream { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowCreateShortcut { get; set; }

#if DEBUG
	[GeneratedSettingsProperty(DefaultValue = false)]
#else
	[GeneratedSettingsProperty(DefaultValue = true)]
#endif
	public partial bool LeaveAppRunning { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowSystemTrayIcon { get; set; }

	[GeneratedSettingsProperty(DefaultValue = FileNameConflictResolveOptionType.GenerateNewName)]
	public partial FileNameConflictResolveOptionType ConflictsResolveOption { get; set; }

	[GeneratedSettingsProperty(DefaultValue = ArchiveFormats.Zip)]
	public partial ArchiveFormats ArchiveFormatsOption { get; set; }

	[GeneratedSettingsProperty(DefaultValue = ArchiveCompressionLevels.Normal)]
	public partial ArchiveCompressionLevels ArchiveCompressionLevelsOption { get; set; }

	[GeneratedSettingsProperty(DefaultValue = ArchiveSplittingSizes.None)]
	public partial ArchiveSplittingSizes ArchiveSplittingSizesOption { get; set; }

	[GeneratedSettingsProperty(DefaultValue = ArchiveDictionarySizes.Auto)]
	public partial ArchiveDictionarySizes ArchiveDictionarySizesOption { get; set; }

	[GeneratedSettingsProperty(DefaultValue = ArchiveWordSizes.Auto)]
	public partial ArchiveWordSizes ArchiveWordSizesOption { get; set; }

	[GeneratedSettingsProperty]
	public partial Dictionary<string, bool>? ShowHashesDictionary { get; set; }

	[GeneratedSettingsProperty(DefaultValueCallback = nameof(GetDefaultUserId))]
	public partial string UserId { get; set; }

	[GeneratedSettingsProperty(DefaultValue = ShellPaneArrangement.Vertical)]
	public partial ShellPaneArrangement ShellPaneArrangementOption { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool ShowShelfPane { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool ShowFilterHeader { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool EnableThumbnailCache { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool EnableSmoothScrolling { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 512d)]
	public partial double ThumbnailCacheSizeLimit { get; set; }

	private static string GetDefaultUserId()
	{
		return Guid.NewGuid().ToString();
	}
}
