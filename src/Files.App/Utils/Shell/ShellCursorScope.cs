// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Windows.Win32;
using Windows.Win32.Foundation;
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
		// Shell menu calls run on several pooled worker threads and can overlap, and the displayed cursor is
		// shared process-wide. A nested scope must not snapshot the wait cursor an earlier scope is still
		// showing, so only the outermost scope records the baseline and only the last one out restores it.
		private static readonly Lock gate = new();
		private static int activeCount;
		private static CURSORINFO baseline;
		private static HWND baselineWindow;

		public static unsafe ShellCursorScope Capture()
		{
			lock (gate)
			{
				if (activeCount == 0)
				{
					CURSORINFO info = new() { cbSize = (uint)sizeof(CURSORINFO) };
					baseline = PInvoke.GetCursorInfo(ref info) ? info : default;
					baselineWindow = baseline.cbSize is 0 ? default : PInvoke.WindowFromPoint(baseline.ptScreenPos);
				}

				activeCount++;
			}

			return default;
		}

		public void Dispose()
		{
			lock (gate)
			{
				if (--activeCount == 0)
					Restore();
			}
		}

		private static unsafe void Restore()
		{
			// Nothing to restore when the capture failed or the cursor was hidden/suppressed (touch input)
			if (baseline.cbSize is 0 || baseline.flags is not CURSORINFO_FLAGS.CURSOR_SHOWING || baseline.hCursor.IsNull)
				return;

			CURSORINFO current = new() { cbSize = (uint)sizeof(CURSORINFO) };
			if (!PInvoke.GetCursorInfo(ref current))
				return;

			// A moved pointer has already re-resolved its cursor through WM_SETCURSOR
			if (current.ptScreenPos != baseline.ptScreenPos)
				return;

			// Focus may have moved to another app (e.g. Alt+Tab) while the pointer stayed put; SetCursor is
			// shared state, so only reassert our cursor while the same Files window is still under the pointer.
			if (baselineWindow.IsNull || PInvoke.WindowFromPoint(current.ptScreenPos) != baselineWindow || !IsOwnedByCurrentProcess(baselineWindow))
				return;

			if (current.flags is CURSORINFO_FLAGS.CURSOR_SHOWING && current.hCursor != baseline.hCursor)
				PInvoke.SetCursor(baseline.hCursor);
		}

		private static bool IsOwnedByCurrentProcess(HWND hwnd)
		{
			_ = PInvoke.GetWindowThreadProcessId(hwnd, out uint processId);
			return processId == (uint)Environment.ProcessId;
		}
	}
}
