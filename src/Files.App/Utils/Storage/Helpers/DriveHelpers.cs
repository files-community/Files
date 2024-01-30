// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Management.Infrastructure;
using System.IO;
using Windows.Devices.Enumeration;
using Windows.Devices.Portable;
using Windows.Storage;
using Windows.Storage.FileProperties;
using DiscUtils.Udf;

namespace Files.App.Utils.Storage
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
			foreach (var item in cimSession.QueryInstances(@"root\cimv2", "WQL", query)) // max 1 result because DriveLetter is unique.
				return (string?)item.CimInstanceProperties["DeviceID"]?.Value ?? string.Empty;

			return string.Empty;
		}

		public static async Task<bool> CheckEmptyDrive(string? drivePath)
		{
			if (string.IsNullOrWhiteSpace(drivePath))
				return false;
			var drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

			var matchingDrive = drivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(x => drivePath.StartsWith(x.Path, StringComparison.Ordinal));
			if (matchingDrive is null || matchingDrive.Type != Data.Items.DriveType.CDRom || matchingDrive.MaxSpace != ByteSizeLib.ByteSize.FromBytes(0))
				return false;

			var ejectButton = await DialogDisplayHelper.ShowDialogAsync(
				"InsertDiscDialog/Title".GetLocalizedResource(),
				string.Format("InsertDiscDialog/Text".GetLocalizedResource(), matchingDrive.Path),
				"InsertDiscDialog/OpenDriveButton".GetLocalizedResource(),
				"Close".GetLocalizedResource());
			if (ejectButton)
			{
				var result = await EjectDeviceAsync(matchingDrive.Path);
				await UIHelpers.ShowDeviceEjectResultAsync(matchingDrive.Type, result);
			}
			return true;
		}

		public static async Task<StorageFolderWithPath> GetRootFromPathAsync(string devicePath)
		{
			if (!Path.IsPathRooted(devicePath))
				return null;

			var drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

			var rootPath = Path.GetPathRoot(devicePath);
			if (devicePath.StartsWith(@"\\?\", StringComparison.Ordinal)) // USB device
			{
				// Check among already discovered drives
				StorageFolder matchingDrive = drivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(x =>
					Helpers.PathNormalization.NormalizePath(x.Path) == Helpers.PathNormalization.NormalizePath(rootPath))?.Root;
				if (matchingDrive is null)
				{
					// Check on all removable drives
					var remDevices = await DeviceInformation.FindAllAsync(StorageDevice.GetDeviceSelector());
					string normalizedRootPath = Helpers.PathNormalization.NormalizePath(rootPath).Replace(@"\\?\", string.Empty, StringComparison.Ordinal);
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
							// Ignore this..
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
				rootPath = lastSepIndex > 1 ? rootPath.Substring(0, lastSepIndex) : rootPath; // Remove share name
				return new StorageFolderWithPath(await StorageFolder.GetFolderFromPathAsync(rootPath), rootPath);
			}
			// It's ok to return null here, on normal drives StorageFolder.GetFolderFromPathAsync works
			return null;
		}

		public static Data.Items.DriveType GetDriveType(System.IO.DriveInfo drive)
		{
			if (drive.DriveType is System.IO.DriveType.Unknown)
			{
				string path = PathNormalization.NormalizePath(drive.Name);

				if (path is "A:" or "B:")
					return Data.Items.DriveType.FloppyDisk;
			}

			return drive.DriveType switch
			{
				System.IO.DriveType.CDRom => Data.Items.DriveType.CDRom,
				System.IO.DriveType.Fixed => Data.Items.DriveType.Fixed,
				System.IO.DriveType.Network => Data.Items.DriveType.Network,
				System.IO.DriveType.NoRootDirectory => Data.Items.DriveType.NoRootDirectory,
				System.IO.DriveType.Ram => Data.Items.DriveType.Ram,
				System.IO.DriveType.Removable => Data.Items.DriveType.Removable,
				_ => Data.Items.DriveType.Unknown,
			};
		}

		public static string GetExtendedDriveLabel(DriveInfo drive)
		{
			return SafetyExtensions.IgnoreExceptions(() =>
			{
				if (drive.DriveType is not System.IO.DriveType.CDRom || drive.DriveFormat is not "UDF")
					return drive.VolumeLabel;
				return SafetyExtensions.IgnoreExceptions(() =>
				{
					var dosDevicePath = Vanara.PInvoke.Kernel32.QueryDosDevice(drive.Name).FirstOrDefault();
					if (string.IsNullOrEmpty(dosDevicePath))
						return drive.VolumeLabel;
					using var driveStream = new FileStream(dosDevicePath.Replace(@"\Device\", @"\\.\"), FileMode.Open, FileAccess.Read);
					using var udf = new UdfReader(driveStream);
					return udf.VolumeLabel;
				}) ?? drive.VolumeLabel;
			}) ?? "";
		}

		public static async Task<StorageItemThumbnail> GetThumbnailAsync(StorageFolder folder)
			=> (StorageItemThumbnail)await FilesystemTasks.Wrap(()
				=> folder.GetThumbnailAsync(ThumbnailMode.SingleItem, 40, ThumbnailOptions.UseCurrentScale).AsTask()
			);
	}
}