using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Interacts;
using Microsoft.Management.Infrastructure;
using System;
using System.Linq;
using System.Threading.Tasks;

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
			foreach (var item in cimSession.QueryInstances(@"root\cimv2", "WQL", query)) // max 1 result because DriveLetter is unique.
				return (string?)item.CimInstanceProperties["DeviceID"]?.Value ?? string.Empty;

			return string.Empty;
		}

		public static async Task<bool> CheckEmptyDrive(string? drivePath)
		{
			if (string.IsNullOrWhiteSpace(drivePath))
				return false;

			var matchingDrive = App.DrivesManager.Drives.FirstOrDefault(x => drivePath.StartsWith(x.Path, StringComparison.Ordinal));
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
	}
}