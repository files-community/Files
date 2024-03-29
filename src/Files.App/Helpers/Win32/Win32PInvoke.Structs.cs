// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;

namespace Files.App.Helpers
{
	public static partial class Win32PInvoke
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct RM_UNIQUE_PROCESS
		{
			public int dwProcessId;
			public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct RM_PROCESS_INFO
		{
			public RM_UNIQUE_PROCESS Process;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
			public string strAppName;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
			public string strServiceShortName;

			public RM_APP_TYPE ApplicationType;
			public uint AppStatus;
			public uint TSSessionId;
			[MarshalAs(UnmanagedType.Bool)]
			public bool bRestartable;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int X;
			public int Y;

			public POINT(int x, int y) => (X, Y) = (x, y);
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct BROWSEINFO
		{
			public IntPtr hwndOwner;
			public IntPtr pidlRoot;
			public string pszDisplayName;
			public string lpszTitle;
			public uint ulFlags;
			public IntPtr lpfn;
			public int lParam;
			public IntPtr iImage;
		}
	}
}
