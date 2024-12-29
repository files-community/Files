// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

namespace Files.App.Extensions
{
	public static class Win32Extensions
	{
		public static bool IsHandleInvalid(this IntPtr handle)
		{
			return handle == IntPtr.Zero || handle.ToInt64() == -1;
		}
	}
}
