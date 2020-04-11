using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Files.Helpers
{
    internal static class SafeNativeMethods
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern int StrCmpLogicalW(string psz1, string psz2);
    }

    public class NaturalStringComparer : IComparer<object>
    {
        public int Compare(object a, object b)
        {
            return SafeNativeMethods.StrCmpLogicalW(a.ToString(), b.ToString());
        }
    }
}
