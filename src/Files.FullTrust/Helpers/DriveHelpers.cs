using System.Management;
using System.Runtime.Versioning;

namespace Files.FullTrust.Helpers
{
    [SupportedOSPlatform("Windows")]
    internal static class DriveHelpers
    {
        /// <summary>
        /// Return the Volume ID of a drive
        /// </summary>
        /// <param name="driveName">The drive name (C:, D:, E:, …)</param>
        public static string GetVolumeID(string driveName)
        {
            string name = driveName.ToUpper();
            string query = $"Select * from Win32_Volume where DriveLetter = '{name}'";
            var searcher = new ManagementObjectSearcher(query);

            foreach (ManagementObject item in searcher.Get()) // max 1 result because DriveLetter is unique.
            {
                return item["DeviceID"]?.ToString() ?? string.Empty;
            }

            return string.Empty;
        }
    }
}
