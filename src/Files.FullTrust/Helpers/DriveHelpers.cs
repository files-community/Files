using System.Management;
using System.Runtime.Versioning;

namespace Files.FullTrust.Helpers
{
    internal static class DriveHelpers
    {
        [SupportedOSPlatform("Windows")]
        public static string GetVolumeId(string driveName)
        {
            string name = driveName.ToUpperInvariant();
            string query = $"SELECT DeviceID FROM Win32_Volume WHERE DriveLetter = '{name}'";

            using var searcher = new ManagementObjectSearcher(query);
            foreach (ManagementObject item in searcher.Get()) // max 1 result because DriveLetter is unique.
            {
                return (string)item?.GetPropertyValue("DeviceID") ?? string.Empty;
            }

            return string.Empty;
        }
    }
}
