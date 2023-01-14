using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.ViewModels;
using Files.Backend.Services.Settings;
using Files.Shared.Enums;

namespace Files.App.Helpers.LayoutPreferences
{
	public class LayoutPreferences
	{
		private readonly IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

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
			var defaultLayout = userSettingsService.FoldersSettingsService.DefaultLayoutMode;

			this.LayoutMode = defaultLayout is FolderLayoutModes.Adaptive ? FolderLayoutModes.DetailsView : defaultLayout;
			this.GridViewSize = userSettingsService.LayoutSettingsService.DefaultGridViewSize;
			this.DirectorySortOption = userSettingsService.FoldersSettingsService.DefaultSortOption;
			this.DirectoryGroupOption = userSettingsService.FoldersSettingsService.DefaultGroupOption;
			this.DirectorySortDirection = userSettingsService.LayoutSettingsService.DefaultDirectorySortDirection;
			this.SortDirectoriesAlongsideFiles = userSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles;
			this.IsAdaptiveLayoutOverridden = defaultLayout is not FolderLayoutModes.Adaptive;

			this.ColumnsViewModel = new ColumnsViewModel();
			this.ColumnsViewModel.DateCreatedColumn.UserCollapsed = !userSettingsService.FoldersSettingsService.ShowDateCreatedColumn;
			this.ColumnsViewModel.DateModifiedColumn.UserCollapsed = !userSettingsService.FoldersSettingsService.ShowDateColumn;
			this.ColumnsViewModel.ItemTypeColumn.UserCollapsed = !userSettingsService.FoldersSettingsService.ShowTypeColumn;
			this.ColumnsViewModel.SizeColumn.UserCollapsed = !userSettingsService.FoldersSettingsService.ShowSizeColumn;
			this.ColumnsViewModel.TagColumn.UserCollapsed = !userSettingsService.FoldersSettingsService.ShowFileTagColumn;
			this.ColumnsViewModel.DateDeletedColumn.UserCollapsed = !userSettingsService.FoldersSettingsService.ShowDateDeletedColumn;
			this.ColumnsViewModel.OriginalPathColumn.UserCollapsed = !userSettingsService.FoldersSettingsService.ShowOriginalPathColumn;
			this.ColumnsViewModel.StatusColumn.UserCollapsed = !userSettingsService.FoldersSettingsService.ShowSyncStatusColumn;

			this.ColumnsViewModel.NameColumn.UserLengthPixels = userSettingsService.FoldersSettingsService.NameColumnWidth;
			this.ColumnsViewModel.DateModifiedColumn.UserLengthPixels = userSettingsService.FoldersSettingsService.DateModifiedColumnWidth;
			this.ColumnsViewModel.DateCreatedColumn.UserLengthPixels = userSettingsService.FoldersSettingsService.DateCreatedColumnWidth;
			this.ColumnsViewModel.ItemTypeColumn.UserLengthPixels = userSettingsService.FoldersSettingsService.TypeColumnWidth;
			this.ColumnsViewModel.SizeColumn.UserLengthPixels = userSettingsService.FoldersSettingsService.SizeColumnWidth;
			this.ColumnsViewModel.TagColumn.UserLengthPixels = userSettingsService.FoldersSettingsService.TagColumnWidth;
			this.ColumnsViewModel.DateDeletedColumn.UserLengthPixels = userSettingsService.FoldersSettingsService.DateDeletedColumnWidth;
			this.ColumnsViewModel.OriginalPathColumn.UserLengthPixels = userSettingsService.FoldersSettingsService.OriginalPathColumnWidth;
			this.ColumnsViewModel.StatusColumn.UserLengthPixels = userSettingsService.FoldersSettingsService.SyncStatusColumnWidth;
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
