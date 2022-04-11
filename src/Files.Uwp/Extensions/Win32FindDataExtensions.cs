using static Files.Uwp.Helpers.NativeFindStorageItemHelper;

namespace Files.Uwp.Extensions
{
    public static class Win32FindDataExtensions
    {
        private const long MAXDWORD = 4294967295;

        public static long GetSize(this WIN32_FIND_DATA findData)
        {
            long fDataFSize = findData.nFileSizeLow;
            long fileSize;
            if (fDataFSize < 0 && findData.nFileSizeHigh > 0)
            {
                fileSize = fDataFSize + (MAXDWORD + 1) + (findData.nFileSizeHigh * (MAXDWORD + 1));
            }
            else
            {
                if (findData.nFileSizeHigh > 0)
                {
                    fileSize = fDataFSize + (findData.nFileSizeHigh * (MAXDWORD + 1));
                }
                else if (fDataFSize < 0)
                {
                    fileSize = fDataFSize + (MAXDWORD + 1);
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