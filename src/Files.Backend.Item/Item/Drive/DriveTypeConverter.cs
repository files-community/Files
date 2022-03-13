using Files.Backend.Item.Tools;
using System;
using System.IO;

namespace Files.Backend.Item
{
    internal static class DriveTypeConverter
    {
        private static readonly string driveAPath = "A:".NormalizePath();
        private static readonly string driveBPath = "B:".NormalizePath();

        public static DriveTypes ToDriveType(string path)
        {
            try
            {
                var info = new DriveInfo(path);
                return ToDriveType(info);
            }
            catch (ArgumentException)
            {
                return DriveTypes.Removable;
            }
            catch
            {
                return DriveTypes.Unknown;
            }
        }

        private static DriveTypes ToDriveType(DriveInfo info)
        {
            if (info.DriveType is DriveType.Unknown)
            {
                string drivePath = info.Name.NormalizePath();
                if (drivePath == driveAPath || drivePath == driveBPath)
                {
                    return DriveTypes.FloppyDisk;
                }
            }

            return info.DriveType switch
            {
                DriveType.Fixed => DriveTypes.Fixed,
                DriveType.Removable => DriveTypes.Removable,
                DriveType.Network => DriveTypes.Network,
                DriveType.Ram => DriveTypes.Ram,
                DriveType.CDRom => DriveTypes.CDRom,
                DriveType.NoRootDirectory => DriveTypes.NoRootDirectory,
                _ => DriveTypes.Unknown,
            };
        }
    }
}
