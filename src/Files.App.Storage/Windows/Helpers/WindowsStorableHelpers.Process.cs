// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;
using Windows.Win32.System.SystemInformation;

namespace Files.App.Storage
{
	public unsafe static partial class WindowsStorableHelpers
	{
		public static bool IsOnArmProcessor()
		{
			IMAGE_FILE_MACHINE dwMachineType = default;

			// Assumes the current process token has "PROCESS_QUERY_INFORMATION" or "PROCESS_QUERY_LIMITED_INFORMATION" access right
			bool fResult = PInvoke.IsWow64Process2(PInvoke.GetCurrentProcess(), null, &dwMachineType);
			if (!fResult)
				Debug.WriteLine($"{nameof(PInvoke.IsWow64Process2)} has failed.");

			return dwMachineType is
				IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_THUMB or
				IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ARMNT or
				IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ARM64 or
				IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ARM;
		}
	}
}
