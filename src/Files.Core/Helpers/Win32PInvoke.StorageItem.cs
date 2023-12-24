// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;

namespace Files.Core.Helpers
{
	// https://stackoverflow.com/questions/317071/how-do-i-find-out-which-process-is-locking-a-file-using-net/317209#317209
	public static partial class Win32PInvoke
	{
		public const int RmRebootReasonNone = 0;
		public const int CCH_RM_MAX_APP_NAME = 255;
		public const int CCH_RM_MAX_SVC_NAME = 63;

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

		public enum RM_APP_TYPE
		{
			RmUnknownApp = 0,
			RmMainWindow = 1,
			RmOtherWindow = 2,
			RmService = 3,
			RmExplorer = 4,
			RmConsole = 5,
			RmCritical = 1000
		}

		[DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
		public static extern int RmRegisterResources(
			uint pSessionHandle,
			uint nFiles,
			string[] rgsFilenames,
			uint nApplications,
			[In] RM_UNIQUE_PROCESS[] rgApplications,
			uint nServices,
			string[] rgsServiceNames
		);

		[DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
		public static extern int RmStartSession(
			out uint pSessionHandle,
			int dwSessionFlags,
			string strSessionKey
		);

		[DllImport("rstrtmgr.dll")]
		public static extern int RmEndSession(
			uint pSessionHandle
		);

		[DllImport("rstrtmgr.dll")]
		public static extern int RmGetList(
			uint dwSessionHandle,
			out uint pnProcInfoNeeded,
			ref uint pnProcInfo,
			[In, Out] RM_PROCESS_INFO[] rgAffectedApps,
			ref uint lpdwRebootReasons
		);
	}
}
