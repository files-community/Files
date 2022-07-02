using System.Runtime.InteropServices;
using static Files.Uwp.Filesystem.Native.NativeConstants;

namespace Files.Uwp.Filesystem.Native
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct REPARSE_DATA_BUFFER
    {
        public uint ReparseTag;
        public short ReparseDataLength;
        public short Reserved;
        public short SubsNameOffset;
        public short SubsNameLength;
        public short PrintNameOffset;
        public short PrintNameLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAXIMUM_REPARSE_DATA_BUFFER_SIZE)]
        public char[] PathBuffer;
    }
}
