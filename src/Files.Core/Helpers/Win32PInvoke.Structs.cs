﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Files.Core.Helpers
{
	public static partial class Win32PInvoke
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int X;
			public int Y;
			public POINT(int x, int y) => (X, Y) = (x, y);
		}

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

		public unsafe struct OVERLAPPED
		{
			public IntPtr Internal;
			public IntPtr InternalHigh;
			public Union PointerAndOffset;
			public IntPtr hEvent;

			[StructLayout(LayoutKind.Explicit)]
			public struct Union
			{
				[FieldOffset(0)] public void* IntPtr;
				[FieldOffset(0)] public OffsetPair Offset;

				public struct OffsetPair { public uint Offset; public uint OffsetHigh; }
			}
		}

		public unsafe struct FILE_NOTIFY_INFORMATION
		{
			public uint NextEntryOffset;
			public uint Action;
			public uint FileNameLength;
			public fixed char FileName[1];
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SYSTEMTIME
		{
			[MarshalAs(UnmanagedType.U2)] public short Year;
			[MarshalAs(UnmanagedType.U2)] public short Month;
			[MarshalAs(UnmanagedType.U2)] public short DayOfWeek;
			[MarshalAs(UnmanagedType.U2)] public short Day;
			[MarshalAs(UnmanagedType.U2)] public short Hour;
			[MarshalAs(UnmanagedType.U2)] public short Minute;
			[MarshalAs(UnmanagedType.U2)] public short Second;
			[MarshalAs(UnmanagedType.U2)] public short Milliseconds;

			public SYSTEMTIME(DateTime dt)
			{
				dt = dt.ToUniversalTime(); // SetSystemTime expects the SYSTEMTIME in UTC
				Year = (short)dt.Year;
				Month = (short)dt.Month;
				DayOfWeek = (short)dt.DayOfWeek;
				Day = (short)dt.Day;
				Hour = (short)dt.Hour;
				Minute = (short)dt.Minute;
				Second = (short)dt.Second;
				Milliseconds = (short)dt.Millisecond;
			}

			public DateTime ToDateTime()
			{
				return new(Year, Month, Day, Hour, Minute, Second, Milliseconds, DateTimeKind.Utc);
			}
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct WIN32_FIND_DATA
		{
			public uint dwFileAttributes;

			public FILETIME ftCreationTime;
			public FILETIME ftLastAccessTime;
			public FILETIME ftLastWriteTime;

			public uint nFileSizeHigh;
			public uint nFileSizeLow;
			public uint dwReserved0;
			public uint dwReserved1;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string cFileName;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
			public string cAlternateFileName;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RM_UNIQUE_PROCESS
		{
			public int dwProcessId;
			public FILETIME ProcessStartTime;
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

		// There is usually no need to define Win32 COM interfaces/P-Invoke methods here.
		// The Vanara library contains the definitions for all members of Shell32.dll, User32.dll and more
		// The ones below are due to bugs in the current version of the library and can be removed once fixed
		// Structure used by SHQueryRecycleBin.
		[StructLayout(LayoutKind.Sequential, Pack = 0)]
		public struct SHQUERYRBINFO
		{
			public int cbSize;

			public long i64Size;

			public long i64NumItems;
		}
	}
}
