using System.Runtime.InteropServices;

namespace Files.Uwp.Filesystem.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TOKEN_USER
    {
        public SID_AND_ATTRIBUTES User;
    }
}
