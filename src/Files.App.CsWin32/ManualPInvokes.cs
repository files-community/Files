// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.System.Variant;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Windows.Win32
{
	public unsafe static partial class PInvoke
	{
		public static HRESULT InitPropVariantFromString(char* psz, PROPVARIANT* ppropvar)
		{
			HRESULT hr = psz != null ? HRESULT.S_OK : HRESULT.E_INVALIDARG;

			if (SUCCEEDED(hr))
			{
				nuint byteCount = (nuint)((MemoryMarshal.CreateReadOnlySpanFromNullTerminated(psz).Length + 1) * 2);

				((ppropvar)->Anonymous.Anonymous.Anonymous.pwszVal) = (char*)(PInvoke.CoTaskMemAlloc(byteCount));
				hr = ((ppropvar)->Anonymous.Anonymous.Anonymous.pwszVal) != null ? HRESULT.S_OK : HRESULT.E_OUTOFMEMORY;
				if (SUCCEEDED(hr))
				{
					NativeMemory.Copy(psz, ((ppropvar)->Anonymous.Anonymous.Anonymous.pwszVal), unchecked(byteCount));
					((ppropvar)->Anonymous.Anonymous.vt) = VARENUM.VT_LPWSTR;
				}
			}

			if (FAILED(hr))
			{
				PInvoke.PropVariantInit(ppropvar);
			}

			return hr;
		}

		public static void PropVariantInit(PROPVARIANT* pvar)
		{
			NativeMemory.Fill(pvar, (uint)(sizeof(PROPVARIANT)), 0);
		}

		public static unsafe nint SetWindowLongPtr(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, nint dwNewLong)
		{
			// NOTE:
			//  Since CsWin32 generates SetWindowLong only on x86 and SetWindowLongPtr only on x64,
			//  we need to manually define both functions here.
			//  For more info, visit https://github.com/microsoft/CsWin32/issues/882
			return sizeof(nint) is 4
				? _SetWindowLong(hWnd, (int)nIndex, (int)dwNewLong)
				: _SetWindowLongPtr(hWnd, (int)nIndex, dwNewLong);

			[DllImport("User32", EntryPoint = "SetWindowLongW", ExactSpelling = true)]
			static extern int _SetWindowLong(HWND hWnd, int nIndex, int dwNewLong);

			[DllImport("User32", EntryPoint = "SetWindowLongPtrW", ExactSpelling = true)]
			static extern nint _SetWindowLongPtr(HWND hWnd, int nIndex, nint dwNewLong);
		}

		[LibraryImport("Shell32.dll", EntryPoint = "SHUpdateRecycleBinIcon")]
		public static partial void SHUpdateRecycleBinIcon();
	}
}
