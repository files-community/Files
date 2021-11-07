using Files.Models.JsonSettings;
using System.Collections.Generic;

namespace Files.Services.Implementation
{
    public class PreferencesSettingsService : BaseObservableJsonSettingsModel, IPreferencesSettingsService
    {
        public PreferencesSettingsService(ISettingsSharingContext settingsSharingContext)
        {
            // Initialize settings
            this.RegisterSettingsContext(settingsSharingContext);
        }

        public override void RaiseOnSettingChangedEvent(object sender, EventArguments.SettingChangedEventArgs e)
        {
            switch (e.settingName)
            {
                case nameof(ShowConfirmDeleteDialog):
                case nameof(OpenFoldersInNewTab):
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
                case nameof(OpenSpecificPageOnStartup):
                case nameof(ContinueLastSessionOnStartUp):
                case nameof(OpenNewTabOnStartup):
                case nameof(AlwaysOpenNewInstance):
                    Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{nameof(e.settingName)} {e.newValue}");
                    break;
            }

            base.RaiseOnSettingChangedEvent(sender, e);
        }

        public bool ShowConfirmDeleteDialog
        {
            get => Get(true);
            set => Set(value);
        }

        public bool OpenFoldersInNewTab
        {
            get => Get(false);
            set => Set(value);
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

        public bool OpenSpecificPageOnStartup
        {
            get => Get(false);
            set => Set(value);
        }

        public string OpenSpecificPageOnStartupPath
        {
            get => Get(string.Empty);
            set => Set(value);
        }

        public bool ContinueLastSessionOnStartUp
        {
            get => Get(false);
            set => Set(value);
        }

        public bool OpenNewTabOnStartup
        {
            get => Get(true);
            set => Set(value);
        }

        public bool AlwaysOpenNewInstance
        {
            get => Get(false);
            set => Set(value);
        }

        public List<string> TabsOnStartupList
        {
            get => Get<List<string>>(null);
            set => Set(value);
        }

        public List<string> LastSessionTabList
        {
            get => Get<List<string>>(null);
            set => Set(value);
        }
    }
}
