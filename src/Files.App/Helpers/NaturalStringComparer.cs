// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Helpers
{
	public sealed class NaturalStringComparer
	{
		public static IComparer<object> GetForProcessor()
		{
			return Win32Helper.IsRunningOnArm ? new StringComparerArm64() : new StringComparerDefault();
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
				return Win32PInvoke.CompareStringEx(
					Win32PInvoke.LOCALE_NAME_USER_DEFAULT,
					Win32PInvoke.SORT_DIGITSASNUMBERS, // Add other flags if required.
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
