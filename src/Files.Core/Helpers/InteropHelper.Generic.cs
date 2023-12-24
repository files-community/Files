// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Text;

namespace Files.Core.Helpers
{
	public class NativeWinApiHelper
	{
		[DllImport("api-ms-win-core-wow64-l1-1-1.dll", SetLastError = true)]
		private static extern bool IsWow64Process2(
			IntPtr process,
			out ushort processMachine,
			out ushort nativeMachine
		);

		// https://stackoverflow.com/questions/54456140/how-to-detect-were-running-under-the-arm64-version-of-windows-10-in-net
		// https://learn.microsoft.com/windows/win32/sysinfo/image-file-machine-constants
		private static bool? isRunningOnArm = null;
		public static bool IsRunningOnArm
		{
			get
			{
				if (isRunningOnArm is null)
				{
					isRunningOnArm = IsArmProcessor();
					App.Logger.LogInformation("Running on ARM: {0}", isRunningOnArm);
				}
				return isRunningOnArm ?? false;
			}
		}

		private static bool IsArmProcessor()
		{
			var handle = System.Diagnostics.Process.GetCurrentProcess().Handle;
			if (!IsWow64Process2(handle, out _, out var nativeMachine))
			{
				return false;
			}
			return (nativeMachine == 0xaa64 ||
					nativeMachine == 0x01c0 ||
					nativeMachine == 0x01c2 ||
					nativeMachine == 0x01c4);
		}

		private static bool? isHasThreadAccessPropertyPresent = null;

		public static bool IsHasThreadAccessPropertyPresent
		{
			get
			{
				isHasThreadAccessPropertyPresent ??= ApiInformation.IsPropertyPresent(typeof(DispatcherQueue).FullName, "HasThreadAccess");
				return isHasThreadAccessPropertyPresent ?? false;
			}
		}

		public static Task<string> GetFileAssociationAsync(string filePath)
			=> Win32API.GetFileAssociationAsync(filePath, true);
	}
}
