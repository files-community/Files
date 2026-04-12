// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Files.App.Controls
{
	/// <summary>
	/// Provides helper methods for classifying expected drag-and-drop COM failures
	/// caused by stale OLE drag payloads (e.g. from Windows Explorer).
	/// </summary>
	internal static class DragDropExceptionHelper
	{
		// CLIPBRD_E_CANT_OPEN / OLE_E_NOTRUNNING: clipboard/data object is no longer available
		private const int HRESULT_CLIPBOARD_DATA_UNAVAILABLE = unchecked((int)0x800401D0);

		// RPC_E_SERVERFAULT: OLE/RPC drag pipeline failure (stale cross-process drag)
		private const int HRESULT_RPC_OLE_FAILURE = unchecked((int)0x80010105);

		/// <summary>
		/// Returns <see langword="true"/> when <paramref name="ex"/> is a <see cref="COMException"/>
		/// with an HResult that indicates a stale or already-released OLE drag payload.
		/// These are expected during sidebar reorder when the user also has File Explorer open.
		/// </summary>
		public static bool IsExpectedStaleDragData(Exception ex)
		{
			return ex is COMException com &&
				   (com.HResult == HRESULT_CLIPBOARD_DATA_UNAVAILABLE ||
					com.HResult == HRESULT_RPC_OLE_FAILURE);
		}

		/// <summary>
		/// Writes a debug-level trace for a stale drag payload event.
		/// </summary>
		[Conditional("DEBUG")]
		public static void LogStaleDrag(Exception ex, string message)
		{
			Debug.WriteLine($"[DragDrop] {message} HResult=0x{ex.HResult:X8}");
		}
	}
}
