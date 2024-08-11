// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Storage.Storables;
using Files.Core.Storage.Storables;
using Microsoft.Extensions.Logging;
using System.IO;
using Windows.Storage;

namespace Files.App.Services
{
	public sealed class RemovableDrivesService : IRemovableDrivesService
	{
		public IStorageDeviceWatcher CreateWatcher()
		{
			return new WindowsStorageDeviceWatcher();
		}

		public async IAsyncEnumerable<ILocatableFolder> GetDrivesAsync()
		{
			var list = DriveInfo.GetDrives();
			var googleDrivePath = App.AppModel.GoogleDrivePath;
			var pCloudDrivePath = App.AppModel.PCloudDrivePath;

#if DEBUG
			Debug.WriteLine($"In RDS.GDA: googleDrivePath: {googleDrivePath}");
#endif

			foreach (var drive in list)
			{
				var shouldSkip = false;
				var sw = Stopwatch.StartNew();
				await foreach (var cloudProvider in GoogleDriveCloudDetector.GetGoogleDriveProvidersFromRegistryAsync(false))
				{
					if (cloudProvider.SyncFolder.Equals(drive.Name))
						shouldSkip = true;
				}
				sw.Stop();

#if DEBUG
				Debug.WriteLine($"In RDS.GDA in await foreach: drive.Name: {drive.Name}");
				Debug.WriteLine($"In RDS.GDA in await foreach: Time elapsed for filter: {sw.Elapsed}");
#endif

				if (shouldSkip)
					continue;

				var res = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(drive.Name).AsTask());
				if (res.ErrorCode is FileSystemStatusCode.Unauthorized)
				{
					App.Logger.LogWarning($"{res.ErrorCode}: Attempting to add the device, {drive.Name},"
						+ " failed at the StorageFolder initialization step. This device will be ignored.");
					continue;
				}
				else if (!res)
				{
					App.Logger.LogWarning($"{res.ErrorCode}: Attempting to add the device, {drive.Name},"
						+ " failed at the StorageFolder initialization step. This device will be ignored.");
					continue;
				}

				using var thumbnail = await DriveHelpers.GetThumbnailAsync(res.Result);
				var type = DriveHelpers.GetDriveType(drive);
				var label = DriveHelpers.GetExtendedDriveLabel(drive);
				var driveItem = await DriveItem.CreateFromPropertiesAsync(res.Result, drive.Name.TrimEnd('\\'), label, type, thumbnail);

#if DEBUG
				Debug.WriteLine($"In RDS.GDA: drive.Name: {drive.Name}");
#endif
				// Don't add here because Google Drive is already displayed under cloud drives
				if (drive.Name == googleDrivePath || drive.Name == pCloudDrivePath)
					continue;

				App.Logger.LogInformation($"Drive added: {driveItem.Path}, {driveItem.Type}");

				yield return driveItem;
			}
		}

		public async Task<ILocatableFolder> GetPrimaryDriveAsync()
		{
			string cDrivePath = $@"{Constants.UserEnvironmentPaths.SystemDrivePath}\";
			return new WindowsStorageFolder(await StorageFolder.GetFolderFromPathAsync(cDrivePath));
		}

		public async Task UpdateDrivePropertiesAsync(ILocatableFolder drive)
		{
			var rootModified = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(drive.Path).AsTask());
			if (rootModified && drive is DriveItem matchingDriveEjected)
			{
				_ = MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
				{
					matchingDriveEjected.Root = rootModified.Result;
					matchingDriveEjected.Text = rootModified.Result.DisplayName;
					return matchingDriveEjected.UpdatePropertiesAsync();
				});
			}
		}
	}
}
