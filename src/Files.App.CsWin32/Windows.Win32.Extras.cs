// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Windows.Win32
{
	namespace Graphics.Gdi
	{
		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		public unsafe delegate BOOL MONITORENUMPROC([In] HMONITOR param0, [In] HDC param1, [In][Out] RECT* param2, [In] LPARAM param3);
	}

	namespace UI.WindowsAndMessaging
	{
		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		public delegate LRESULT WNDPROC(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam);
	}

	public static partial class PInvoke
	{
		[DllImport("User32", EntryPoint = "SetWindowLongW", ExactSpelling = true)]
		static extern int _SetWindowLong(HWND hWnd, int nIndex, int dwNewLong);

		[DllImport("User32", EntryPoint = "SetWindowLongPtrW", ExactSpelling = true)]
		static extern nint _SetWindowLongPtr(HWND hWnd, int nIndex, nint dwNewLong);

		// NOTE:
		//  CsWin32 doesn't generate SetWindowLong on other than x86 and vice versa.
		//  For more info, visit https://github.com/microsoft/CsWin32/issues/882
		public static unsafe nint SetWindowLongPtr(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, nint dwNewLong)
		{
			return sizeof(nint) is 4
				? (nint)_SetWindowLong(hWnd, (int)nIndex, (int)dwNewLong)
				: _SetWindowLongPtr(hWnd, (int)nIndex, dwNewLong);
		}
	}
}
