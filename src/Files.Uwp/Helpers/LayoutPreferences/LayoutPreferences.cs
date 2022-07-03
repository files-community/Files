using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services.Settings;
using Files.Shared.Enums;
using Files.Uwp.ViewModels;

namespace Files.Uwp.Helpers.LayoutPreferences
{
    public class LayoutPreferences
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

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
            this.LayoutMode = UserSettingsService.LayoutSettingsService.DefaultLayoutMode;
            this.GridViewSize = UserSettingsService.LayoutSettingsService.DefaultGridViewSize;
            this.DirectorySortOption = UserSettingsService.LayoutSettingsService.DefaultDirectorySortOption;
            this.DirectoryGroupOption = UserSettingsService.LayoutSettingsService.DefaultDirectoryGroupOption;
            this.DirectorySortDirection = UserSettingsService.LayoutSettingsService.DefaultDirectorySortDirection;
            this.SortDirectoriesAlongsideFiles = UserSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles;
            this.IsAdaptiveLayoutOverridden = false;

            this.ColumnsViewModel = new ColumnsViewModel();
            this.ColumnsViewModel.DateCreatedColumn.UserCollapsed = !UserSettingsService.LayoutSettingsService.ShowDateCreatedColumn;
            this.ColumnsViewModel.DateModifiedColumn.UserCollapsed = !UserSettingsService.LayoutSettingsService.ShowDateColumn;
            this.ColumnsViewModel.ItemTypeColumn.UserCollapsed = !UserSettingsService.LayoutSettingsService.ShowTypeColumn;
            this.ColumnsViewModel.SizeColumn.UserCollapsed = !UserSettingsService.LayoutSettingsService.ShowSizeColumn;
            this.ColumnsViewModel.TagColumn.UserCollapsed = !UserSettingsService.LayoutSettingsService.ShowFileTagColumn;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj == this)
            {
                return true;
            }
            if (obj is LayoutPreferences prefs)
            {
                return (
                    prefs.LayoutMode == this.LayoutMode &&
                    prefs.GridViewSize == this.GridViewSize &&
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
            hashCode = (hashCode * 397) ^ DirectorySortOption.GetHashCode();
            hashCode = (hashCode * 397) ^ DirectorySortDirection.GetHashCode();
            hashCode = (hashCode * 397) ^ SortDirectoriesAlongsideFiles.GetHashCode();
            hashCode = (hashCode * 397) ^ IsAdaptiveLayoutOverridden.GetHashCode();
            hashCode = (hashCode * 397) ^ ColumnsViewModel.GetHashCode();
            return hashCode;
        }
    }
}
