using Files.Models.JsonSettings;
using Microsoft.AppCenter.Analytics;
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
        
        public bool ShowDotFiles
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

        public bool ShowFolderSize
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

        public override void RaiseOnSettingChangedEvent(object sender, EventArguments.SettingChangedEventArgs e)
        {
            switch (e.settingName)
            {
                case nameof(ShowConfirmDeleteDialog):
                case nameof(OpenFoldersInNewTab):
                case nameof(ShowFileExtensions):
                case nameof(AreHiddenItemsVisible):
                case nameof(AreSystemItemsHidden):
                case nameof(ShowDotFiles):
                case nameof(ListAndSortDirectoriesAlongsideFiles):
                case nameof(OpenFilesWithOneClick):
                case nameof(OpenFoldersWithOneClick):
                case nameof(SearchUnindexedItems):
                case nameof(AreLayoutPreferencesPerFolder):
                case nameof(AdaptiveLayoutEnabled):
                case nameof(AreFileTagsEnabled):
                case nameof(ShowFolderSize):
                case nameof(OpenSpecificPageOnStartup):
                case nameof(ContinueLastSessionOnStartUp):
                case nameof(OpenNewTabOnStartup):
                case nameof(AlwaysOpenNewInstance):
                    Analytics.TrackEvent($"{e.settingName} {e.newValue}");
                    break;
            }

            base.RaiseOnSettingChangedEvent(sender, e);
        }

        public void ReportToAppCenter()
        {
            Analytics.TrackEvent($"{nameof(ShowConfirmDeleteDialog)}, {ShowConfirmDeleteDialog}");
            Analytics.TrackEvent($"{nameof(OpenFoldersInNewTab)}, {OpenFoldersInNewTab}");
            Analytics.TrackEvent($"{nameof(ShowFileExtensions)}, {ShowFileExtensions}");
            Analytics.TrackEvent($"{nameof(AreHiddenItemsVisible)}, {AreHiddenItemsVisible}");
            Analytics.TrackEvent($"{nameof(AreSystemItemsHidden)}, {AreSystemItemsHidden}");
            Analytics.TrackEvent($"{nameof(ShowDotFiles)}, {ShowDotFiles}");
            Analytics.TrackEvent($"{nameof(ListAndSortDirectoriesAlongsideFiles)}, {ListAndSortDirectoriesAlongsideFiles}");
            Analytics.TrackEvent($"{nameof(OpenFilesWithOneClick)}, {OpenFilesWithOneClick}");
            Analytics.TrackEvent($"{nameof(OpenFoldersWithOneClick)}, {OpenFoldersWithOneClick}");
            Analytics.TrackEvent($"{nameof(SearchUnindexedItems)}, {SearchUnindexedItems}");
            Analytics.TrackEvent($"{nameof(AreLayoutPreferencesPerFolder)}, {AreLayoutPreferencesPerFolder}");
            Analytics.TrackEvent($"{nameof(AdaptiveLayoutEnabled)}, {AdaptiveLayoutEnabled}");
            Analytics.TrackEvent($"{nameof(AreFileTagsEnabled)}, {AreFileTagsEnabled}");
            Analytics.TrackEvent($"{nameof(ShowFolderSize)}, {ShowFolderSize}");
            Analytics.TrackEvent($"{nameof(OpenSpecificPageOnStartup)}, {OpenSpecificPageOnStartup}");
            Analytics.TrackEvent($"{nameof(ContinueLastSessionOnStartUp)}, {ContinueLastSessionOnStartUp}");
            Analytics.TrackEvent($"{nameof(OpenNewTabOnStartup)}, {OpenNewTabOnStartup}");
            Analytics.TrackEvent($"{nameof(AlwaysOpenNewInstance)}, {AlwaysOpenNewInstance}");
        }
    }
}
