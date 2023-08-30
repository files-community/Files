using System;
using System.Runtime.InteropServices;
using Vanara.PInvoke;

namespace Files.App.Utils.Shell
{
	public static class ItemStreamHelper
	{
		[DllImport("shlwapi.dll", CallingConvention = CallingConvention.StdCall, PreserveSig = true, CharSet = CharSet.Unicode)]
		static extern HRESULT SHCreateStreamOnFileEx(string pszFile, STGM grfMode, uint dwAttributes, uint fCreate, IntPtr pstmTemplate, out IntPtr ppstm);

		[DllImport("shell32.dll", CallingConvention = CallingConvention.StdCall, PreserveSig = true, CharSet = CharSet.Unicode)]
		static extern HRESULT SHCreateItemFromParsingName(string pszPath, IntPtr pbc, ref Guid riid, out IntPtr ppv);

		static readonly Guid IShellItemIid = Guid.ParseExact("43826d1e-e718-42ee-bc55-a1e261c37bfe", "d");

		public static IntPtr IShellItemFromPath(string path)
		{
			IntPtr psi;
			Guid iid = IShellItemIid;
			var hr = SHCreateItemFromParsingName(path, IntPtr.Zero, ref iid, out psi);
			if ((int)hr < 0)
				return IntPtr.Zero;
			return psi;
		}

		public static IntPtr IStreamFromPath(string path)
		{
			IntPtr pstm;
			var hr = SHCreateStreamOnFileEx(path,
				STGM.STGM_READ | STGM.STGM_FAILIFTHERE | STGM.STGM_SHARE_DENY_NONE,
				0, 0, IntPtr.Zero, out pstm);
			if ((int)hr < 0)
				return IntPtr.Zero;
			return pstm;
		}

		public static void ReleaseObject(IntPtr obj)
		{
			Marshal.Release(obj);
		}
	}
}
