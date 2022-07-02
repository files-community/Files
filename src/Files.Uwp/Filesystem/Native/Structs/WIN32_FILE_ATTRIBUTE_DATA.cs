using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Windows.Storage;

namespace Files.Uwp.Filesystem.Native
{
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
}
