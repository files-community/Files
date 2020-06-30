using System;
using static Files.Helpers.NativeFindStorageItemHelper;

namespace Files.Helpers
{
    public static class Win32FindDataExtensions
    {
        public static long GetSize(this WIN32_FIND_DATA findData)
        {
            long fDataFSize = findData.nFileSizeLow;
            long fileSize;
            if (fDataFSize < 0 && findData.nFileSizeHigh > 0)
            {
                fileSize = fDataFSize + 4294967296 + (findData.nFileSizeHigh * 4294967296);
            }
            else
            {
                if (findData.nFileSizeHigh > 0)
                {
                    fileSize = fDataFSize + (findData.nFileSizeHigh * 4294967296);
                }
                else if (fDataFSize < 0)
                {
                    fileSize = fDataFSize + 4294967296;
                }
                else
                {
                    fileSize = fDataFSize;
                }
            }

            return fileSize;
        }
    }
}