using System;
using System.Runtime.InteropServices.ComTypes;
using static Files.Backend.Item.Tools.NativeFindStorageItemHelper;

namespace Files.Backend.Item.Tools
{
    internal static class Win32FindDataExtension
    {
        private const long MAXDWORD = 4294967295;

        public static DateTime ToDateTime(this ref FILETIME fileTime)
        {
            try
            {
                FileTimeToSystemTime(ref fileTime, out SYSTEMTIME systemCreatedTimeOutput);
                return systemCreatedTimeOutput.ToDateTime();
            }
            catch (Exception e)
            {
                throw new InvalidCastException("invalid DateTime", e);
            }
        }

        public static long GetSize(this WIN32_FIND_DATA findData)
        {
            long sizeLow = findData.nFileSizeLow;
            long sizeHigh = findData.nFileSizeHigh;

            return sizeLow + (sizeLow, sizeHigh) switch
            {
                ( < 0, > 0) => (MAXDWORD + 1) * (sizeHigh + 1),
                (_, > 0) => (MAXDWORD + 1) * sizeHigh,
                ( < 0, _) => MAXDWORD + 1,
                _ => 0,
            };
        }
    }
}
