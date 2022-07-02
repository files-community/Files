using System.Runtime.InteropServices;

namespace Files.Uwp.Filesystem.Native
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
    public struct FILE_STREAM_INFO
    {
        public uint NextEntryOffset;
        public uint StreamNameLength;
        public long StreamSize;
        public long StreamAllocationSize;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
        public string StreamName;
    }
}
