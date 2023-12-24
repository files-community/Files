// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;

namespace Files.Core.Helpers
{
	public static partial class Win32PInvoke
	{
		[DllImport("api-ms-win-core-wow64-l1-1-1.dll", SetLastError = true)]
		public static extern bool IsWow64Process2(
			IntPtr process,
			out ushort processMachine,
			out ushort nativeMachine
		);
	}
}
