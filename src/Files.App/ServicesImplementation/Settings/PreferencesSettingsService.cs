using Files.Backend.Services.Settings;
using Files.Shared.EventArguments;
using Files.App.Serialization;
using Microsoft.AppCenter.Analytics;
using System.Collections.Generic;

namespace Files.App.ServicesImplementation.Settings
{
    internal sealed class PreferencesSettingsService : BaseObservableJsonSettings, IPreferencesSettingsService
    {
        public PreferencesSettingsService(ISettingsSharingContext settingsSharingContext)
        {
            // Register root
            RegisterSettingsContext(settingsSharingContext);
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

        public bool ShowThumbnails
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

        public bool AreAlternateStreamsVisible
        {
            get => Get(false);
            set => Set(value);
        }

        public bool ShowDotFiles
        {
            get => Get(true);
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

        public bool ColumnLayoutOpenFoldersWithOneClick
        {
            get => Get(true);
            set => Set(value);
        }

        public bool SearchUnindexedItems
        {
            get => Get(false);
            set => Set(value);
        }

        public bool ForceLayoutPreferencesOnAllDirectories
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

        protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
        {
            base.RaiseOnSettingChangedEvent(sender, e);
        }

        public void ReportToAppCenter()
        {
            Analytics.TrackEvent($"{nameof(ShowConfirmDeleteDialog)} {ShowConfirmDeleteDialog}");
            Analytics.TrackEvent($"{nameof(OpenFoldersInNewTab)} {OpenFoldersInNewTab}");
            Analytics.TrackEvent($"{nameof(ShowFileExtensions)} {ShowFileExtensions}");
            Analytics.TrackEvent($"{nameof(AreHiddenItemsVisible)} {AreHiddenItemsVisible}");
            Analytics.TrackEvent($"{nameof(AreSystemItemsHidden)} {AreSystemItemsHidden}");
            Analytics.TrackEvent($"{nameof(AreAlternateStreamsVisible)} {AreAlternateStreamsVisible}");
            Analytics.TrackEvent($"{nameof(ShowDotFiles)} {ShowDotFiles}");
            Analytics.TrackEvent($"{nameof(OpenFilesWithOneClick)} {OpenFilesWithOneClick}");
            Analytics.TrackEvent($"{nameof(OpenFoldersWithOneClick)} {OpenFoldersWithOneClick}");
            Analytics.TrackEvent($"{nameof(ColumnLayoutOpenFoldersWithOneClick)} {ColumnLayoutOpenFoldersWithOneClick}");
            Analytics.TrackEvent($"{nameof(SearchUnindexedItems)} {SearchUnindexedItems}");
            Analytics.TrackEvent($"{nameof(ForceLayoutPreferencesOnAllDirectories)} {ForceLayoutPreferencesOnAllDirectories}");
            Analytics.TrackEvent($"{nameof(ShowFolderSize)} {ShowFolderSize}");
            Analytics.TrackEvent($"{nameof(OpenSpecificPageOnStartup)} {OpenSpecificPageOnStartup}");
            Analytics.TrackEvent($"{nameof(ContinueLastSessionOnStartUp)} {ContinueLastSessionOnStartUp}");
            Analytics.TrackEvent($"{nameof(OpenNewTabOnStartup)} {OpenNewTabOnStartup}");
            Analytics.TrackEvent($"{nameof(AlwaysOpenNewInstance)} {AlwaysOpenNewInstance}");
        }
    }
}
