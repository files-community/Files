using System.Runtime.InteropServices;

namespace Files.Uwp.Filesystem.Native
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct FILE_ID_BOTH_DIR_INFO
    {
        public uint NextEntryOffset;
        public uint FileIndex;
        public long CreationTime;
        public long LastAccessTime;
        public long LastWriteTime;
        public long ChangeTime;
        public long EndOfFile;
        public long AllocationSize;
        public uint FileAttributes;
        public uint FileNameLength;
        public uint EaSize;
        public char ShortNameLength;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 12)]
        public string ShortName;
        public long FileId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1)]
        public string FileName;
    }
}
