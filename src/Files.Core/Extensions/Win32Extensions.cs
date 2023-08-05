// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Extensions
{
	public static class Win32Extensions
	{
		public static bool IsHandleInvalid(this IntPtr handle)
		{
			return handle == IntPtr.Zero || handle.ToInt64() == -1;
		}
	}
}
