// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
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

		[DllImport("shell32.dll", EntryPoint = "SHUpdateRecycleBinIcon", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern void SHUpdateRecycleBinIcon();
	}

	namespace Extras
	{
		[GeneratedComInterface, Guid("EACDD04C-117E-4E17-88F4-D1B12B0E3D89"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		public partial interface IDCompositionTarget
		{
			[PreserveSig]
			int SetRoot(nint visual);
		}
	}
}
