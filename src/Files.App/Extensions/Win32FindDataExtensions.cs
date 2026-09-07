// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;

namespace Files.App.Extensions
{
	public static class Win32FindDataExtensions
	{
		private const long MAX_DWORD = 4294967295;

		public static long GetSize(this Win32PInvoke.WIN32_FIND_DATA findData)
		{
			long fDataFSize = findData.nFileSizeLow;

			return
				fDataFSize +
				(fDataFSize < 0 ? MAX_DWORD + 1 : 0) +
				(findData.nFileSizeHigh > 0 ? findData.nFileSizeHigh * (MAX_DWORD + 1) : 0);
		}

		public static long GetSize(this WIN32_FIND_DATAW findData)
		{
			long fDataFSize = findData.nFileSizeLow;

			return
				fDataFSize +
				(fDataFSize < 0 ? MAX_DWORD + 1 : 0) +
				(findData.nFileSizeHigh > 0 ? findData.nFileSizeHigh * (MAX_DWORD + 1) : 0);
		}

		public static DateTime ToDateTime(this SYSTEMTIME value)
			=> new(value.wYear, value.wMonth, value.wDay, value.wHour, value.wMinute, value.wSecond, value.wMilliseconds, DateTimeKind.Utc);
	}
}
