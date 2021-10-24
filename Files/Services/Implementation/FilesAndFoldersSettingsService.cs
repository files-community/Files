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

        public override void RaiseOnSettingChangedEvent(object sender, EventArguments.SettingChangedEventArgs e)
        {
            switch (e.settingName)
            {
                case nameof(ShowFileExtensions):
                case nameof(AreHiddenItemsVisible):
                case nameof(AreSystemItemsHidden):
                case nameof(ListAndSortDirectoriesAlongsideFiles):
                case nameof(OpenFilesWithOneClick):
                case nameof(OpenFoldersWithOneClick):
                case nameof(SearchUnindexedItems):
                case nameof(AreLayoutPreferencesPerFolder):
                case nameof(AdaptiveLayoutEnabled):
                case nameof(AreFileTagsEnabled):
                    Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{nameof(e.settingName)} {e.newValue}");
                    break;
            }

            base.RaiseOnSettingChangedEvent(sender, e);
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

        public bool OpenFilesWithOneClick
        {
            get => Get(false);
            set => Set(value);
        }
        
        public bool OpenFoldersWithOneClick
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
