// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Cloud;

namespace Files.App.Utils.Cloud
{
	/// <summary>
	/// Represents an item for cloud drive synchronization status.
	/// </summary>
	public class CloudDriveSyncStatusItem : ObservableObject
	{
		/// <summary>
		/// Gets the glyph represents the sync status.
		/// </summary>
		public string? Glyph { get; }

		/// <summary>
		/// Gets the sync status.
		/// </summary>
		public CloudDriveSyncStatus SyncStatus { get; }

		/// <summary>
		/// Gets a value whether loading sync status or not.
		/// </summary>
		public bool LoadSyncStatus { get; }

		/// <summary>
		/// Gets the humanized text represents the sync status.
		/// </summary>
		public string SyncStatusString { get; }

		public CloudDriveSyncStatusItem()
		{
			SyncStatusString = "CloudDriveSyncStatus_Unknown".GetLocalizedResource();
		}

		private CloudDriveSyncStatusItem(CloudDriveSyncStatus syncStatus): this()
		{
			SyncStatus = syncStatus;
		}

		private CloudDriveSyncStatusItem(string glyph, CloudDriveSyncStatus syncStatus, string SyncStatusStringKey)
		{
			SyncStatus = syncStatus;
			Glyph = glyph;
			LoadSyncStatus = true;
			SyncStatusString = SyncStatusStringKey.GetLocalizedResource();
		}

		public static CloudDriveSyncStatusItem FromCloudDriveSyncStatus(CloudDriveSyncStatus syncStatus)
		{
			return syncStatus switch
			{
				// File
				CloudDriveSyncStatus.FileOnline
					=> new CloudDriveSyncStatusItem("\uE753", syncStatus, "CloudDriveSyncStatus_Online"),
				CloudDriveSyncStatus.FileOffline or CloudDriveSyncStatus.FileOfflinePinned
					=> new CloudDriveSyncStatusItem("\uE73E", syncStatus, "CloudDriveSyncStatus_Offline"),
				CloudDriveSyncStatus.FileSync
					=> new CloudDriveSyncStatusItem("\uE895", syncStatus, "CloudDriveSyncStatus_Sync"),

				// Folder
				CloudDriveSyncStatus.FolderOnline or CloudDriveSyncStatus.FolderOfflinePartial
					=> new CloudDriveSyncStatusItem("\uE753", syncStatus, "CloudDriveSyncStatus_PartialOffline"),
				CloudDriveSyncStatus.FolderOfflineFull or CloudDriveSyncStatus.FolderOfflinePinned or CloudDriveSyncStatus.FolderEmpty
					=> new CloudDriveSyncStatusItem("\uE73E", syncStatus, "CloudDriveSyncStatus_Offline"),
				CloudDriveSyncStatus.FolderExcluded
					=> new CloudDriveSyncStatusItem("\uF140", syncStatus, "CloudDriveSyncStatus_Excluded"),

				// Unknown
				_ => new CloudDriveSyncStatusItem(syncStatus),
			};
		}
	}
}
