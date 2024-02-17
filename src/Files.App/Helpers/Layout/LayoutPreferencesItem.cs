// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Helpers
{
	/// <summary>
	/// Represents item for a folder's layout preferences.
	/// </summary>
	public class LayoutPreferencesItem
	{
		// Dependency injections

		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		// Fields

		public ColumnsViewModel ColumnsViewModel;

		public bool SortDirectoriesAlongsideFiles;
		public bool SortFilesFirst;
		public bool IsAdaptiveLayoutOverridden;

		// Icon heights
		public int IconHeightDetailsView;
		public int IconHeightListView;
		public int IconHeightTilesView;
		public int IconHeightGridView;
		public int IconHeightColumnsView;

		public FolderLayoutModes LayoutMode;

		public SortOption DirectorySortOption;
		public SortDirection DirectorySortDirection;
		public SortDirection DirectoryGroupDirection;

		public GroupOption DirectoryGroupOption;
		public GroupByDateUnit DirectoryGroupByDateUnit;

		// Constructor

		public LayoutPreferencesItem()
		{
			var defaultLayout = UserSettingsService.FoldersSettingsService.DefaultLayoutMode;

			LayoutMode = defaultLayout is FolderLayoutModes.Adaptive ? FolderLayoutModes.DetailsView : defaultLayout;
			IconHeightDetailsView = UserSettingsService.LayoutSettingsService.DefaultIconHeightDetailsView;
			IconHeightListView = UserSettingsService.LayoutSettingsService.DefaultIconHeightListView;
			IconHeightTilesView = UserSettingsService.LayoutSettingsService.DefaulIconHeightTilesView;
			IconHeightGridView = UserSettingsService.LayoutSettingsService.DefaulIconHeightGridView;
			IconHeightColumnsView = UserSettingsService.LayoutSettingsService.DefaultIconHeightColumnsView;
			DirectorySortOption = UserSettingsService.FoldersSettingsService.DefaultSortOption;
			DirectoryGroupOption = UserSettingsService.FoldersSettingsService.DefaultGroupOption;
			DirectorySortDirection = UserSettingsService.FoldersSettingsService.DefaultDirectorySortDirection;
			DirectoryGroupDirection = UserSettingsService.FoldersSettingsService.DefaultDirectoryGroupDirection;
			DirectoryGroupByDateUnit = UserSettingsService.FoldersSettingsService.DefaultGroupByDateUnit;
			SortDirectoriesAlongsideFiles = UserSettingsService.FoldersSettingsService.DefaultSortDirectoriesAlongsideFiles;
			SortFilesFirst = UserSettingsService.FoldersSettingsService.DefaultSortFilesFirst;
			IsAdaptiveLayoutOverridden = defaultLayout is not FolderLayoutModes.Adaptive;

			ColumnsViewModel = new ColumnsViewModel();
			ColumnsViewModel.DateCreatedColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowDateCreatedColumn;
			ColumnsViewModel.DateModifiedColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowDateColumn;
			ColumnsViewModel.ItemTypeColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowTypeColumn;
			ColumnsViewModel.SizeColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowSizeColumn;
			ColumnsViewModel.GitStatusColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowGitStatusColumn;
			ColumnsViewModel.GitLastCommitDateColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowGitLastCommitDateColumn;
			ColumnsViewModel.GitLastCommitMessageColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowGitLastCommitMessageColumn;
			ColumnsViewModel.GitCommitAuthorColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowGitCommitAuthorColumn;
			ColumnsViewModel.GitLastCommitShaColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowGitLastCommitShaColumn;
			ColumnsViewModel.TagColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowFileTagColumn;
			ColumnsViewModel.DateDeletedColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowDateDeletedColumn;
			ColumnsViewModel.PathColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowPathColumn;
			ColumnsViewModel.OriginalPathColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowOriginalPathColumn;
			ColumnsViewModel.StatusColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowSyncStatusColumn;

			ColumnsViewModel.NameColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.NameColumnWidth;
			ColumnsViewModel.DateModifiedColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.DateModifiedColumnWidth;
			ColumnsViewModel.DateCreatedColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.DateCreatedColumnWidth;
			ColumnsViewModel.ItemTypeColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.TypeColumnWidth;
			ColumnsViewModel.SizeColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.SizeColumnWidth;
			ColumnsViewModel.GitStatusColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.GitStatusColumnWidth;
			ColumnsViewModel.GitLastCommitDateColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.GitLastCommitDateColumnWidth;
			ColumnsViewModel.GitLastCommitMessageColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.GitLastCommitMessageColumnWidth;
			ColumnsViewModel.GitCommitAuthorColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.GitCommitAuthorColumnWidth;
			ColumnsViewModel.GitLastCommitShaColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.GitLastCommitShaColumnWidth;
			ColumnsViewModel.TagColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.TagColumnWidth;
			ColumnsViewModel.DateDeletedColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.DateDeletedColumnWidth;
			ColumnsViewModel.PathColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.PathColumnWidth;
			ColumnsViewModel.OriginalPathColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.OriginalPathColumnWidth;
			ColumnsViewModel.StatusColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.SyncStatusColumnWidth;
		}

		// Overridden methods

		public override bool Equals(object? obj)
		{
			if (obj is null)
				return false;

			if (obj == this)
				return true;

			if (obj is LayoutPreferencesItem item)
			{
				return (
					item.LayoutMode == LayoutMode &&
					item.IconHeightDetailsView == IconHeightDetailsView &&
					item.IconHeightListView == IconHeightListView &&
					item.IconHeightTilesView == IconHeightTilesView &&
					item.IconHeightGridView == IconHeightGridView &&
					item.IconHeightColumnsView == IconHeightColumnsView &&
					item.DirectoryGroupOption == DirectoryGroupOption &&
					item.DirectorySortOption == DirectorySortOption &&
					item.DirectorySortDirection == DirectorySortDirection &&
					item.DirectoryGroupDirection == DirectoryGroupDirection &&
					item.DirectoryGroupByDateUnit == DirectoryGroupByDateUnit &&
					item.SortDirectoriesAlongsideFiles == SortDirectoriesAlongsideFiles &&
					item.SortFilesFirst == SortFilesFirst &&
					item.IsAdaptiveLayoutOverridden == IsAdaptiveLayoutOverridden &&
					item.ColumnsViewModel.Equals(ColumnsViewModel));
			}
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			HashCode hash = new();

			hash.Add(LayoutMode);
			hash.Add(IconHeightDetailsView);
			hash.Add(IconHeightListView);
			hash.Add(IconHeightTilesView);
			hash.Add(IconHeightGridView);
			hash.Add(IconHeightColumnsView);
			hash.Add(DirectoryGroupOption);
			hash.Add(DirectorySortOption);
			hash.Add(DirectorySortDirection);
			hash.Add(DirectoryGroupDirection);
			hash.Add(DirectoryGroupByDateUnit);
			hash.Add(SortDirectoriesAlongsideFiles);
			hash.Add(SortFilesFirst);
			hash.Add(IsAdaptiveLayoutOverridden);
			hash.Add(ColumnsViewModel);

			return hash.ToHashCode();
		}
	}
}
