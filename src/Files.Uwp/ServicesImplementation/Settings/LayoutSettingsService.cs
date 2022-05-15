using Files.Backend.Services.Settings;
using Files.Shared.Enums;
using Files.Uwp.Serialization;

namespace Files.Uwp.ServicesImplementation.Settings
{
    internal sealed class LayoutSettingsService : BaseObservableJsonSettings, ILayoutSettingsService
    {
        public LayoutSettingsService(ISettingsSharingContext settingsSharingContext)
        {
            // Register root
            RegisterSettingsContext(settingsSharingContext);
        }

        public bool ShowDateColumn
        {
            get => Get(true);
            set => Set(value);
        }

        public bool ShowDateCreatedColumn
        {
            get => Get(false);
            set => Set(value);
        }

        public bool ShowTypeColumn
        {
            get => Get(true);
            set => Set(value);
        }

        public bool ShowSizeColumn
        {
            get => Get(true);
            set => Set(value);
        }

        public bool ShowFileTagColumn
        {
            get => Get(true);
            set => Set(value);
        }

        public int DefaultGridViewSize
        {
            get => (int)Get((long)Constants.Browser.GridViewBrowser.GridViewSizeSmall);
            set => Set((long)value);
        }

        public FolderLayoutModes DefaultLayoutMode
        {
            get => (FolderLayoutModes)Get((long)FolderLayoutModes.DetailsView);
            set => Set((long)value);
        }

        public SortDirection DefaultDirectorySortDirection
        {
            get => (SortDirection)Get((long)SortDirection.Ascending);
            set => Set((long)value);
        }

        public SortOption DefaultDirectorySortOption
        {
            get => (SortOption)Get((long)SortOption.Name);
            set => Set((long)value);
        }
        
        public bool DefaultSortDirectoriesAlongsideFiles
        {
            get => Get(false);
            set => Set(value);
        }

        public GroupOption DefaultDirectoryGroupOption
        {
            get => (GroupOption)Get((long)GroupOption.None);
            set => Set((long)value);
        }

        public void ReportToAppCenter()
        {
        }
    }
}
