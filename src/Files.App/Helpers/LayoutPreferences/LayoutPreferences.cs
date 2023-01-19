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

			LayoutMode = defaultLayout is FolderLayoutModes.Adaptive ? FolderLayoutModes.DetailsView : defaultLayout;
			GridViewSize = userSettingsService.LayoutSettingsService.DefaultGridViewSize;
			DirectorySortOption = userSettingsService.FoldersSettingsService.DefaultSortOption;
			DirectoryGroupOption = userSettingsService.FoldersSettingsService.DefaultGroupOption;
			DirectorySortDirection = userSettingsService.LayoutSettingsService.DefaultDirectorySortDirection;
			SortDirectoriesAlongsideFiles = userSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles;
			IsAdaptiveLayoutOverridden = defaultLayout is not FolderLayoutModes.Adaptive;

			ColumnsViewModel = new ColumnsViewModel();
			ColumnsViewModel.DateCreatedColumn.UserCollapsed = !userSettingsService.FoldersSettingsService.ShowDateCreatedColumn;
			ColumnsViewModel.DateModifiedColumn.UserCollapsed = !userSettingsService.FoldersSettingsService.ShowDateColumn;
			ColumnsViewModel.ItemTypeColumn.UserCollapsed = !userSettingsService.FoldersSettingsService.ShowTypeColumn;
			ColumnsViewModel.SizeColumn.UserCollapsed = !userSettingsService.FoldersSettingsService.ShowSizeColumn;
			ColumnsViewModel.TagColumn.UserCollapsed = !userSettingsService.FoldersSettingsService.ShowFileTagColumn;
			ColumnsViewModel.DateDeletedColumn.UserCollapsed = !userSettingsService.FoldersSettingsService.ShowDateDeletedColumn;
			ColumnsViewModel.OriginalPathColumn.UserCollapsed = !userSettingsService.FoldersSettingsService.ShowOriginalPathColumn;
			ColumnsViewModel.StatusColumn.UserCollapsed = !userSettingsService.FoldersSettingsService.ShowSyncStatusColumn;

			ColumnsViewModel.NameColumn.UserLengthPixels = userSettingsService.FoldersSettingsService.NameColumnWidth;
			ColumnsViewModel.DateModifiedColumn.UserLengthPixels = userSettingsService.FoldersSettingsService.DateModifiedColumnWidth;
			ColumnsViewModel.DateCreatedColumn.UserLengthPixels = userSettingsService.FoldersSettingsService.DateCreatedColumnWidth;
			ColumnsViewModel.ItemTypeColumn.UserLengthPixels = userSettingsService.FoldersSettingsService.TypeColumnWidth;
			ColumnsViewModel.SizeColumn.UserLengthPixels = userSettingsService.FoldersSettingsService.SizeColumnWidth;
			ColumnsViewModel.TagColumn.UserLengthPixels = userSettingsService.FoldersSettingsService.TagColumnWidth;
			ColumnsViewModel.DateDeletedColumn.UserLengthPixels = userSettingsService.FoldersSettingsService.DateDeletedColumnWidth;
			ColumnsViewModel.OriginalPathColumn.UserLengthPixels = userSettingsService.FoldersSettingsService.OriginalPathColumnWidth;
			ColumnsViewModel.StatusColumn.UserLengthPixels = userSettingsService.FoldersSettingsService.SyncStatusColumnWidth;
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
			hashCode = (hashCode * 397) ^ SortDirectoriesAlongsideFiles.GetHashCode();
			hashCode = (hashCode * 397) ^ IsAdaptiveLayoutOverridden.GetHashCode();
			hashCode = (hashCode * 397) ^ ColumnsViewModel.GetHashCode();
			return hashCode;
		}
	}
}
