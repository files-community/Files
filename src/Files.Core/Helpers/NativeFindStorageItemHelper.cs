using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Files.Core.Helpers
{
	public class NativeFindStorageItemHelper
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
				dt = dt.ToUniversalTime();  // SetSystemTime expects the SYSTEMTIME in UTC
				Year = (short)dt.Year;
				Month = (short)dt.Month;
				DayOfWeek = (short)dt.DayOfWeek;
				Day = (short)dt.Day;
				Hour = (short)dt.Hour;
				Minute = (short)dt.Minute;
				Second = (short)dt.Second;
				Milliseconds = (short)dt.Millisecond;
			}

			public DateTime ToDateTime() => new(Year, Month, Day, Hour, Minute, Second, Milliseconds, DateTimeKind.Utc);
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

		[DllImport("api-ms-win-core-file-fromapp-l1-1-0.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr FindFirstFileExFromApp(
			string lpFileName,
			FINDEX_INFO_LEVELS fInfoLevelId,
			out WIN32_FIND_DATA lpFindFileData,
			FINDEX_SEARCH_OPS fSearchOp,
			IntPtr lpSearchFilter,
			int dwAdditionalFlags);

		public const int FIND_FIRST_EX_CASE_SENSITIVE = 1;
		public const int FIND_FIRST_EX_LARGE_FETCH = 2;

		[DllImport("api-ms-win-core-file-l1-1-0.dll", CharSet = CharSet.Unicode)]
		public static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

		[DllImport("api-ms-win-core-file-l1-1-0.dll")]
		public static extern bool FindClose(IntPtr hFindFile);

		[DllImport("api-ms-win-core-timezone-l1-1-0.dll", SetLastError = true)]
		public static extern bool FileTimeToSystemTime(ref FILETIME lpFileTime, out SYSTEMTIME lpSystemTime);

		public static bool GetWin32FindDataForPath(string targetPath, out WIN32_FIND_DATA findData)
		{
			FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
			int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;

			IntPtr hFile = FindFirstFileExFromApp(targetPath, findInfoLevel, out findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, additionalFlags);
			if (hFile.ToInt64() != -1)
			{
				FindClose(hFile);
				return true;
			}
			return false;
		}
	}
}
