// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Files.Core.Helpers
{
	public static partial class Win32PInvoke
	{
		public const uint GENERIC_READ = 0x80000000;
		public const uint GENERIC_WRITE = 0x40000000;
		public const uint FILE_SHARE_READ = 0x00000001;
		public const uint FILE_SHARE_WRITE = 0x00000002;
		public const int OPEN_EXISTING = 3;

		public const uint FILE_APPEND_DATA = 0x0004;
		public const uint FILE_WRITE_ATTRIBUTES = 0x100;

		public const uint FILE_SHARE_DELETE = 0x00000004;

		public const uint FILE_BEGIN = 0;
		public const uint FILE_END = 2;

		public const uint CREATE_ALWAYS = 2;
		public const uint CREATE_NEW = 1;
		public const uint OPEN_ALWAYS = 4;
		public const uint TRUNCATE_EXISTING = 5;

		public const int MAXIMUM_REPARSE_DATA_BUFFER_SIZE = 16 * 1024;
		public const int FSCTL_GET_REPARSE_POINT = 0x000900A8;
		public const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003;
		public const uint IO_REPARSE_TAG_SYMLINK = 0xA000000C;

		public enum File_Attributes : uint
		{
			Readonly = 0x00000001,
			Hidden = 0x00000002,
			System = 0x00000004,
			Directory = 0x00000010,
			Archive = 0x00000020,
			Device = 0x00000040,
			Normal = 0x00000080,
			Temporary = 0x00000100,
			SparseFile = 0x00000200,
			ReparsePoint = 0x00000400,
			Compressed = 0x00000800,
			Offline = 0x00001000,
			NotContentIndexed = 0x00002000,
			Encrypted = 0x00004000,
			Write_Through = 0x80000000,
			Overlapped = 0x40000000,
			NoBuffering = 0x20000000,
			RandomAccess = 0x10000000,
			SequentialScan = 0x08000000,
			DeleteOnClose = 0x04000000,
			BackupSemantics = 0x02000000,
			PosixSemantics = 0x01000000,
			OpenReparsePoint = 0x00200000,
			OpenNoRecall = 0x00100000,
			FirstPipeInstance = 0x00080000
		}

		[DllImport("api-ms-win-core-handle-l1-1-0.dll")]
		public static extern bool CloseHandle(
			IntPtr hObject
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

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct REPARSE_DATA_BUFFER
		{
			public uint ReparseTag;
			public short ReparseDataLength;
			public short Reserved;
			public short SubsNameOffset;
			public short SubsNameLength;
			public short PrintNameOffset;
			public short PrintNameLength;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXIMUM_REPARSE_DATA_BUFFER_SIZE)]
			public char[] PathBuffer;
		}

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
		public static extern bool MoveFileFromApp(
			string lpExistingFileName,
			string lpNewFileName
		);

		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Auto,
		CallingConvention = CallingConvention.StdCall,
		SetLastError = true)]
		public static extern bool CopyFileFromApp(
			string lpExistingFileName,
			string lpNewFileName,
			bool bFailIfExists
		);

		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", CharSet = CharSet.Auto,
		CallingConvention = CallingConvention.StdCall,
		SetLastError = true)]
		public static extern bool DeleteFileFromApp(
			string lpFileName
		);

		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetFileAttributesExFromApp(
			string lpFileName,
			GET_FILEEX_INFO_LEVELS fInfoLevelId,
			out WIN32_FILE_ATTRIBUTE_DATA lpFileInformation
		);

		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetFileAttributesFromApp(
			string lpFileName,
			FileAttributes dwFileAttributes
		);

		[DllImport("api-ms-win-core-file-l1-2-1.dll", CharSet = CharSet.Auto,
		CallingConvention = CallingConvention.StdCall,
		SetLastError = true)]
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
			uint dwNumberOfBytesTransferred,
			ref NativeOverlapped lpOverlapped);

		public enum GET_FILEEX_INFO_LEVELS
		{
			GetFileExInfoStandard,
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct WIN32_FILE_ATTRIBUTE_DATA
		{
			public FileAttributes dwFileAttributes;
			public FILETIME ftCreationTime;
			public FILETIME ftLastAccessTime;
			public FILETIME ftLastWriteTime;
			public uint nFileSizeHigh;
			public uint nFileSizeLow;
		}

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

		public enum FILE_INFO_BY_HANDLE_CLASS
		{
			FileBasicInfo = 0,
			FileStandardInfo = 1,
			FileNameInfo = 2,
			FileRenameInfo = 3,
			FileDispositionInfo = 4,
			FileAllocationInfo = 5,
			FileEndOfFileInfo = 6,
			FileStreamInfo = 7,
			FileCompressionInfo = 8,
			FileAttributeTagInfo = 9,
			FileIdBothDirectoryInfo = 10,// 0x0A
			FileIdBothDirectoryRestartInfo = 11, // 0xB
			FileIoPriorityHintInfo = 12, // 0xC
			FileRemoteProtocolInfo = 13, // 0xD
			FileFullDirectoryInfo = 14, // 0xE
			FileFullDirectoryRestartInfo = 15, // 0xF
			FileStorageInfo = 16, // 0x10
			FileAlignmentInfo = 17, // 0x11
			FileIdInfo = 18, // 0x12
			FileIdExtdDirectoryInfo = 19, // 0x13
			FileIdExtdDirectoryRestartInfo = 20, // 0x14
			MaximumFileInfoByHandlesClass
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct FILE_ID_BOTH_DIR_INFO
		{
			public uint NextEntryOffset;
			public uint FileIndex;
			public long CreationTime;
			public long LastAccessTime;
			public long LastWriteTime;
			public long ChangeTime;
			public long EndOfFile;
			public long AllocationSize;
			public uint FileAttributes;
			public uint FileNameLength;
			public uint EaSize;
			public char ShortNameLength;
			[MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 12)]
			public string ShortName;
			public long FileId;
			[MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 1)]
			public string FileName;
		}

		[DllImport("api-ms-win-core-file-l2-1-1.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern bool GetFileInformationByHandleEx(
			IntPtr hFile,
			FILE_INFO_BY_HANDLE_CLASS infoClass,
			out FILE_ID_BOTH_DIR_INFO dirInfo,
			uint dwBufferSize
		);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
		public struct FILE_STREAM_INFO
		{
			public uint NextEntryOffset;
			public uint StreamNameLength;
			public long StreamSize;
			public long StreamAllocationSize;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
			public string StreamName;
		}

		[DllImport("api-ms-win-core-file-l2-1-1.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		public static extern bool GetFileInformationByHandleEx(
			IntPtr hFile,
			FILE_INFO_BY_HANDLE_CLASS infoClass,
			IntPtr dirInfo,
			uint dwBufferSize
		);
	}
}
