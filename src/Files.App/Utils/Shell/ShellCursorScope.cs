// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Puts the visible cursor back after a shell menu call on a worker thread. Shell extensions reached
	/// through QueryContextMenu/HandleMenuMsg set a wait or arrow cursor via SetCursor on the calling
	/// thread, and a stationary pointer receives no WM_SETCURSOR afterwards, so whatever cursor they
	/// leave behind stays on screen until the mouse moves.
	/// </summary>
	internal readonly struct ShellCursorScope : IDisposable
	{
		private readonly CURSORINFO capturedInfo;

		private ShellCursorScope(CURSORINFO info)
		{
			capturedInfo = info;
		}

		public static unsafe ShellCursorScope Capture()
		{
			CURSORINFO info = new() { cbSize = (uint)sizeof(CURSORINFO) };
			if (!PInvoke.GetCursorInfo(ref info))
				info = default;

			return new(info);
		}

		public unsafe void Dispose()
		{
			// Nothing to restore when the capture failed or the cursor was hidden/suppressed (touch input)
			if (capturedInfo.cbSize is 0 || capturedInfo.flags is not CURSORINFO_FLAGS.CURSOR_SHOWING || capturedInfo.hCursor.IsNull)
				return;

			CURSORINFO current = new() { cbSize = (uint)sizeof(CURSORINFO) };
			if (!PInvoke.GetCursorInfo(ref current))
				return;

			// A moved pointer has already re-resolved its cursor through WM_SETCURSOR
			if (current.ptScreenPos != capturedInfo.ptScreenPos)
				return;

			if (current.flags is CURSORINFO_FLAGS.CURSOR_SHOWING && current.hCursor != capturedInfo.hCursor)
				PInvoke.SetCursor(capturedInfo.hCursor);
		}
	}
}
