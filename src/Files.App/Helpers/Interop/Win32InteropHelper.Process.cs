// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Foundation.Metadata;
using Windows.System;

namespace Files.App.Helpers
{
	public static partial class Win32InteropHelper
	{
		// https://stackoverflow.com/questions/54456140/how-to-detect-were-running-under-the-arm64-version-of-windows-10-in-net
		// https://learn.microsoft.com/windows/win32/sysinfo/image-file-machine-constants
		public static bool IsRunningOnArm
			=> IsArmProcessor();

		private static bool IsArmProcessor()
		{
			var handle = Process.GetCurrentProcess().Handle;
			if (!Win32Interop.IsWow64Process2(handle, out _, out var nativeMachine))
			{
				return false;
			}

			return
				nativeMachine == 0xaa64 ||
				nativeMachine == 0x01c0 ||
				nativeMachine == 0x01c2 ||
				nativeMachine == 0x01c4;
		}

		public static bool IsHasThreadAccessPropertyPresent
			=> ApiInformation.IsPropertyPresent(typeof(DispatcherQueue).FullName, "HasThreadAccess");

		public static Task<string> GetFileAssociationAsync(string filePath)
		{
			return Win32API.GetFileAssociationAsync(filePath, true);
		}
	}
}
