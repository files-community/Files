// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace Files.App.Helpers
{
	public static partial class Win32PInvoke
	{
		public delegate void LpoverlappedCompletionRoutine(
			uint dwErrorCode,
			uint dwNumberOfBytesTransfered,
			OVERLAPPED lpOverlapped
		);

		public delegate void LPOVERLAPPED_COMPLETION_ROUTINE(
			uint dwErrorCode,
			uint dwNumberOfBytesTransfered,
			ref NativeOverlapped lpOverlapped
		);

		[DllImport("api-ms-win-core-handle-l1-1-0.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseHandle(
			IntPtr hObject
		);

		[DllImport("api-ms-win-core-io-l1-1-1.dll")]
		public static extern bool CancelIoEx(
			IntPtr hFile,
			IntPtr lpOverlapped
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
			IntPtr lpOverlapped);

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
			LPOVERLAPPED_COMPLETION_ROUTINE lpCompletionRoutine);

		[DllImport("api-ms-win-core-file-l1-2-1.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern bool GetFileTime(
			[In] IntPtr hFile,
			out System.Runtime.InteropServices.ComTypes.FILETIME lpCreationTime,
			out System.Runtime.InteropServices.ComTypes.FILETIME lpLastAccessTime,
			out System.Runtime.InteropServices.ComTypes.FILETIME lpLastWriteTime
		);

		[DllImport("api-ms-win-core-file-l1-2-1.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern bool SetFileTime(
			[In] IntPtr hFile,
			in System.Runtime.InteropServices.ComTypes.FILETIME lpCreationTime,
			in System.Runtime.InteropServices.ComTypes.FILETIME lpLastAccessTime,
			in System.Runtime.InteropServices.ComTypes.FILETIME lpLastWriteTime
		);

		[DllImport("api-ms-win-core-file-l2-1-1.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern bool GetFileInformationByHandleEx(
			IntPtr hFile,
			FILE_INFO_BY_HANDLE_CLASS infoClass,
			out FILE_ID_BOTH_DIR_INFO dirInfo,
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

		[DllImport("api-ms-win-core-wow64-l1-1-1.dll", SetLastError = true)]
		public static extern bool IsWow64Process2(
			IntPtr process,
			out ushort processMachine,
			out ushort nativeMachine
		);

		[DllImport("api-ms-win-core-file-l1-1-0.dll", CharSet = CharSet.Unicode)]
		public static extern bool FindNextFile(
			IntPtr hFindFile,
			out WIN32_FIND_DATA lpFindFileData
		);

		[DllImport("api-ms-win-core-file-l1-1-0.dll")]
		public static extern bool FindClose(
			IntPtr hFindFile
		);

		[DllImport("api-ms-win-core-timezone-l1-1-0.dll", SetLastError = true)]
		public static extern bool FileTimeToSystemTime(
			ref System.Runtime.InteropServices.ComTypes.FILETIME lpFileTime,
			out SYSTEMTIME lpSystemTime
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

		[LibraryImport("shell32.dll", EntryPoint = "#865", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static partial bool IsElevationRequired(
			[MarshalAs(UnmanagedType.LPWStr)] string pszPath);

		// cryptui.dll
		[DllImport("cryptui.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public unsafe static extern bool CryptUIDlgViewSignerInfo(CRYPTUI_VIEWSIGNERINFO_STRUCT* pViewInfo);
	}
}
