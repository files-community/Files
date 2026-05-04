// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Settings;

public sealed partial class Settings
{
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
