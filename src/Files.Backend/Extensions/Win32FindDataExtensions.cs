// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using static Files.Backend.Helpers.NativeFindStorageItemHelper;

namespace Files.Backend.Extensions
{
	public static class Win32FindDataExtensions
	{
		private const long MAXDWORD = 4294967295;

		public static long GetSize(this WIN32_FIND_DATA findData)
		{
			long fDataFSize = findData.nFileSizeLow;

			return fDataFSize
				+ (fDataFSize < 0 ? MAXDWORD + 1 : 0)
				+ (findData.nFileSizeHigh > 0 ? findData.nFileSizeHigh * (MAXDWORD + 1) : 0)
			;
		}
	}
}
