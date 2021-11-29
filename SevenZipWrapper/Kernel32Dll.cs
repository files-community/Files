using System;
using System.Runtime.InteropServices;
using System.Security;

namespace SevenZipExtractor
{
    internal static class Kernel32Dll
    {
        [DllImport("api-ms-win-core-libraryloader-l2-1-0.dll", SetLastError = true)]
        public static extern SafeLibraryHandle LoadPackagedLibrary([MarshalAs(UnmanagedType.LPWStr)] string libraryName, int reserved = 0);

        [DllImport("api-ms-win-core-libraryloader-l1-2-0.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        internal static extern IntPtr GetProcAddress(SafeLibraryHandle hModule, [MarshalAs(UnmanagedType.LPStr)] string procName);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("api-ms-win-core-libraryloader-l1-2-0.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary(IntPtr hModule);
    }
}