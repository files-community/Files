using System;

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
