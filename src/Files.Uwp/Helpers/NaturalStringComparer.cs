using System;
using System.Collections.Generic;
using static Files.Uwp.Filesystem.Native.NativeConstants;
using static Files.Uwp.Filesystem.Native.NativeHelpers;

namespace Files.Uwp.Helpers
{
    public class NaturalStringComparer
    {
        public static IComparer<object> GetForProcessor()
            => IsRunningOnArm ? new StringComparerArm64() : new StringComparerDefault();

        private class StringComparerArm64 : IComparer<object>
        {
            public int Compare(object a, object b) => StringComparer.CurrentCulture.Compare(a, b);
        }

        private class StringComparerDefault : IComparer<object>
        {
            public int Compare(object a, object b) => CompareStringEx(
                LOCALE_NAME_USER_DEFAULT,
                SORT_DIGITSASNUMBERS, // Add other flags if required.
                a?.ToString(),
                a?.ToString().Length ?? 0,
                b?.ToString(),
                b?.ToString().Length ?? 0,
                IntPtr.Zero,
                IntPtr.Zero,
                0
            ) - 2;
        }
    }
}