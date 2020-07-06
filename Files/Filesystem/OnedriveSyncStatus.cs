using GalaSoft.MvvmLight;
using Windows.UI.Xaml.Media;

namespace Files.Filesystem
{
    public enum OnedriveSyncStatus
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

    public class OnedriveSyncStatusUI : ObservableObject
    {
        private bool _LoadSyncStatus;
        public bool LoadSyncStatus { get => _LoadSyncStatus; set => Set(ref _LoadSyncStatus, value); }
        private string _Glyph;
        public string Glyph { get => _Glyph; set => Set(ref _Glyph, value); }
        private SolidColorBrush _Foreground;
        public SolidColorBrush Foreground { get => _Foreground; set => Set(ref _Foreground, value); }

        public static OnedriveSyncStatusUI FromOnedriveSyncStatus(OnedriveSyncStatus syncStatus)
        {
            var statusUI = new OnedriveSyncStatusUI();

            switch (syncStatus)
            {
                // File
                case OnedriveSyncStatus.File_Online:
                    statusUI.LoadSyncStatus = true;
                    statusUI.Glyph = "\uE753";
                    statusUI.Foreground = (SolidColorBrush)App.Current.Resources["OnedriveSyncStatusOnlineColor"];
                    break;
                case OnedriveSyncStatus.File_Offline:
                case OnedriveSyncStatus.File_Offline_Pinned:
                    statusUI.LoadSyncStatus = true;
                    statusUI.Glyph = "\uE73E";
                    statusUI.Foreground = (SolidColorBrush)App.Current.Resources["OnedriveSyncStatusOfflineColor"];
                    break;
                case OnedriveSyncStatus.File_Sync:
                    statusUI.LoadSyncStatus = true;
                    statusUI.Glyph = "\uE895";
                    statusUI.Foreground = (SolidColorBrush)App.Current.Resources["OnedriveSyncStatusOnlineColor"];
                    break;

                // Folder
                case OnedriveSyncStatus.Folder_Online:
                case OnedriveSyncStatus.Folder_Offline_Partial:
                    statusUI.LoadSyncStatus = true;
                    statusUI.Glyph = "\uE753";
                    statusUI.Foreground = (SolidColorBrush)App.Current.Resources["OnedriveSyncStatusOnlineColor"];
                    break;
                case OnedriveSyncStatus.Folder_Offline_Full:
                case OnedriveSyncStatus.Folder_Offline_Pinned:
                case OnedriveSyncStatus.Folder_Empty:
                    statusUI.LoadSyncStatus = true;
                    statusUI.Glyph = "\uE73E";
                    statusUI.Foreground = (SolidColorBrush)App.Current.Resources["OnedriveSyncStatusOfflineColor"];
                    break;
                case OnedriveSyncStatus.Folder_Excluded:
                    statusUI.LoadSyncStatus = true;
                    statusUI.Glyph = "\uE711";
                    statusUI.Foreground = (SolidColorBrush)App.Current.Resources["OnedriveSyncStatusExcludedColor"];
                    break;

                // Unknown
                case OnedriveSyncStatus.NotSynced:
                case OnedriveSyncStatus.Unknown:
                default:
                    statusUI.LoadSyncStatus = false;
                    break;
            }

            return statusUI;
        }
    }
}
