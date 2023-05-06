// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;

namespace Files.App.Utils
{
	internal static class SafeNativeMethods
	{
		public static readonly int NORM_IGNORECASE = 0x00000001;
		public static readonly int NORM_IGNORENONSPACE = 0x00000002;
		public static readonly int NORM_IGNORESYMBOLS = 0x00000004;
		public static readonly int LINGUISTIC_IGNORECASE = 0x00000010;
		public static readonly int LINGUISTIC_IGNOREDIACRITIC = 0x00000020;
		public static readonly int NORM_IGNOREKANATYPE = 0x00010000;
		public static readonly int NORM_IGNOREWIDTH = 0x00020000;
		public static readonly int NORM_LINGUISTIC_CASING = 0x08000000;
		public static readonly int SORT_STRINGSORT = 0x00001000;
		public static readonly int SORT_DIGITSASNUMBERS = 0x00000008;

		public static readonly string LOCALE_NAME_USER_DEFAULT = null;
		public static readonly string LOCALE_NAME_INVARIANT = string.Empty;
		public static readonly string LOCALE_NAME_SYSTEM_DEFAULT = "!sys-default-locale";

		[DllImport("api-ms-win-core-string-l1-1-0.dll", CharSet = CharSet.Unicode)]
		public static extern int ComparestringEx(
		  string localeName,
		  int flags,
		  string str1,
		  int count1,
		  string str2,
		  int count2,
		  IntPtr versionInformation,
		  IntPtr reserved,
		  int param);
	}

	public class NaturalStringComparer
	{
		public static IComparer<object> GetForProcessor()
			=> NativeWinApiHelper.IsRunningOnArm
				? new StringComparerArm64()
				: new StringComparerDefault();

		private class StringComparerArm64 : IComparer<object>
		{
			public int Compare(object a, object b)
				=> StringComparer.CurrentCulture.Compare(a, b);
		}

		private class StringComparerDefault : IComparer<object>
		{
			public int Compare(object a, object b)
			{
				return SafeNativeMethods.ComparestringEx(
					SafeNativeMethods.LOCALE_NAME_USER_DEFAULT,
					SafeNativeMethods.SORT_DIGITSASNUMBERS, // Add other flags if required.
					a?.ToString(),
					a?.ToString().Length ?? 0,
					b?.ToString(),
					b?.ToString().Length ?? 0,
					IntPtr.Zero,
					IntPtr.Zero,
					0) - 2;
			}
		}
	}
}
