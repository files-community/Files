using System;
using System.Runtime.InteropServices;

namespace Files.Uwp.Filesystem.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SID_AND_ATTRIBUTES
    {
        public IntPtr Sid;

        public uint Attributes;
    }
}
