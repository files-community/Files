// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Files.App.Helpers
{
	internal static class SafeNativeMethods
	{
		public const Int32 NORM_IGNORECASE = 0x00000001;
		public const Int32 NORM_IGNORENONSPACE = 0x00000002;
		public const Int32 NORM_IGNORESYMBOLS = 0x00000004;
		public const Int32 LINGUISTIC_IGNORECASE = 0x00000010;
		public const Int32 LINGUISTIC_IGNOREDIACRITIC = 0x00000020;
		public const Int32 NORM_IGNOREKANATYPE = 0x00010000;
		public const Int32 NORM_IGNOREWIDTH = 0x00020000;
		public const Int32 NORM_LINGUISTIC_CASING = 0x08000000;
		public const Int32 SORT_STRINGSORT = 0x00001000;
		public const Int32 SORT_DIGITSASNUMBERS = 0x00000008;

		public const String LOCALE_NAME_USER_DEFAULT = null;
		public const String LOCALE_NAME_INVARIANT = "";
		public const String LOCALE_NAME_SYSTEM_DEFAULT = "!sys-default-locale";

		[DllImport("api-ms-win-core-string-l1-1-0.dll", CharSet = CharSet.Unicode)]
		public static extern Int32 CompareStringEx(
		  String localeName,
		  Int32 flags,
		  String str1,
		  Int32 count1,
		  String str2,
		  Int32 count2,
		  IntPtr versionInformation,
		  IntPtr reserved,
		  Int32 param
		);
	}

	public sealed class NaturalStringComparer
	{
		public static IComparer<object> GetForProcessor()
		{
			return NativeWinApiHelper.IsRunningOnArm ? new StringComparerArm64() : new StringComparerDefault();
		}

		private sealed class StringComparerArm64 : IComparer<object>
		{
			public int Compare(object a, object b)
			{
				return StringComparer.CurrentCulture.Compare(a, b);
			}
		}

		private sealed class StringComparerDefault : IComparer<object>
		{
			public int Compare(object a, object b)
			{
				return SafeNativeMethods.CompareStringEx(
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