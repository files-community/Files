﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

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
	}
}
