// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.ViewModels;
using Files.Backend.Services.Settings;
using Files.Shared.Enums;

namespace Files.App.Helpers.LayoutPreferences
{
	public class LayoutPreferences
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public SortOption DirectorySortOption;
		public SortDirection DirectorySortDirection;
		public bool SortDirectoriesAlongsideFiles;
		public GroupOption DirectoryGroupOption;
		public SortDirection DirectoryGroupDirection;
		public FolderLayoutModes LayoutMode;
		public int GridViewSize;
		public bool IsAdaptiveLayoutOverridden;

		public ColumnsViewModel ColumnsViewModel;

		[LiteDB.BsonIgnore]
		public static LayoutPreferences DefaultLayoutPreferences => new LayoutPreferences();

		public LayoutPreferences()
		{
			var defaultLayout = UserSettingsService.FoldersSettingsService.DefaultLayoutMode;

			LayoutMode = defaultLayout is FolderLayoutModes.Adaptive ? FolderLayoutModes.DetailsView : defaultLayout;
			GridViewSize = UserSettingsService.LayoutSettingsService.DefaultGridViewSize;
			DirectorySortOption = UserSettingsService.FoldersSettingsService.DefaultSortOption;
			DirectoryGroupOption = UserSettingsService.FoldersSettingsService.DefaultGroupOption;
			DirectorySortDirection = UserSettingsService.FoldersSettingsService.DefaultDirectorySortDirection;
			DirectoryGroupDirection = UserSettingsService.FoldersSettingsService.DefaultDirectoryGroupDirection;
			SortDirectoriesAlongsideFiles = UserSettingsService.FoldersSettingsService.DefaultSortDirectoriesAlongsideFiles;
			IsAdaptiveLayoutOverridden = defaultLayout is not FolderLayoutModes.Adaptive;

			ColumnsViewModel = new ColumnsViewModel();
			ColumnsViewModel.DateCreatedColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowDateCreatedColumn;
			ColumnsViewModel.DateModifiedColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowDateColumn;
			ColumnsViewModel.ItemTypeColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowTypeColumn;
			ColumnsViewModel.SizeColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowSizeColumn;
			ColumnsViewModel.TagColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowFileTagColumn;
			ColumnsViewModel.DateDeletedColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowDateDeletedColumn;
			ColumnsViewModel.OriginalPathColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowOriginalPathColumn;
			ColumnsViewModel.StatusColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowSyncStatusColumn;

			ColumnsViewModel.NameColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.NameColumnWidth;
			ColumnsViewModel.DateModifiedColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.DateModifiedColumnWidth;
			ColumnsViewModel.DateCreatedColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.DateCreatedColumnWidth;
			ColumnsViewModel.ItemTypeColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.TypeColumnWidth;
			ColumnsViewModel.SizeColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.SizeColumnWidth;
			ColumnsViewModel.TagColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.TagColumnWidth;
			ColumnsViewModel.DateDeletedColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.DateDeletedColumnWidth;
			ColumnsViewModel.OriginalPathColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.OriginalPathColumnWidth;
			ColumnsViewModel.StatusColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.SyncStatusColumnWidth;
		}

		public override bool Equals(object? obj)
		{
			if (obj is null)
				return false;

			if (obj == this)
				return true;

			if (obj is LayoutPreferences prefs)
			{
				return (
					prefs.LayoutMode == LayoutMode &&
					prefs.GridViewSize == GridViewSize &&
					prefs.DirectoryGroupOption == DirectoryGroupOption &&
					prefs.DirectorySortOption == DirectorySortOption &&
					prefs.DirectorySortDirection == DirectorySortDirection &&
					prefs.DirectoryGroupDirection == DirectoryGroupDirection &&
					prefs.SortDirectoriesAlongsideFiles == SortDirectoriesAlongsideFiles &&
					prefs.IsAdaptiveLayoutOverridden == IsAdaptiveLayoutOverridden &&
					prefs.ColumnsViewModel.Equals(ColumnsViewModel));
			}
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			var hashCode = LayoutMode.GetHashCode();
			hashCode = (hashCode * 397) ^ GridViewSize.GetHashCode();
			hashCode = (hashCode * 397) ^ DirectoryGroupOption.GetHashCode();
			hashCode = (hashCode * 397) ^ DirectorySortOption.GetHashCode();
			hashCode = (hashCode * 397) ^ DirectorySortDirection.GetHashCode();
			hashCode = (hashCode * 397) ^ DirectoryGroupDirection.GetHashCode();
			hashCode = (hashCode * 397) ^ SortDirectoriesAlongsideFiles.GetHashCode();
			hashCode = (hashCode * 397) ^ IsAdaptiveLayoutOverridden.GetHashCode();
			hashCode = (hashCode * 397) ^ ColumnsViewModel.GetHashCode();
			return hashCode;
		}
	}
}
