// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using OwlCore.Storage.System.IO;
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

		public async IAsyncEnumerable<IFolder> GetDrivesAsync()
		{
			var pCloudDrivePath = App.AppModel.PCloudDrivePath;

			// IsReady/VolumeLabel block until the network timeout for an unreachable mapped drive, so probe off the UI thread.
			foreach (var drive in await Task.Run(DriveInfo.GetDrives).ConfigureAwait(false))
			{
				var probe = await Task.Run<(string Label, Data.Items.DriveType Type)?>(() =>
				{
					try
					{
						return drive.IsReady
							? (DriveHelpers.GetExtendedDriveLabel(drive), DriveHelpers.GetDriveType(drive))
							: null;
					}
					catch
					{
						return null;
					}
				});

				if (probe is not { } info)
					continue;

				// Filter out cloud drives; we don't want them in the plain "Drives" sections.
				if (info.Label.Equals("Google Drive") || drive.Name.Equals(pCloudDrivePath))
					continue;

				var res = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(drive.Name).AsTask());
				if (res.ErrorCode is FileSystemStatusCode.Unauthorized || !res)
				{
					App.Logger.LogWarning($"{res.ErrorCode}: Attempting to add the device, {drive.Name},"
						+ " failed at the StorageFolder initialization step. This device will be ignored.");
					continue;
				}

				var root = res.Result!;
				using var thumbnail = await DriveHelpers.GetThumbnailAsync(root);
				var driveItem = await DriveItem.CreateFromPropertiesAsync(root, drive.Name.TrimEnd('\\'), info.Label, info.Type, thumbnail);

				App.Logger.LogInformation($"Drive added: {driveItem.Path}, {driveItem.Type}");

				yield return driveItem;
			}
		}

		public Task<IFolder?> GetPrimaryDriveAsync()
		{
			var cDrivePath = $@"{Constants.UserEnvironmentPaths.SystemDrivePath}\";
			if (!Directory.Exists(cDrivePath))
			{
				App.Logger.LogWarning($"Primary system drive '{cDrivePath}' could not be found.");
				return Task.FromResult<IFolder?>(null);
			}

			return Task.FromResult<IFolder?>(new SystemFolder(cDrivePath));
		}

		public async Task UpdateDrivePropertiesAsync(IFolder drive)
		{
			var rootModified = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(drive.Id).AsTask());
			if (rootModified && drive is DriveItem matchingDriveEjected)
			{
				var root = rootModified.Result!;
				_ = MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
				{
					matchingDriveEjected.Root = root;
					matchingDriveEjected.Text = root.DisplayName;
					return matchingDriveEjected.UpdatePropertiesAsync();
				});
			}
		}
	}
}
