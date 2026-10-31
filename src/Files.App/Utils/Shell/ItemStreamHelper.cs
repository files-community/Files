// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace Files.App.Utils.Shell
{
	public static class ItemStreamHelper
	{
		public static IShellItem? IShellItemFromPath(string path)
		{
			var hr = PInvoke.SHCreateItemFromParsingName(path, null!, out IShellItem psi);
			return hr.Failed ? null : psi;
		}

		public static IStream? IStreamFromPath(string path)
		{
			var hr = PInvoke.SHCreateStreamOnFileEx(
				path,
				(uint)(STGM.STGM_READ | STGM.STGM_FAILIFTHERE | STGM.STGM_SHARE_DENY_NONE),
				0,
				false,
				null,
				out IStream pstm);

			return hr.Failed ? null : pstm;
		}
	}
}
