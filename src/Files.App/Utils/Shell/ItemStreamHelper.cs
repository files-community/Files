// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.System.Com;

namespace Files.App.Utils.Shell
{
	public static class ItemStreamHelper
	{
		static readonly Guid IShellItemIid = Guid.ParseExact("43826d1e-e718-42ee-bc55-a1e261c37bfe", "d");

		public static unsafe IntPtr IShellItemFromPath(string path)
		{
			void* psi;
			Guid iid = IShellItemIid;
			var hr = PInvoke.SHCreateItemFromParsingName(path, null, ref iid, out psi);
			if ((int)hr < 0)
				return IntPtr.Zero;
			return (IntPtr)psi;
		}

		public static unsafe IntPtr IStreamFromPath(string path)
		{
			IStream* pstm;
			var hr = PInvoke.SHCreateStreamOnFileEx(
				path,
				(uint)(STGM.STGM_READ | STGM.STGM_FAILIFTHERE | STGM.STGM_SHARE_DENY_NONE),
				0,
				false,
				null,
				&pstm);

			if ((int)hr < 0)
				return IntPtr.Zero;

			return (IntPtr)pstm;
		}

		public static void ReleaseObject(IntPtr obj)
		{
			Marshal.Release(obj);
		}
	}
}
