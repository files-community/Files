// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace Files.SearchService.Usn;

/// <summary>
/// P/Invoke declarations for NTFS USN Change Journal access.
/// All structures match the Windows SDK definitions for USN_RECORD_V2
/// and MFT_ENUM_DATA_V0 used by FSCTL_ENUM_USN_DATA.
/// </summary>
internal static partial class NativeMethods
{
	// ---- IOCTL codes -------------------------------------------------------

	internal const uint FSCTL_ENUM_USN_DATA    = 0x900B3;
	internal const uint FSCTL_READ_USN_JOURNAL  = 0x900BB;
	internal const uint FSCTL_QUERY_USN_JOURNAL = 0x900F4;

	// ---- File attribute flags ----------------------------------------------

	internal const uint FILE_ATTRIBUTE_DIRECTORY    = 0x10;
	internal const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x400;

	// ---- USN reason flags (live watcher) -----------------------------------

	internal const uint USN_REASON_FILE_CREATE    = 0x00000100;
	internal const uint USN_REASON_FILE_DELETE    = 0x00000200;
	internal const uint USN_REASON_RENAME_NEW_NAME = 0x00002000;
	internal const uint USN_REASON_RENAME_OLD_NAME = 0x00001000;
	internal const uint USN_REASON_DATA_OVERWRITE  = 0x00000001;
	internal const uint USN_REASON_DATA_EXTEND     = 0x00000002;

	// ---- CreateFile constants ----------------------------------------------

	internal const uint GENERIC_READ              = 0x80000000;
	internal const uint FILE_SHARE_READ           = 0x00000001;
	internal const uint FILE_SHARE_WRITE          = 0x00000002;
	internal const uint OPEN_EXISTING             = 3;
	internal const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

	// ---- FRN masking -------------------------------------------------------
	// USN FileReferenceNumbers encode a sequence number in the high 16 bits.
	// GetFileInformationByHandle returns only the 48-bit MFT record number.
	// Mask when comparing USN FRNs to a handle-derived FRN.
	internal const ulong FRN_MFT_MASK = 0x0000_FFFF_FFFF_FFFF;

	// ---- Structs -----------------------------------------------------------

	[StructLayout(LayoutKind.Sequential)]
	internal struct MFT_ENUM_DATA_V0
	{
		public ulong StartFileReferenceNumber;
		public long  LowUsn;
		public long  HighUsn;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct USN_RECORD_V2
	{
		public uint   RecordLength;
		public ushort MajorVersion;
		public ushort MinorVersion;
		public ulong  FileReferenceNumber;
		public ulong  ParentFileReferenceNumber;
		public long   Usn;
		public long   TimeStamp;
		public uint   Reason;
		public uint   SourceInfo;
		public uint   SecurityId;
		public uint   FileAttributes;
		public ushort FileNameLength;
		public ushort FileNameOffset;
		// FileNameLength bytes of UTF-16LE filename immediately follow in the buffer.
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct USN_JOURNAL_DATA_V0
	{
		public ulong UsnJournalID;
		public long  FirstUsn;
		public long  NextUsn;
		public long  LowestValidUsn;
		public long  MaxUsn;
		public ulong MaximumSize;
		public ulong AllocationDelta;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct READ_USN_JOURNAL_DATA_V0
	{
		public long  StartUsn;
		public uint  ReasonMask;
		public uint  ReturnOnlyOnClose;
		public ulong Timeout;
		public ulong BytesToWaitFor;
		public ulong UsnJournalID;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct BY_HANDLE_FILE_INFORMATION
	{
		public uint   FileAttributes;
		public long   CreationTime;      // FILETIME as 64-bit int
		public long   LastAccessTime;
		public long   LastWriteTime;
		public uint   VolumeSerialNumber;
		public uint   FileSizeHigh;
		public uint   FileSizeLow;
		public uint   NumberOfLinks;
		public uint   FileIndexHigh;
		public uint   FileIndexLow;
	}

	// ---- P/Invoke ----------------------------------------------------------

	[LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
	internal static partial SafeFileHandle CreateFileW(
		string lpFileName,
		uint   dwDesiredAccess,
		uint   dwShareMode,
		nint   lpSecurityAttributes,
		uint   dwCreationDisposition,
		uint   dwFlagsAndAttributes,
		nint   hTemplateFile);

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static partial bool GetFileInformationByHandle(
		SafeHandle                   hFile,
		out BY_HANDLE_FILE_INFORMATION lpFileInformation);

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static partial bool DeviceIoControl(
		SafeHandle          hDevice,
		uint                dwIoControlCode,
		ref MFT_ENUM_DATA_V0 lpInBuffer,
		int                 nInBufferSize,
		byte[]              lpOutBuffer,
		int                 nOutBufferSize,
		out int             lpBytesReturned,
		nint                lpOverlapped);

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static partial bool DeviceIoControl(
		SafeHandle               hDevice,
		uint                     dwIoControlCode,
		ref READ_USN_JOURNAL_DATA_V0 lpInBuffer,
		int                      nInBufferSize,
		byte[]                   lpOutBuffer,
		int                      nOutBufferSize,
		out int                  lpBytesReturned,
		nint                     lpOverlapped);

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static partial bool DeviceIoControl(
		SafeHandle           hDevice,
		uint                 dwIoControlCode,
		nint                 lpInBuffer,
		int                  nInBufferSize,
		out USN_JOURNAL_DATA_V0 lpOutBuffer,
		int                  nOutBufferSize,
		out int              lpBytesReturned,
		nint                 lpOverlapped);
}
