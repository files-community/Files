using static Files.Core.Helpers.NativeFindStorageItemHelper;

namespace Files.Core.Extensions
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
