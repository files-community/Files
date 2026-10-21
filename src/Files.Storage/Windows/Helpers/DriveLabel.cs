// Copyright (c) Files Community
// Licensed under the MIT License.

using DiscUtils.Udf;
using Files.Shared.Extensions;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Files.App.Storage;

public static class DriveLabel
{
	public static unsafe string GetExtendedDriveLabel(SystemIO.DriveInfo drive)
	{
		return SafetyExtensions.IgnoreExceptions(() =>
		{
			if (drive.DriveType is not SystemIO.DriveType.CDRom || drive.DriveFormat is not "UDF")
				return drive.VolumeLabel;

			return SafetyExtensions.IgnoreExceptions(() =>
			{
				string dosDevicePath = "";

				fixed (char* cDeviceName = drive.Name)
				{
					var cch = PInvoke.QueryDosDevice(cDeviceName, null, 0u);

					fixed (char* cTargetPath = new char[cch])
					{
						PWSTR pszTargetPath = new(cTargetPath);
						PInvoke.QueryDosDevice(cDeviceName, pszTargetPath, 0u);
						dosDevicePath = pszTargetPath.ToString();
					}
				}

				if (string.IsNullOrEmpty(dosDevicePath))
					return drive.VolumeLabel;

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
