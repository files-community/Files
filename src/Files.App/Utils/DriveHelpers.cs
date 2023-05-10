// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Management.Infrastructure;
using Windows.Devices.Enumeration;
using Windows.Devices.Portable;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Files.App.Helpers
{
	public static class DriveHelpers
	{
		public static Task<bool> EjectDeviceAsync(string path)
		{
			var removableDevice = new RemovableDevice(path);
			return removableDevice.EjectAsync();
		}

		public static string GetVolumeId(string driveName)
		{
			string name = driveName.ToUpperInvariant();
			string query = $"SELECT DeviceID FROM Win32_Volume WHERE DriveLetter = '{name}'";

			using var cimSession = CimSession.Create(null);

			// Max 1 result because DriveLetter is unique.
			foreach (var item in cimSession.QueryInstances(@"root\cimv2", "WQL", query))
				return (string?)item.CimInstanceProperties["DeviceID"]?.Value ?? string.Empty;

			return string.Empty;
		}

		public static async Task<bool> CheckEmptyDrive(string? drivePath)
		{
			if (string.IsNullOrWhiteSpace(drivePath))
				return false;
			var drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

			var matchingDrive = drivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(x => drivePath.StartsWith(x.Path, StringComparison.Ordinal));
			if (matchingDrive is null || matchingDrive.Type != DriveType.CDRom || matchingDrive.MaxSpace != ByteSizeLib.ByteSize.FromBytes(0))
				return false;

			var ejectButton = await DialogDisplayHelper.ShowDialogAsync(
				"InsertDiscDialog/Title".GetLocalizedResource(),
				string.Format("InsertDiscDialog/Text".GetLocalizedResource(), matchingDrive.Path),
				"InsertDiscDialog/OpenDriveButton".GetLocalizedResource(),
				"Close".GetLocalizedResource());

			if (ejectButton)
			{
				var result = await EjectDeviceAsync(matchingDrive.Path);
				await UIHelpers.ShowDeviceEjectResultAsync(result);
			}

			return true;
		}

		public static async Task<StorageFolderWithPath> GetRootFromPathAsync(string devicePath)
		{
			if (!SystemIO.Path.IsPathRooted(devicePath))
				return null;

			var drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

			var rootPath = SystemIO.Path.GetPathRoot(devicePath);
			if (devicePath.StartsWith(@"\\?\", StringComparison.Ordinal)) // USB device
			{
				// Check among already discovered drives
				StorageFolder matchingDrive = drivesViewModel.Drives
					.Cast<DriveItem>()
					.FirstOrDefault(x =>
						PathNormalization.NormalizePath(x.Path) == PathNormalization.NormalizePath(rootPath))?.Root;

				if (matchingDrive is null)
				{
					// Check on all removable drives
					var remDevices = await DeviceInformation.FindAllAsync(StorageDevice.GetDeviceSelector());
					string normalizedRootPath = PathNormalization.NormalizePath(rootPath).Replace(@"\\?\", string.Empty, StringComparison.Ordinal);

					foreach (var item in remDevices)
					{
						try
						{
							var root = StorageDevice.FromId(item.Id);
							if (normalizedRootPath == root.Name.ToUpperInvariant())
							{
								matchingDrive = root;
								break;
							}
						}
						catch (Exception)
						{
							// Ignore this
						}
					}
				}
				if (matchingDrive is not null)
				{
					return new StorageFolderWithPath(matchingDrive, rootPath);
				}
			}
			// Network share
			else if (devicePath.StartsWith(@"\\", StringComparison.Ordinal) &&
				!devicePath.StartsWith(@"\\SHELL\", StringComparison.Ordinal))
			{
				int lastSepIndex = rootPath.LastIndexOf(@"\", StringComparison.Ordinal);

				 // Remove share name
				rootPath = lastSepIndex > 1 ? rootPath.Substring(0, lastSepIndex) : rootPath;

				return new StorageFolderWithPath(await StorageFolder.GetFolderFromPathAsync(rootPath), rootPath);
			}

			// It's ok to return null here, on normal drives StorageFolder.GetFolderFromPathAsync works
			return null;
		}

		public static DriveType GetDriveType(SystemIO.DriveInfo drive)
		{
			if (drive.DriveType is SystemIO.DriveType.Unknown)
			{
				string path = PathNormalization.NormalizePath(drive.Name);

				if (path is "A:" or "B:")
					return DriveType.FloppyDisk;
			}

			return drive.DriveType switch
			{
				SystemIO.DriveType.CDRom => DriveType.CDRom,
				SystemIO.DriveType.Fixed => DriveType.Fixed,
				SystemIO.DriveType.Network => DriveType.Network,
				SystemIO.DriveType.NoRootDirectory => DriveType.NoRootDirectory,
				SystemIO.DriveType.Ram => DriveType.Ram,
				SystemIO.DriveType.Removable => DriveType.Removable,
				_ => DriveType.Unknown,
			};
		}

		public static async Task<StorageItemThumbnail> GetThumbnailAsync(StorageFolder folder)
		{
			return (StorageItemThumbnail)await FilesystemTasks.Wrap(()
				=> folder.GetThumbnailAsync(ThumbnailMode.SingleItem, 40, ThumbnailOptions.UseCurrentScale).AsTask());
		}
	}
}
