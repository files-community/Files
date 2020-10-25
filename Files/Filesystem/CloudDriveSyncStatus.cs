using Microsoft.Toolkit.Mvvm.ComponentModel;
using Windows.UI.Xaml.Media;

namespace Files.Filesystem
{
    public enum CloudDriveSyncStatus
    {
        Unknown = -1,
        Folder_Online = 0,
        Folder_Offline_Partial = 1,
        Folder_Offline_Full = 2,
        Folder_Offline_Pinned = 3,
        Folder_Excluded = 4,
        Folder_Empty = 5,
        NotSynced = 6,
        File_Online = 8,
        File_Sync = 9,
        File_Offline = 14,
        File_Offline_Pinned = 15,
    }

    public class CloudDriveSyncStatusUI : ObservableObject
    {
        private bool _LoadSyncStatus;

        public bool LoadSyncStatus
        {
            get => _LoadSyncStatus;
            set => SetProperty(ref _LoadSyncStatus, value);
        }

        private string _Glyph;

        public string Glyph
        {
            get => _Glyph;
            set => SetProperty(ref _Glyph, value);
        }

        private SolidColorBrush _Foreground;

        public SolidColorBrush Foreground
        {
            get => _Foreground;
            set => SetProperty(ref _Foreground, value);
        }

        public static CloudDriveSyncStatusUI FromCloudDriveSyncStatus(CloudDriveSyncStatus syncStatus)
        {
            var statusUI = new CloudDriveSyncStatusUI();

            switch (syncStatus)
            {
                // File
                case CloudDriveSyncStatus.File_Online:
                    statusUI.LoadSyncStatus = true;
                    statusUI.Glyph = "\uE753";
                    statusUI.Foreground = (SolidColorBrush)App.Current.Resources["CloudDriveSyncStatusOnlineColor"];
                    break;

                case CloudDriveSyncStatus.File_Offline:
                case CloudDriveSyncStatus.File_Offline_Pinned:
                    statusUI.LoadSyncStatus = true;
                    statusUI.Glyph = "\uE73E";
                    statusUI.Foreground = (SolidColorBrush)App.Current.Resources["CloudDriveSyncStatusOfflineColor"];
                    break;

                case CloudDriveSyncStatus.File_Sync:
                    statusUI.LoadSyncStatus = true;
                    statusUI.Glyph = "\uE895";
                    statusUI.Foreground = (SolidColorBrush)App.Current.Resources["CloudDriveSyncStatusOnlineColor"];
                    break;

                // Folder
                case CloudDriveSyncStatus.Folder_Online:
                case CloudDriveSyncStatus.Folder_Offline_Partial:
                    statusUI.LoadSyncStatus = true;
                    statusUI.Glyph = "\uE753";
                    statusUI.Foreground = (SolidColorBrush)App.Current.Resources["CloudDriveSyncStatusOnlineColor"];
                    break;

                case CloudDriveSyncStatus.Folder_Offline_Full:
                case CloudDriveSyncStatus.Folder_Offline_Pinned:
                case CloudDriveSyncStatus.Folder_Empty:
                    statusUI.LoadSyncStatus = true;
                    statusUI.Glyph = "\uE73E";
                    statusUI.Foreground = (SolidColorBrush)App.Current.Resources["CloudDriveSyncStatusOfflineColor"];
                    break;

                case CloudDriveSyncStatus.Folder_Excluded:
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