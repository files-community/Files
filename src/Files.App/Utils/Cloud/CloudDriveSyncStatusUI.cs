// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Utils.Cloud;
using Microsoft.UI.Xaml;

namespace Files.App.Utils.Cloud
{
	public class CloudDriveSyncStatusUI : ObservableObject
	{
		public string Glyph { get; }

		public Style OpacityIcon { get; }

		public CloudDriveSyncStatus SyncStatus { get; }

		public bool LoadSyncStatus { get; }

		public string SyncStatusString { get; } = "CloudDriveSyncStatus_Unknown".GetLocalizedResource();

		public CloudDriveSyncStatusUI()
		{
		}

		private CloudDriveSyncStatusUI(CloudDriveSyncStatus syncStatus)
		{
			SyncStatus = syncStatus;
		}

		private CloudDriveSyncStatusUI(string glyph, Style opacityIcon, CloudDriveSyncStatus syncStatus, string SyncStatusStringKey)
		{
			SyncStatus = syncStatus;
			Glyph = glyph;
			OpacityIcon = opacityIcon;
			LoadSyncStatus = true;
			SyncStatusString = SyncStatusStringKey.GetLocalizedResource();
		}

		public static CloudDriveSyncStatusUI FromCloudDriveSyncStatus(CloudDriveSyncStatus syncStatus) => syncStatus switch
		{
			// File
			CloudDriveSyncStatus.FileOnline
				=> new CloudDriveSyncStatusUI("\uE753", (Style)Application.Current.Resources["ColorIconCloud"], syncStatus, "CloudDriveSyncStatus_Online"),
			CloudDriveSyncStatus.FileOffline
				=> new CloudDriveSyncStatusUI("\uE73E", (Style)Application.Current.Resources["ColorIconCloudSynced"], syncStatus, "CloudDriveSyncStatus_Offline"),
			CloudDriveSyncStatus.FileOfflinePinned
				=> new CloudDriveSyncStatusUI("\uE73E", (Style)Application.Current.Resources["ColorIconCloudKeepOffline"], syncStatus, "CloudDriveSyncStatus_Offline"),
			CloudDriveSyncStatus.FileSync
				=> new CloudDriveSyncStatusUI("\uE895", (Style)Application.Current.Resources["ColorIconCloudSyncing"], syncStatus, "CloudDriveSyncStatus_Sync"),

			// Folder
			CloudDriveSyncStatus.FolderOnline or CloudDriveSyncStatus.FolderOfflinePartial
				=> new CloudDriveSyncStatusUI("\uE753", (Style)Application.Current.Resources["ColorIconCloud"], syncStatus, "CloudDriveSyncStatus_PartialOffline"),
			CloudDriveSyncStatus.FolderOfflineFull or CloudDriveSyncStatus.FolderEmpty
				=> new CloudDriveSyncStatusUI("\uE73E", (Style)Application.Current.Resources["ColorIconCloudSynced"], syncStatus, "CloudDriveSyncStatus_Offline"),
			CloudDriveSyncStatus.FolderOfflinePinned
				=> new CloudDriveSyncStatusUI("\uE73E", (Style)Application.Current.Resources["ColorIconCloudKeepOffline"], syncStatus, "CloudDriveSyncStatus_Offline"),
			CloudDriveSyncStatus.FolderExcluded
				=> new CloudDriveSyncStatusUI("\uF140", (Style)Application.Current.Resources["ColorIconCloudUnavailable"], syncStatus, "CloudDriveSyncStatus_Excluded"),

			// Unknown
			_ => new CloudDriveSyncStatusUI(syncStatus),
		};
	}
}
