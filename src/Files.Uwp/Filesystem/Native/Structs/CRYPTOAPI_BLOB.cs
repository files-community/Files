using System;
using System.Runtime.InteropServices;

namespace Files.Uwp.Filesystem.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CRYPTOAPI_BLOB
    {
        public uint cbData;

        public IntPtr pbData;
    }
}
