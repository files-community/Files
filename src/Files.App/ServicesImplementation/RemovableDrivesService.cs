using CommunityToolkit.WinUI;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Storage.WindowsStorage;
using Files.Backend.Models;
using Files.Backend.Services;
using Files.Sdk.Storage.LocatableStorage;
using Files.Shared.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using static Files.App.Constants.Widgets;

namespace Files.App.ServicesImplementation
{
	public class RemovableDrivesService : IRemovableDrivesService
	{
		public IStorageDeviceWatcher CreateWatcher()
		{
			return new WindowsStorageDeviceWatcher() as IStorageDeviceWatcher;
		}

		public async Task<IReadOnlyList<ILocatableFolder>> GetDrivesAsync()
		{
			var list = DriveInfo.GetDrives();
			List<ILocatableFolder> drives = new List<ILocatableFolder>();

			foreach (var drive in list)
			{
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
				var driveItem = await DriveItem.CreateFromPropertiesAsync(res.Result, drive.Name.TrimEnd('\\'), type, thumbnail);

				lock (drives)
				{
					// If drive already in list, skip.
					if (drives.Any(x => x.Path == drive.Name))
					{
						continue;
					}

					App.Logger.LogInformation($"Drive added: {driveItem.Path}, {driveItem.Type}");
					drives.Add(driveItem);
				}
			}

			return drives.AsReadOnly();
		}

		public async Task<string> GetPrimaryDrivePathAsync()
		{
			string cDrivePath = @"C:\";
			return new WindowsStorageFolder(await StorageFolder.GetFolderFromPathAsync(cDrivePath)).Path;
		}

		public async Task UpdateDrivePropertiesAsync(ILocatableFolder drive)
		{
			var rootModified = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(drive.Id).AsTask());
			if (rootModified && drive is DriveItem matchingDriveEjected)
			{
				_ = App.Window.DispatcherQueue.EnqueueAsync(() =>
				{
					matchingDriveEjected.Root = rootModified.Result;
					matchingDriveEjected.Text = rootModified.Result.DisplayName;
					return matchingDriveEjected.UpdatePropertiesAsync();
				});
			}
		}
	}
}
