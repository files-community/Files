using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Interacts;
using Files.App.ViewModels;
using Microsoft.Management.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Portable;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Files.App.Helpers
{
	public static class DriveHelpers
	{
		public static async Task<bool> EjectDeviceAsync(string path)
		{
			var removableDevice = new RemovableDevice(path);
			return await removableDevice.EjectAsync();
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
			if (matchingDrive is null || matchingDrive.Type != DataModels.NavigationControlItems.DriveType.CDRom || matchingDrive.MaxSpace != ByteSizeLib.ByteSize.FromBytes(0))
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

		public static DataModels.NavigationControlItems.DriveType GetDriveType(System.IO.DriveInfo drive)
		{
			if (drive.DriveType is System.IO.DriveType.Unknown)
			{
				string path = PathNormalization.NormalizePath(drive.Name);

				if (path is "A:" or "B:")
					return DataModels.NavigationControlItems.DriveType.FloppyDisk;
			}

			return drive.DriveType switch
			{
				System.IO.DriveType.CDRom => DataModels.NavigationControlItems.DriveType.CDRom,
				System.IO.DriveType.Fixed => DataModels.NavigationControlItems.DriveType.Fixed,
				System.IO.DriveType.Network => DataModels.NavigationControlItems.DriveType.Network,
				System.IO.DriveType.NoRootDirectory => DataModels.NavigationControlItems.DriveType.NoRootDirectory,
				System.IO.DriveType.Ram => DataModels.NavigationControlItems.DriveType.Ram,
				System.IO.DriveType.Removable => DataModels.NavigationControlItems.DriveType.Removable,
				_ => DataModels.NavigationControlItems.DriveType.Unknown,
			};
		}

		public static async Task<StorageItemThumbnail> GetThumbnailAsync(StorageFolder folder)
			=> (StorageItemThumbnail)await FilesystemTasks.Wrap(()
				=> folder.GetThumbnailAsync(ThumbnailMode.SingleItem, 40, ThumbnailOptions.UseCurrentScale).AsTask()
			);
	}
}