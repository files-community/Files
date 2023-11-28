// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils
{
	public class NaturalStringComparer
	{
		public static IComparer<object> GetForProcessor()
		{
			return Win32InteropHelper.IsRunningOnArm ? new StringComparerArm64() : new StringComparerDefault();
		}

		private class StringComparerArm64 : IComparer<object>
		{
			public int Compare(object a, object b)
			{
				return StringComparer.CurrentCulture.Compare(a, b);
			}
		}

		private class StringComparerDefault : IComparer<object>
		{
			public int Compare(object a, object b)
			{
				return Win32Interop.CompareStringEx(
					Win32Interop.LOCALE_NAME_USER_DEFAULT,
					Win32Interop.SORT_DIGITSASNUMBERS, // Add other flags if required.
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
