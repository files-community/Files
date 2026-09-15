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
			var drives = await Task.Run(DriveInfo.GetDrives).ConfigureAwait(false);

			// Probe drives in parallel so one slow drive doesn't delay the rest
			var pending = drives.Select(drive => GetDriveItemAsync(drive, pCloudDrivePath)).ToList();

			await foreach (var completed in Task.WhenEach(pending))
			{
				if (await completed is { } driveItem)
					yield return driveItem;
			}
		}

		private static async Task<IFolder?> GetDriveItemAsync(DriveInfo drive, string pCloudDrivePath)
		{
			try
			{
				// IsReady and the label read can block for a long time, so give each probe its own thread
				var probe = await Task.Factory.StartNew<(string Label, Data.Items.DriveType Type)?>(
					() => drive.IsReady
						? (DriveHelpers.GetExtendedDriveLabel(drive), DriveHelpers.GetDriveType(drive))
						: null,
					CancellationToken.None,
					TaskCreationOptions.LongRunning,
					TaskScheduler.Default);

				if (probe is not { } info)
					return null;

				// Filter out cloud drives; we don't want them in the plain "Drives" sections.
				if (info.Label.Equals("Google Drive") || drive.Name.Equals(pCloudDrivePath))
					return null;

				var res = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(drive.Name).AsTask());
				if (res.ErrorCode is FileSystemStatusCode.Unauthorized || !res)
				{
					App.Logger.LogWarning($"{res.ErrorCode}: Attempting to add the device, {drive.Name},"
						+ " failed at the StorageFolder initialization step. This device will be ignored.");
					return null;
				}

				var root = res.Result!;
				using var thumbnail = await DriveHelpers.GetThumbnailAsync(root);
				var driveItem = await DriveItem.CreateFromPropertiesAsync(root, drive.Name.TrimEnd('\\'), info.Label, info.Type, thumbnail);

				App.Logger.LogInformation($"Drive added: {driveItem.Path}, {driveItem.Type}");

				return driveItem;
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, $"Failed to load the drive {drive.Name}");
				return null;
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
