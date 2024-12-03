// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

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

		/// <summary>Contains information about the size and position of a window.</summary>
		/// <remarks>
		/// <para><see href="https://learn.microsoft.com/windows/win32/api/winuser/ns-winuser-windowpos">Learn more about this API from docs.microsoft.com</see>.</para>
		/// </remarks>
		public partial struct WINDOWPOS
		{
			/// <summary>
			/// <para>Type: <b>HWND</b> A handle to the window.</para>
			/// <para><see href="https://learn.microsoft.com/windows/win32/api/winuser/ns-winuser-windowpos#members">Read more on docs.microsoft.com</see>.</para>
			/// </summary>
			internal HWND hwnd;

			/// <summary>
			/// <para>Type: <b>HWND</b> The position of the window in Z order (front-to-back position). This member can be a handle to the window behind which this window is placed, or can be one of the special values listed with the <a href="https://docs.microsoft.com/windows/desktop/api/winuser/nf-winuser-setwindowpos">SetWindowPos</a> function.</para>
			/// <para><see href="https://learn.microsoft.com/windows/win32/api/winuser/ns-winuser-windowpos#members">Read more on docs.microsoft.com</see>.</para>
			/// </summary>
			internal HWND hwndInsertAfter;

			/// <summary>
			/// <para>Type: <b>int</b> The position of the left edge of the window.</para>
			/// <para><see href="https://learn.microsoft.com/windows/win32/api/winuser/ns-winuser-windowpos#members">Read more on docs.microsoft.com</see>.</para>
			/// </summary>
			internal int x;

			/// <summary>
			/// <para>Type: <b>int</b> The position of the top edge of the window.</para>
			/// <para><see href="https://learn.microsoft.com/windows/win32/api/winuser/ns-winuser-windowpos#members">Read more on docs.microsoft.com</see>.</para>
			/// </summary>
			internal int y;

			/// <summary>
			/// <para>Type: <b>int</b> The window width, in pixels.</para>
			/// <para><see href="https://learn.microsoft.com/windows/win32/api/winuser/ns-winuser-windowpos#members">Read more on docs.microsoft.com</see>.</para>
			/// </summary>
			internal int cx;

			/// <summary>
			/// <para>Type: <b>int</b> The window height, in pixels.</para>
			/// <para><see href="https://learn.microsoft.com/windows/win32/api/winuser/ns-winuser-windowpos#members">Read more on docs.microsoft.com</see>.</para>
			/// </summary>
			internal int cy;

			/// <summary>Type: <b>UINT</b></summary>
			public SET_WINDOW_POS_FLAGS flags;
		}
	}
}
