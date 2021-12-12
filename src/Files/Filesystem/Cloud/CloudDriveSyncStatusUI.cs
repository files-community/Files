using Files.Enums;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp;
using Windows.UI.Xaml.Media;

namespace Files.Filesystem.Cloud
{
    public class CloudDriveSyncStatusUI : ObservableObject
    {
        private bool loadSyncStatus;

        public bool LoadSyncStatus
        {
            get => loadSyncStatus;
            set => SetProperty(ref loadSyncStatus, value);
        }

        private string glyph;

        public string Glyph
        {
            get => glyph;
            set => SetProperty(ref glyph, value);
        }

        private string syncStatusString = "CloudDriveSyncStatus_Unknown".GetLocalized();

        public string SyncStatusString
        {
            get => syncStatusString;
            set => SetProperty(ref syncStatusString, value);
        }

        public CloudDriveSyncStatus SyncStatus { get; set; }

        public static CloudDriveSyncStatusUI FromCloudDriveSyncStatus(CloudDriveSyncStatus syncStatus)
        {
            var statusUI = new CloudDriveSyncStatusUI();
            statusUI.SyncStatus = syncStatus;
            switch (syncStatus)
            {
                // File
                case CloudDriveSyncStatus.FileOnline:
                    statusUI.LoadSyncStatus = true;
                    statusUI.Glyph = "\uE753";
                    statusUI.SyncStatusString = "CloudDriveSyncStatus_Online".GetLocalized();
                    break;

                case CloudDriveSyncStatus.FileOffline:
                case CloudDriveSyncStatus.FileOfflinePinned:
                    statusUI.LoadSyncStatus = true;
                    statusUI.Glyph = "\uE73E";
                    statusUI.SyncStatusString = "CloudDriveSyncStatus_Offline".GetLocalized();
                    break;

                case CloudDriveSyncStatus.FileSync:
                    statusUI.LoadSyncStatus = true;
                    statusUI.Glyph = "\uE895";
                    statusUI.SyncStatusString = "CloudDriveSyncStatus_Sync".GetLocalized();
                    break;

                // Folder
                case CloudDriveSyncStatus.FolderOnline:
                case CloudDriveSyncStatus.FolderOfflinePartial:
                    statusUI.LoadSyncStatus = true;
                    statusUI.Glyph = "\uE753";
                    statusUI.SyncStatusString = "CloudDriveSyncStatus_PartialOffline".GetLocalized();
                    break;

                case CloudDriveSyncStatus.FolderOfflineFull:
                case CloudDriveSyncStatus.FolderOfflinePinned:
                case CloudDriveSyncStatus.FolderEmpty:
                    statusUI.LoadSyncStatus = true;
                    statusUI.Glyph = "\uE73E";
                    statusUI.SyncStatusString = "CloudDriveSyncStatus_Offline".GetLocalized();
                    break;

                case CloudDriveSyncStatus.FolderExcluded:
                    statusUI.LoadSyncStatus = true;
                    statusUI.Glyph = "\uF140";
                    statusUI.SyncStatusString = "CloudDriveSyncStatus_Excluded".GetLocalized();
                    break;

                // Unknown
                case CloudDriveSyncStatus.NotSynced:
                case CloudDriveSyncStatus.Unknown:
                default:
                    statusUI.LoadSyncStatus = false;
                    break;
            }

            return statusUI;
        }
    }
}