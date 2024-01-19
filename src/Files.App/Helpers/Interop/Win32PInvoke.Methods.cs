// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace Files.App.Helpers
{
	public static partial class Win32PInvoke
	{
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
		public static extern short GetKeyState(
			short code
		);

		[DllImport("Shcore.dll", SetLastError = true)]
		public static extern int GetDpiForMonitor(
			IntPtr hmonitor,
			uint dpiType,
			out uint dpiX,
			out uint dpiY
		);

		[DllImport("api-ms-win-core-processthreads-l1-1-0.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool OpenProcessToken(
			[In] IntPtr ProcessHandle,
			TokenAccess DesiredAccess,
			out IntPtr TokenHandle
		);

		[DllImport("api-ms-win-core-processthreads-l1-1-2.dll", SetLastError = true, ExactSpelling = true)]
		public static extern IntPtr GetCurrentProcess();

		[DllImport("api-ms-win-security-base-l1-1-0.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetTokenInformation(
			IntPtr hObject,
			TOKEN_INFORMATION_CLASS tokenInfoClass,
			IntPtr pTokenInfo,
			int tokenInfoLength,
			out int returnLength
		);

		[DllImport("api-ms-win-core-handle-l1-1-0.dll")]
		public static extern bool CloseHandle(
			IntPtr hObject
		);

		[DllImport("api-ms-win-security-base-l1-1-0.dll", ExactSpelling = true, SetLastError = true)]
		public static extern int GetLengthSid(
			IntPtr pSid
		);

		[DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CryptUnprotectData(
			in CRYPTOAPI_BLOB pDataIn,
			StringBuilder szDataDescr,
			in CRYPTOAPI_BLOB pOptionalEntropy,
			IntPtr pvReserved,
			IntPtr pPromptStruct,
			CryptProtectFlags dwFlags,
			out CRYPTOAPI_BLOB pDataOut
		);

		[DllImport("api-ms-win-core-wow64-l1-1-1.dll", SetLastError = true)]
		public static extern bool IsWow64Process2(
			IntPtr process,
			out ushort processMachine,
			out ushort nativeMachine
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

		public delegate void LpoverlappedCompletionRoutine(
			uint dwErrorCode,
			uint dwNumberOfBytesTransfered,
			OVERLAPPED lpOverlapped
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

		[DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
		static extern int RmRegisterResources(
			uint pSessionHandle,
			uint nFiles,
			string[] rgsFilenames,
			uint nApplications,
			[In] RM_UNIQUE_PROCESS[] rgApplications,
			uint nServices,
			string[] rgsServiceNames
		);

		[DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
		static extern int RmStartSession(
			out uint pSessionHandle,
			int dwSessionFlags,
			string strSessionKey
		);

		[DllImport("rstrtmgr.dll")]
		static extern int RmEndSession(
			uint pSessionHandle
		);

		[DllImport("rstrtmgr.dll")]
		static extern int RmGetList(
			uint dwSessionHandle,
			out uint pnProcInfoNeeded,
			ref uint pnProcInfo,
			[In, Out] RM_PROCESS_INFO[] rgAffectedApps,
			ref uint lpdwRebootReasons
		);

		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern IntPtr CreateFileFromApp(
			string lpFileName,
			uint dwDesiredAccess,
			uint dwShareMode,
			IntPtr SecurityAttributes,
			uint dwCreationDisposition,
			uint dwFlagsAndAttributes,
			IntPtr hTemplateFile
		);

		[DllImport("api-ms-win-core-io-l1-1-0.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeviceIoControl(
			IntPtr hDevice,
			uint dwIoControlCode,
			IntPtr lpInBuffer,
			uint nInBufferSize,
			//IntPtr lpOutBuffer,
			out REPARSE_DATA_BUFFER outBuffer,
			uint nOutBufferSize,
			out uint lpBytesReturned,
			IntPtr lpOverlapped
		);

		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern IntPtr CreateFile2FromApp(
			string lpFileName,
			uint dwDesiredAccess,
			uint dwShareMode,
			uint dwCreationDisposition,
			IntPtr pCreateExParams
		);

		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Auto,
		CallingConvention = CallingConvention.StdCall,
		SetLastError = true)]
		public static extern bool CreateDirectoryFromApp(
			string lpPathName,
			IntPtr SecurityAttributes
		);

		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern bool MoveFileFromApp(
			string lpExistingFileName,
			string lpNewFileName
		);

		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern bool CopyFileFromApp(
			string lpExistingFileName,
			string lpNewFileName,
			bool bFailIfExists
		);

		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern bool DeleteFileFromApp(
			string lpFileName
		);

		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern bool RemoveDirectoryFromApp(
			string lpPathName
		);

		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetFileAttributesExFromApp(
			string lpFileName,
			GET_FILEEX_INFO_LEVELS fInfoLevelId,
			out WIN32_FILE_ATTRIBUTE_DATA lpFileInformation);

		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetFileAttributesFromApp(
			string lpFileName,
			FileAttributes dwFileAttributes);

		[DllImport("api-ms-win-core-file-l1-2-1.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern uint SetFilePointer(
			IntPtr hFile,
			long lDistanceToMove,
			IntPtr lpDistanceToMoveHigh,
			uint dwMoveMethod
		);

		[DllImport("api-ms-win-core-file-l1-2-1.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public unsafe static extern bool ReadFile(
			IntPtr hFile,
			byte* lpBuffer,
			int nBufferLength,
			int* lpBytesReturned,
			IntPtr lpOverlapped
		);

		[DllImport("api-ms-win-core-file-l1-2-1.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public unsafe static extern bool WriteFile(
			IntPtr hFile,
			byte* lpBuffer,
			int nBufferLength,
			int* lpBytesWritten,
			IntPtr lpOverlapped
		);

		[DllImport("api-ms-win-core-file-l1-2-1.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern bool WriteFileEx(
			IntPtr hFile,
			byte[] lpBuffer,
			uint nNumberOfBytesToWrite,
			[In] ref NativeOverlapped lpOverlapped,
			LPOVERLAPPED_COMPLETION_ROUTINE lpCompletionRoutine
		);

		public delegate void LPOVERLAPPED_COMPLETION_ROUTINE(
			uint dwErrorCode,
			uint dwNumberOfBytesTransfered,
			ref NativeOverlapped lpOverlapped
		);

		[DllImport("api-ms-win-core-file-l1-2-1.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern bool GetFileTime(
			[In] IntPtr hFile,
			out FILETIME lpCreationTime,
			out FILETIME lpLastAccessTime,
			out FILETIME lpLastWriteTime
		);

		[DllImport("api-ms-win-core-file-l1-2-1.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern bool SetFileTime(
			[In] IntPtr hFile,
			in FILETIME lpCreationTime,
			in FILETIME lpLastAccessTime,
			in FILETIME lpLastWriteTime
		);

		[DllImport("api-ms-win-core-file-l2-1-1.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern bool GetFileInformationByHandleEx(
			IntPtr hFile,
			FILE_INFO_BY_HANDLE_CLASS infoClass,
			out FILE_ID_BOTH_DIR_INFO dirInfo,
			uint dwBufferSize
		);

		[DllImport("api-ms-win-core-file-l2-1-1.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern bool GetFileInformationByHandleEx(
			IntPtr hFile,
			FILE_INFO_BY_HANDLE_CLASS infoClass,
			IntPtr dirInfo,
			uint dwBufferSize
		);

		[DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr FindFirstStreamW(
			string lpFileName,
			StreamInfoLevels InfoLevel,
			[In, Out, MarshalAs(UnmanagedType.LPStruct)] WIN32_FIND_STREAM_DATA lpFindStreamData,
			uint dwFlags
		);

		[DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool FindNextStreamW(
			IntPtr hndFindFile,
			[In, Out, MarshalAs(UnmanagedType.LPStruct)] WIN32_FIND_STREAM_DATA lpFindStreamData
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

		[DllImport("user32.dll", SetLastError = true)]
		public static extern void SwitchToThisWindow(
			IntPtr hWnd, bool altTab
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
			nint hWnd,
			int nIndex,
			IntPtr dwNewLong
		);

		[DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLongPtr")]
		public static extern IntPtr SetWindowLongPtr64(
			nint hWnd,
			int nIndex,
			IntPtr dwNewLong
		);

		[DllImport("user32.dll")]
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

		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr FindFirstFileExFromApp(
			string lpFileName,
			FINDEX_INFO_LEVELS fInfoLevelId,
			out WIN32_FIND_DATA lpFindFileData,
			FINDEX_SEARCH_OPS fSearchOp,
			IntPtr lpSearchFilter,
			int dwAdditionalFlags
		);

		[DllImport("api-ms-win-core-file-l1-1-0.dll", CharSet = CharSet.Unicode)]
		public static extern bool FindNextFile(
			IntPtr hFindFile,
			out WIN32_FIND_DATA lpFindFileData
		);

		[DllImport("api-ms-win-core-file-l1-1-0.dll")]
		public static extern bool FindClose(
			IntPtr hFindFile);

		[DllImport("api-ms-win-core-timezone-l1-1-0.dll", SetLastError = true)]
		public static extern bool FileTimeToSystemTime(
			ref FILETIME lpFileTime,
			out SYSTEMTIME lpSystemTime
		);
	}
}
