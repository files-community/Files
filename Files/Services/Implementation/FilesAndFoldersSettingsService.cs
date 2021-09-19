using Files.Models.JsonSettings;

namespace Files.Services.Implementation
{
    public class FilesAndFoldersSettingsService : BaseJsonSettingsModel, IFilesAndFoldersSettingsService
    {
        public FilesAndFoldersSettingsService(ISettingsSharingContext settingsSharingContext)
        {
            // Initialize settings
            this.RegisterSettingsContext(settingsSharingContext);
        }

        public bool ShowFileExtensions
        {
            get => Get(true);
            set => Set(value);
        }

        public bool AreHiddenItemsVisible
        {
            get => Get(false);
            set => Set(value);
        }

        public bool AreSystemItemsHidden
        {
            get => Get(true);
            set => Set(value);
        }

        public bool ListAndSortDirectoriesAlongsideFiles
        {
            get => Get(false);
            set => Set(value);
        }

        public bool OpenItemsWithOneclick
        {
            get => Get(false);
            set => Set(value);
        }

        public bool SearchUnindexedItems
        {
            get => Get(false);
            set => Set(value);
        }

        public bool AreLayoutPreferencesPerFolder
        {
            get => Get(true);
            set => Set(value);
        }

        public bool AdaptiveLayoutEnabled
        {
            get => Get(true);
            set => Set(value);
        }

        public bool AreFileTagsEnabled
        {
            get => Get(false);
            set => Set(value);
        }
    }
}
