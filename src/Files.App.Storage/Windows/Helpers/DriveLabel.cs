// Copyright (c) Files Community
// Licensed under the MIT License.

using DiscUtils.Udf;
using Files.Shared.Extensions;
using Windows.Win32;

namespace Files.App.Storage;

public static class DriveLabel
{
	public static string GetExtendedDriveLabel(SystemIO.DriveInfo drive)
	{
		return SafetyExtensions.IgnoreExceptions(() =>
		{
			if (drive.DriveType is not SystemIO.DriveType.CDRom || drive.DriveFormat is not "UDF")
				return drive.VolumeLabel;

			return SafetyExtensions.IgnoreExceptions(() =>
			{
				Span<char> targetPath = stackalloc char[4096];
				if (PInvoke.QueryDosDevice(drive.Name.TrimEnd('\\'), targetPath) is 0)
					return drive.VolumeLabel;

				string dosDevicePath = targetPath[..targetPath.IndexOf('\0')].ToString();

				using var driveStream = new SystemIO.FileStream(
					dosDevicePath.Replace(@"\Device\", @"\\.\"),
					SystemIO.FileMode.Open,
					SystemIO.FileAccess.Read);

				using var udf = new UdfReader(driveStream);

				return udf.VolumeLabel;
			}) ?? drive.VolumeLabel;

		}) ?? "";
	}
}
