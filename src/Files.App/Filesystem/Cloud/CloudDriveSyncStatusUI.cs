// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.Extensions;
using Files.Shared.Cloud;

namespace Files.App.Filesystem.Cloud
{
	public class CloudDriveSyncStatusUI : ObservableObject
	{
		public string Glyph { get; }

		public CloudDriveSyncStatus SyncStatus { get; }
		public bool LoadSyncStatus { get; }
		public string SyncStatusString { get; } = "CloudDriveSyncStatus_Unknown".GetLocalizedResource();

		public CloudDriveSyncStatusUI() { }
		private CloudDriveSyncStatusUI(CloudDriveSyncStatus syncStatus) => SyncStatus = syncStatus;
		private CloudDriveSyncStatusUI(string glyph, CloudDriveSyncStatus syncStatus, string SyncStatusStringKey)
		{
			SyncStatus = syncStatus;
			Glyph = glyph;
			LoadSyncStatus = true;
			SyncStatusString = SyncStatusStringKey.GetLocalizedResource();
		}

		public static CloudDriveSyncStatusUI FromCloudDriveSyncStatus(CloudDriveSyncStatus syncStatus) => syncStatus switch
		{
			// File
			CloudDriveSyncStatus.FileOnline
				=> new CloudDriveSyncStatusUI("\uE753", syncStatus, "CloudDriveSyncStatus_Online"),
			CloudDriveSyncStatus.FileOffline or CloudDriveSyncStatus.FileOfflinePinned
				=> new CloudDriveSyncStatusUI("\uE73E", syncStatus, "CloudDriveSyncStatus_Offline"),
			CloudDriveSyncStatus.FileSync
				=> new CloudDriveSyncStatusUI("\uE895", syncStatus, "CloudDriveSyncStatus_Sync"),

			// Folder
			CloudDriveSyncStatus.FolderOnline or CloudDriveSyncStatus.FolderOfflinePartial
				=> new CloudDriveSyncStatusUI("\uE753", syncStatus, "CloudDriveSyncStatus_PartialOffline"),
			CloudDriveSyncStatus.FolderOfflineFull or CloudDriveSyncStatus.FolderOfflinePinned or CloudDriveSyncStatus.FolderEmpty
				=> new CloudDriveSyncStatusUI("\uE73E", syncStatus, "CloudDriveSyncStatus_Offline"),
			CloudDriveSyncStatus.FolderExcluded
				=> new CloudDriveSyncStatusUI("\uF140", syncStatus, "CloudDriveSyncStatus_Excluded"),

			// Unknown
			_ => new CloudDriveSyncStatusUI(syncStatus),
		};
	}
}