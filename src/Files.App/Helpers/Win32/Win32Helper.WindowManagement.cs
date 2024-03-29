// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using System.Reflection;
using Vanara.PInvoke;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;
using static Vanara.PInvoke.User32;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides static helper for Win32.
	/// </summary>
	public static partial class Win32Helper
	{
		/// <summary>
		/// Brings the app window to foreground.
		/// </summary>
		/// <remarks>
		/// For more information, visit
		/// <br/>
		/// - <a href="https://stackoverflow.com/questions/1544179/what-are-the-differences-between-bringwindowtotop-setforegroundwindow-setwindo" />
		/// <br/>
		/// - <a href="https://stackoverflow.com/questions/916259/win32-bring-a-window-to-top" />
		/// </remarks>
		/// <param name="hWnd">The window handle to bring.</param>
		public static unsafe void BringToForegroundEx(Windows.Win32.Foundation.HWND hWnd)
		{
			var hCurWnd = PInvoke.GetForegroundWindow();
			var dwMyID = PInvoke.GetCurrentThreadId();
			var dwCurID = PInvoke.GetWindowThreadProcessId(hCurWnd);

			PInvoke.AttachThreadInput(dwCurID, dwMyID, true);

			PInvoke.SetWindowPos(hWnd, (Windows.Win32.Foundation.HWND)(-1), 0, 0, 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE);
			PInvoke.SetWindowPos(hWnd, (Windows.Win32.Foundation.HWND)(-2), 0, 0, 0, 0, SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE);
			PInvoke.SetForegroundWindow(hWnd);
			PInvoke.SetFocus(hWnd);
			PInvoke.SetActiveWindow(hWnd);
			PInvoke.AttachThreadInput(dwCurID, dwMyID, false);
		}

		/// <summary>
		/// Sets cursor when hovering on a specific element.
		/// </summary>
		/// <param name="uiElement">An element to be changed.</param>
		/// <param name="cursor">Cursor to change.</param>
		public static void ChangeCursor(this UIElement uiElement, InputCursor cursor)
		{
			Type type = typeof(UIElement);

			type.InvokeMember(
				"ProtectedCursor",
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance,
				null,
				uiElement,
				[cursor]
			);
		}

		/// <summary>
		/// Changes an attribute of the specified window.
		/// </summary>
		/// <param name="hWnd">A handle to the window and, indirectly, the class to which the window belongs.</param>
		/// <param name="nIndex">The zero-based offset to the value to be set.</param>
		/// <param name="dwNewLong">The replacement value.</param>
		/// <returns>If the function succeeds, the return value is the previous value of the specified offset.</returns>
		public static IntPtr SetWindowLong(HWND hWnd, WindowLongFlags nIndex, IntPtr dwNewLong)
		{
			return
				IntPtr.Size == 4
					? Win32PInvoke.SetWindowLongPtr32(hWnd, nIndex, dwNewLong)
					: Win32PInvoke.SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
		}
	}
}
