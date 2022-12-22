using Files.App.Interacts;
using Microsoft.Management.Infrastructure;
using System.Threading.Tasks;

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
			{
				return (string)item.CimInstanceProperties["DeviceID"]?.Value ?? string.Empty;
			}

			return string.Empty;
		}
	}
}