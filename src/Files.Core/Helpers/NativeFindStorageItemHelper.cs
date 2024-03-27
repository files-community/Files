// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices.Marshalling;

namespace Files.Core.Helpers
{
	/// <summary>
	/// Provides a bunch of Win32API for native find storage items.
	/// </summary>
	public sealed partial class NativeFindStorageItemHelper
	{
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

		public enum FINDEX_INFO_LEVELS
		{
			FindExInfoStandard = 0,
			FindExInfoBasic = 1
		}

		public enum FINDEX_SEARCH_OPS
		{
			FindExSearchNameMatch = 0,
			FindExSearchLimitToDirectories = 1,
			FindExSearchLimitToDevices = 2
		}
		
		[LibraryImport("api-ms-win-core-file-fromapp-l1-1-0.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
		public static partial IntPtr FindFirstFileExFromApp(
			string lpFileName,
			FINDEX_INFO_LEVELS fInfoLevelId,
			out WIN32_FIND_DATA lpFindFileData,
			FINDEX_SEARCH_OPS fSearchOp,
			IntPtr lpSearchFilter,
			int dwAdditionalFlags);

		public const int FIND_FIRST_EX_CASE_SENSITIVE = 1;
		public const int FIND_FIRST_EX_LARGE_FETCH = 2;

		[LibraryImport("api-ms-win-core-file-l1-1-0.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static partial bool FindNextFile(
			IntPtr hFindFile,
			out WIN32_FIND_DATA lpFindFileData);

		[LibraryImport("api-ms-win-core-file-l1-1-0.dll")]
		[return:MarshalAs(UnmanagedType.I1)] 
		public static partial bool FindClose(
			IntPtr hFindFile);

		[LibraryImport("api-ms-win-core-timezone-l1-1-0.dll", SetLastError = true)]
		[return:MarshalAs(UnmanagedType.I1)] 
		public static partial bool FileTimeToSystemTime(
			ref FILETIME lpFileTime,
			out SYSTEMTIME lpSystemTime);

		public static bool GetWin32FindDataForPath(
			string targetPath,
			[MarshalUsing(typeof(Win32FindDataMarshaller))] out WIN32_FIND_DATA findData)
		{
			FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;

			int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;

			IntPtr hFile = FindFirstFileExFromApp(
				targetPath,
				findInfoLevel,
				out findData,
				FINDEX_SEARCH_OPS.FindExSearchNameMatch,
				IntPtr.Zero,
				additionalFlags);
			
			if (hFile.ToInt64() != -1)
			{
				FindClose(hFile);

				return true;
			}

			return false;
		}
	}
}
