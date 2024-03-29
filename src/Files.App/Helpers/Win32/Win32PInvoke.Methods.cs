// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;
using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;

namespace Files.App.Helpers
{
	public static partial class Win32PInvoke
	{
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

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern bool SetPropW(
			IntPtr hWnd,
			string lpString,
			IntPtr hData
		);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetCursorPos(
			out POINT point
		);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		public static extern IntPtr CreateEvent(
			IntPtr lpEventAttributes,
			bool bManualReset,
			bool bInitialState,
			string lpName
		);

		[DllImport("kernel32.dll")]
		public static extern bool SetEvent(
			IntPtr hEvent
		);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern int GetDpiForWindow(
			IntPtr hwnd
		);

		[DllImport("ole32.dll")]
		public static extern uint CoWaitForMultipleObjects(
			uint dwFlags,
			uint dwMilliseconds,
			ulong nHandles,
			IntPtr[] pHandles,
			out uint dwIndex
		);

		[DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLong")]
		public static extern int SetWindowLongPtr32(
			HWND hWnd,
			WindowLongFlags nIndex,
			IntPtr dwNewLong
		);

		[DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLongPtr")]
		public static extern IntPtr SetWindowLongPtr64(
			HWND hWnd,
			WindowLongFlags nIndex,
			IntPtr dwNewLong
		);

		[DllImport("User32.dll")]
		public extern static short GetKeyState(
			int n
		);

		[DllImport("shell32.dll")]
		public static extern IntPtr SHBrowseForFolder(
			ref BROWSEINFO lpbi
		);

		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		public static extern bool SHGetPathFromIDList(
			IntPtr pidl,
			[MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszPath
		);
	}
}
