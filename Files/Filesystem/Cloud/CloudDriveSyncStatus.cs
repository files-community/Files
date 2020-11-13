using Microsoft.Toolkit.Mvvm.ComponentModel;
using Windows.UI.Xaml.Media;

namespace Files.Filesystem.Cloud
{
    public enum CloudDriveSyncStatus
    {
        Unknown = -1,
        FolderOnline = 0,
        FolderOfflinePartial = 1,
        FolderOfflineFull = 2,
        FolderOfflinePinned = 3,
        FolderExcluded = 4,
        FolderEmpty = 5,
        NotSynced = 6,
        FileOnline = 8,
        FileSync = 9,
        FileOffline = 14,
        FileOfflinePinned = 15,
    }

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

        private SolidColorBrush foreground;

        public SolidColorBrush Foreground
        {
            get => foreground;
            set => SetProperty(ref foreground, value);
        }

        public static CloudDriveSyncStatusUI FromCloudDriveSyncStatus(CloudDriveSyncStatus syncStatus)
        {
            var statusUI = new CloudDriveSyncStatusUI();

            switch (syncStatus)
            {
                // File
                case CloudDriveSyncStatus.FileOnline:
                    statusUI.LoadSyncStatus = true;
                    statusUI.Glyph = "\uE753";
                    statusUI.Foreground = (SolidColorBrush)App.Current.Resources["CloudDriveSyncStatusOnlineColor"];
                    break;

                case CloudDriveSyncStatus.FileOffline:
                case CloudDriveSyncStatus.FileOfflinePinned:
                    statusUI.LoadSyncStatus = true;
                    statusUI.Glyph = "\uE73E";
                    statusUI.Foreground = (SolidColorBrush)App.Current.Resources["CloudDriveSyncStatusOfflineColor"];
                    break;

                case CloudDriveSyncStatus.FileSync:
                    statusUI.LoadSyncStatus = true;
                    statusUI.Glyph = "\uE895";
                    statusUI.Foreground = (SolidColorBrush)App.Current.Resources["CloudDriveSyncStatusOnlineColor"];
                    break;

                // Folder
                case CloudDriveSyncStatus.FolderOnline:
                case CloudDriveSyncStatus.FolderOfflinePartial:
                    statusUI.LoadSyncStatus = true;
                    statusUI.Glyph = "\uE753";
                    statusUI.Foreground = (SolidColorBrush)App.Current.Resources["CloudDriveSyncStatusOnlineColor"];
                    break;

                case CloudDriveSyncStatus.FolderOfflineFull:
                case CloudDriveSyncStatus.FolderOfflinePinned:
                case CloudDriveSyncStatus.FolderEmpty:
                    statusUI.LoadSyncStatus = true;
                    statusUI.Glyph = "\uE73E";
                    statusUI.Foreground = (SolidColorBrush)App.Current.Resources["CloudDriveSyncStatusOfflineColor"];
                    break;

                case CloudDriveSyncStatus.FolderExcluded:
                    statusUI.LoadSyncStatus = true;
                    statusUI.Glyph = "\uF140";
                    statusUI.Foreground = (SolidColorBrush)App.Current.Resources["CloudDriveSyncStatusExcludedColor"];
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