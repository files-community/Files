// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Text.Json.Nodes;

namespace Files.App.Data.Settings;

public sealed partial class Settings : BaseJsonSettings
{
	private static readonly Lazy<Settings> lazyDefault = new(() => new Settings(initialize: true));
	public static Settings Default => lazyDefault.Value;

	public Settings() : this(initialize: false)
	{
	}

	private Settings(bool initialize) : base("settings.json")
	{
		if (initialize)
			Initialize();
	}

	[GeneratedSettingsProperty]
	public partial List<ActionWithParameterItem>? ActionsV2 { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowStatusCenterTeachingTip { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowBackgroundRunningNotification { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool RestoreTabsOnStartup { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 255d, GetValueCallback = nameof(GetSidebarWidth))]
	public partial double SidebarWidth { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool IsSidebarOpen { get; set; }

	[GeneratedSettingsProperty(DefaultValue = "Default")]
	public partial string AppThemeMode { get; set; }

	[GeneratedSettingsProperty(DefaultValue = "#00000000")]
	public partial string AppThemeBackgroundColor { get; set; }

	[GeneratedSettingsProperty(DefaultValue = "")]
	public partial string AppThemeAddressBarBackgroundColor { get; set; }

	[GeneratedSettingsProperty(DefaultValue = "")]
	public partial string AppThemeToolbarBackgroundColor { get; set; }

	[GeneratedSettingsProperty(DefaultValue = "")]
	public partial string AppThemeSidebarBackgroundColor { get; set; }

	[GeneratedSettingsProperty(DefaultValue = "")]
	public partial string AppThemeFileAreaBackgroundColor { get; set; }

	[GeneratedSettingsProperty(DefaultValue = "")]
	public partial string AppThemeFileAreaSecondaryBackgroundColor { get; set; }

	[GeneratedSettingsProperty(DefaultValue = "")]
	public partial string AppThemeInfoPaneBackgroundColor { get; set; }

	[GeneratedSettingsProperty(DefaultValueCallback = nameof(GetDefaultAppThemeFontFamily))]
	public partial string AppThemeFontFamily { get; set; }

	[GeneratedSettingsProperty(DefaultValue = BackdropMaterialType.MicaAlt)]
	public partial BackdropMaterialType AppThemeBackdropMaterial { get; set; }

	[GeneratedSettingsProperty(DefaultValue = "")]
	public partial string AppThemeBackgroundImageSource { get; set; }

	[GeneratedSettingsProperty(DefaultValue = Stretch.UniformToFill)]
	public partial Stretch AppThemeBackgroundImageFit { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 1f)]
	public partial float AppThemeBackgroundImageOpacity { get; set; }

	[GeneratedSettingsProperty(DefaultValue = VerticalAlignment.Center)]
	public partial VerticalAlignment AppThemeBackgroundImageVerticalAlignment { get; set; }

	[GeneratedSettingsProperty(DefaultValue = HorizontalAlignment.Center)]
	public partial HorizontalAlignment AppThemeBackgroundImageHorizontalAlignment { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowToolbar { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowStatusBar { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowTabActions { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool ShowShelfPaneToggleButton { get; set; }

	[GeneratedSettingsProperty(DefaultValue = StatusCenterVisibility.Always)]
	public partial StatusCenterVisibility StatusCenterVisibility { get; set; }

	[GeneratedSettingsProperty]
	public partial Dictionary<string, List<ToolbarItemSettingsEntry>>? CustomToolbarItems { get; set; }

	[GeneratedSettingsProperty]
	public partial Dictionary<string, List<string>>? LastKnownToolbarDefaults { get; set; }

	private static double GetSidebarWidth(double value)
	{
		return Math.Min(Math.Max(value, Constants.UI.MinimumSidebarWidth), 500d);
	}

	private static string GetDefaultAppThemeFontFamily()
	{
		return Constants.Appearance.StandardFont;
	}

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool HasClickedReviewPrompt { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool HasClickedSponsorPrompt { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowRunningAsAdminPrompt { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowDataStreamsAreHiddenPrompt { get; set; }

	[GeneratedSettingsProperty(DefaultValue = OpenInIDEOption.GitRepos)]
	public partial OpenInIDEOption OpenInIDEOption { get; set; }

	[GeneratedSettingsProperty(DefaultValueCallback = nameof(GetDefaultIDEPath))]
	public partial string IDEPath { get; set; }

	[GeneratedSettingsProperty(DefaultValueCallback = nameof(GetDefaultIDEName))]
	public partial string IDEName { get; set; }

	private static string GetDefaultIDEPath()
	{
		return SoftwareHelpers.IsVSCodeInstalled() ? "code" : string.Empty;
	}

	private static string GetDefaultIDEName()
	{
		return SoftwareHelpers.IsVSCodeInstalled() ? Strings.VisualStudioCode.GetLocalizedResource() : string.Empty;
	}

	[GeneratedSettingsProperty]
	public partial List<TagViewModel>? FileTagList { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool ShowHiddenItems { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool ShowProtectedSystemFiles { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool AreAlternateStreamsVisible { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowDotFiles { get; set; }

	[GeneratedSettingsProperty(DefaultValue = SingleClickOpenMode.OnlyForTouch, MigrateValueCallback = nameof(MigrateLegacySingleClickSettings))]
	public partial SingleClickOpenMode OpenFilesWithSingleClick { get; set; }

	[GeneratedSettingsProperty(DefaultValue = SingleClickOpenMode.OnlyForTouch)]
	public partial SingleClickOpenMode OpenFoldersWithSingleClick { get; set; }

	[GeneratedSettingsProperty(DefaultValue = SingleClickOpenMode.Always)]
	public partial SingleClickOpenMode OpenFoldersInColumnsViewWithSingleClick { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool OpenFoldersInNewTab { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ScrollToPreviousFolderWhenNavigatingUp { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool CalculateFolderSizes { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowFileExtensions { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowThumbnails { get; set; }

	[GeneratedSettingsProperty(DefaultValue = DeleteConfirmationPolicies.Always)]
	public partial DeleteConfirmationPolicies DeleteConfirmationPolicy { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool SelectFilesOnHover { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool DoubleClickToGoUp { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowFileExtensionWarning { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowCheckboxesWhenSelectingItems { get; set; }

	[GeneratedSettingsProperty(DefaultValue = SizeUnitTypes.BinaryUnits)]
	public partial SizeUnitTypes SizeUnitFormat { get; set; }

	private void MigrateLegacySingleClickSettings(JsonObject settings)
	{
		if (settings.TryGetPropertyValue("OpenItemsWithOneClick", out var openItemsWithOneClick) &&
			openItemsWithOneClick is not null)
		{
			var legacy = openItemsWithOneClick.GetValue<bool>();
			OpenFilesWithSingleClick = legacy
				? SingleClickOpenMode.Always
				: SingleClickOpenMode.Never;
		}

		if (settings.TryGetPropertyValue("OpenFoldersWithOneClick", out var openFoldersWithOneClick) &&
			openFoldersWithOneClick is not null)
		{
			var legacy = openFoldersWithOneClick.GetValue<int>();
			switch (legacy)
			{
				case 0:
					OpenFoldersWithSingleClick = SingleClickOpenMode.Never;
					OpenFoldersInColumnsViewWithSingleClick = SingleClickOpenMode.Always;
					break;
				case 1:
					OpenFoldersWithSingleClick = SingleClickOpenMode.Always;
					OpenFoldersInColumnsViewWithSingleClick = SingleClickOpenMode.Always;
					break;
				case 2:
					OpenFoldersWithSingleClick = SingleClickOpenMode.Never;
					OpenFoldersInColumnsViewWithSingleClick = SingleClickOpenMode.Never;
					break;
			}
		}
	}

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

	[GeneratedSettingsProperty(ExportIgnore = true)]
	public partial List<string>? LastSessionTabList { get; set; }

	[GeneratedSettingsProperty(ExportIgnore = true)]
	public partial List<string>? LastCrashedTabList { get; set; }

	[GeneratedSettingsProperty(ExportIgnore = true)]
	public partial List<string>? PathHistoryList { get; set; }

	[GeneratedSettingsProperty(ExportIgnore = true)]
	public partial List<string>? PreviousSearchQueriesList { get; set; }

	[GeneratedSettingsProperty(ExportIgnore = true)]
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

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool IsInfoPaneEnabled { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 300d, GetValueCallback = nameof(GetInfoPaneSize))]
	public partial double HorizontalSizePx { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 250d, GetValueCallback = nameof(GetInfoPaneSize))]
	public partial double VerticalSizePx { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 1d, GetValueCallback = nameof(GetMediaVolume))]
	public partial double MediaVolume { get; set; }

	[GeneratedSettingsProperty(DefaultValue = InfoPaneTabs.Details)]
	public partial InfoPaneTabs SelectedTab { get; set; }

	private static double GetInfoPaneSize(double value)
	{
		return Math.Max(100d, value);
	}

	private static double GetMediaVolume(double value)
	{
		return Math.Min(Math.Max(value, 0d), 1d);
	}

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool SyncFolderPreferencesAcrossDirectories { get; set; }

	[GeneratedSettingsProperty(DefaultValue = FolderLayoutModes.Adaptive)]
	public partial FolderLayoutModes DefaultLayoutMode { get; set; }

	[GeneratedSettingsProperty(DefaultValue = SortOption.Name)]
	public partial SortOption DefaultSortOption { get; set; }

	[GeneratedSettingsProperty(DefaultValue = SortDirection.Ascending)]
	public partial SortDirection DefaultDirectorySortDirection { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool DefaultSortDirectoriesAlongsideFiles { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool DefaultSortFilesFirst { get; set; }

	[GeneratedSettingsProperty(DefaultValue = GroupOption.None)]
	public partial GroupOption DefaultGroupOption { get; set; }

	[GeneratedSettingsProperty(DefaultValue = SortDirection.Ascending)]
	public partial SortDirection DefaultDirectoryGroupDirection { get; set; }

	[GeneratedSettingsProperty(DefaultValue = GroupByDateUnit.Year)]
	public partial GroupByDateUnit DefaultGroupByDateUnit { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 80d)]
	public partial double GitStatusColumnWidth { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 140d)]
	public partial double GitLastCommitDateColumnWidth { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 140d)]
	public partial double GitLastCommitMessageColumnWidth { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 140d)]
	public partial double GitCommitAuthorColumnWidth { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 80d)]
	public partial double GitLastCommitShaColumnWidth { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 140d)]
	public partial double TagColumnWidth { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 240d)]
	public partial double NameColumnWidth { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 200d)]
	public partial double DateModifiedColumnWidth { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 140d)]
	public partial double TypeColumnWidth { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 200d)]
	public partial double DateCreatedColumnWidth { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 100d)]
	public partial double SizeColumnWidth { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 200d)]
	public partial double DateDeletedColumnWidth { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 200d)]
	public partial double PathColumnWidth { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 200d)]
	public partial double OriginalPathColumnWidth { get; set; }

	[GeneratedSettingsProperty(DefaultValue = 50d)]
	public partial double SyncStatusColumnWidth { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowDateColumn { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool ShowDateCreatedColumn { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowTypeColumn { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowSizeColumn { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool ShowGitStatusColumn { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool ShowGitLastCommitDateColumn { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool ShowGitLastCommitMessageColumn { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool ShowGitCommitAuthorColumn { get; set; }

	[GeneratedSettingsProperty(DefaultValue = false)]
	public partial bool ShowGitLastCommitShaColumn { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowFileTagColumn { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowDateDeletedColumn { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowPathColumn { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowOriginalPathColumn { get; set; }

	[GeneratedSettingsProperty(DefaultValue = true)]
	public partial bool ShowSyncStatusColumn { get; set; }

	[GeneratedSettingsProperty(DefaultValue = DetailsViewSizeKind.Small)]
	public partial DetailsViewSizeKind DetailsViewSize { get; set; }

	[GeneratedSettingsProperty(DefaultValue = ListViewSizeKind.Small)]
	public partial ListViewSizeKind ListViewSize { get; set; }

	[GeneratedSettingsProperty(DefaultValue = CardsViewSizeKind.Small)]
	public partial CardsViewSizeKind CardsViewSize { get; set; }

	[GeneratedSettingsProperty(DefaultValue = GridViewSizeKind.Large)]
	public partial GridViewSizeKind GridViewSize { get; set; }

	[GeneratedSettingsProperty(DefaultValue = ColumnsViewSizeKind.Small)]
	public partial ColumnsViewSizeKind ColumnsViewSize { get; set; }
}
