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
		public FolderLayoutModes LayoutMode;
		public int GridViewSize;
		public bool IsAdaptiveLayoutOverridden;

		public ColumnsViewModel ColumnsViewModel;

		[LiteDB.BsonIgnore]
		public static LayoutPreferences DefaultLayoutPreferences => new LayoutPreferences();

		public LayoutPreferences()
		{
			var defaultLayout = UserSettingsService.FoldersSettingsService.DefaultLayoutMode;

			this.LayoutMode = defaultLayout is FolderLayoutModes.Adaptive ? FolderLayoutModes.DetailsView : defaultLayout;
			this.GridViewSize = UserSettingsService.LayoutSettingsService.DefaultGridViewSize;
			this.DirectorySortOption = UserSettingsService.FoldersSettingsService.DefaultSortOption;
			this.DirectoryGroupOption = UserSettingsService.FoldersSettingsService.DefaultGroupOption;
			this.DirectorySortDirection = UserSettingsService.LayoutSettingsService.DefaultDirectorySortDirection;
			this.SortDirectoriesAlongsideFiles = UserSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles;
			this.IsAdaptiveLayoutOverridden = defaultLayout is not FolderLayoutModes.Adaptive;

			this.ColumnsViewModel = new ColumnsViewModel();
			this.ColumnsViewModel.DateCreatedColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowDateCreatedColumn;
			this.ColumnsViewModel.DateModifiedColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowDateColumn;
			this.ColumnsViewModel.ItemTypeColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowTypeColumn;
			this.ColumnsViewModel.SizeColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowSizeColumn;
			this.ColumnsViewModel.TagColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowFileTagColumn;
			this.ColumnsViewModel.DateDeletedColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowDateDeletedColumn;
			this.ColumnsViewModel.OriginalPathColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowOriginalPathColumn;
			this.ColumnsViewModel.StatusColumn.UserCollapsed = !UserSettingsService.FoldersSettingsService.ShowSyncStatusColumn;

			this.ColumnsViewModel.NameColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.NameColumnWidth;
			this.ColumnsViewModel.DateModifiedColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.DateModifiedColumnWidth;
			this.ColumnsViewModel.DateCreatedColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.DateCreatedColumnWidth;
			this.ColumnsViewModel.ItemTypeColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.TypeColumnWidth;
			this.ColumnsViewModel.SizeColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.SizeColumnWidth;
			this.ColumnsViewModel.TagColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.TagColumnWidth;
			this.ColumnsViewModel.DateDeletedColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.DateDeletedColumnWidth;
			this.ColumnsViewModel.OriginalPathColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.OriginalPathColumnWidth;
			this.ColumnsViewModel.StatusColumn.UserLengthPixels = UserSettingsService.FoldersSettingsService.SyncStatusColumnWidth;
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
					prefs.LayoutMode == this.LayoutMode &&
					prefs.GridViewSize == this.GridViewSize &&
					prefs.DirectoryGroupOption == this.DirectoryGroupOption &&
					prefs.DirectorySortOption == this.DirectorySortOption &&
					prefs.DirectorySortDirection == this.DirectorySortDirection &&
					prefs.SortDirectoriesAlongsideFiles == this.SortDirectoriesAlongsideFiles &&
					prefs.IsAdaptiveLayoutOverridden == this.IsAdaptiveLayoutOverridden &&
					prefs.ColumnsViewModel.Equals(this.ColumnsViewModel));
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
			hashCode = (hashCode * 397) ^ SortDirectoriesAlongsideFiles.GetHashCode();
			hashCode = (hashCode * 397) ^ IsAdaptiveLayoutOverridden.GetHashCode();
			hashCode = (hashCode * 397) ^ ColumnsViewModel.GetHashCode();
			return hashCode;
		}
	}
}
