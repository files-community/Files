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
		public delegate void LpoverlappedCompletionRoutine(
			uint dwErrorCode,
			uint dwNumberOfBytesTransfered,
			OVERLAPPED lpOverlapped
		);

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
		
		[DllImport("shell32.dll")]
		public static extern IntPtr SHBrowseForFolder(
			ref BROWSEINFO lpbi
		);

		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		public static extern bool SHGetPathFromIDList(
			IntPtr pidl,
			[MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszPath
		);

		[DllImport("api-ms-win-core-handle-l1-1-0.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseHandle(
			IntPtr hObject
		);

		[DllImport("api-ms-win-core-io-l1-1-1.dll")]
		public static extern bool GetOverlappedResult(
			IntPtr hFile,
			OVERLAPPED lpOverlapped,
			out int lpNumberOfBytesTransferred,
			bool bWait
		);

		[DllImport("api-ms-win-core-io-l1-1-1.dll")]
		public static extern bool CancelIo(
			IntPtr hFile
		);

		[DllImport("api-ms-win-core-io-l1-1-1.dll")]
		public static extern bool CancelIoEx(
			IntPtr hFile,
			IntPtr lpOverlapped
		);

		[DllImport("api-ms-win-core-synch-l1-2-0.dll")]
		public static extern uint WaitForMultipleObjectsEx(
			uint nCount,
			IntPtr[] lpHandles,
			bool bWaitAll,
			uint dwMilliseconds,
			bool bAlertable
		);

		[DllImport("api-ms-win-core-synch-l1-2-0.dll", SetLastError = true)]
		public static extern bool ResetEvent(
			IntPtr hEvent
		);

		[DllImport("api-ms-win-core-synch-l1-2-0.dll", SetLastError = true)]
		public static extern uint WaitForSingleObjectEx(
			IntPtr hHandle,
			uint dwMilliseconds,
			bool bAlertable
		);

		[DllImport("api-ms-win-core-file-l2-1-0.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public unsafe static extern bool ReadDirectoryChangesW(
			IntPtr hDirectory,
			byte* lpBuffer,
			int nBufferLength,
			bool bWatchSubtree,
			int dwNotifyFilter,
			int* lpBytesReturned,
			ref OVERLAPPED lpOverlapped,
			LpoverlappedCompletionRoutine lpCompletionRoutine
		);

		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern IntPtr CreateFileFromAppW(
			string lpFileName,
			uint dwDesiredAccess,
			uint dwShareMode,
			IntPtr SecurityAttributes,
			uint dwCreationDisposition,
			uint dwFlagsAndAttributes,
			IntPtr hTemplateFile
		);

		[DllImport("api-ms-win-core-io-l1-1-0.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool DeviceIoControl(
			IntPtr hDevice,
			uint dwIoControlCode,
			IntPtr lpInBuffer,
			uint nInBufferSize,
			IntPtr lpOutBuffer,
			uint nOutBufferSize,
			out uint lpBytesReturned,
			IntPtr lpOverlapped
		);

		[DllImport("api-ms-win-core-io-l1-1-0.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool DeviceIoControl(
			IntPtr hDevice,
			uint dwIoControlCode,
			byte[] lpInBuffer,
			uint nInBufferSize,
			IntPtr lpOutBuffer,
			uint nOutBufferSize,
			out uint lpBytesReturned,
			IntPtr lpOverlapped
		);

		[DllImport("user32.dll")]
		public static extern int ToUnicode(
			uint virtualKeyCode,
			uint scanCode,
			byte[] keyboardState,
			[Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 64)] StringBuilder receivingBuffer,
			int bufferSize,
			uint flags
		);
	}
}
