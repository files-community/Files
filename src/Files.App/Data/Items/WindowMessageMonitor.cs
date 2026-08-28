// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WNDPROC = Windows.Win32.Extras.ManagedWNDPROC;

namespace Files.App.Data.Items
{
	public readonly struct WindowMessage
	{
		public uint MessageId { get; init; }
		public nuint WParam { get; init; }
		public nint LParam { get; init; }
	}

	public sealed class WindowMessageEventArgs : EventArgs
	{
		public WindowMessage Message { get; init; }

		// When true the original window procedure is skipped and Result is returned
		public bool Handled { get; set; }

		public nint Result { get; set; }
	}

	/// <summary>
	/// Monitors window messages of an arbitrary window by subclassing its window procedure.
	/// </summary>
	public sealed unsafe partial class WindowMessageMonitor : IDisposable
	{
		private readonly HWND _hwnd;
		private readonly WNDPROC _newWndProc;
		private readonly nint _oldWndProc;
		private bool _disposed;

		public event EventHandler<WindowMessageEventArgs>? WindowMessageReceived;

		public WindowMessageMonitor(HWND hwnd)
		{
			_hwnd = hwnd;

			_newWndProc = new(NewWindowProc);
			var pNewWndProc = Marshal.GetFunctionPointerForDelegate(_newWndProc);
			_oldWndProc = PInvoke.SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, pNewWndProc);
		}

		private LRESULT NewWindowProc(HWND param0, uint param1, WPARAM param2, LPARAM param3)
		{
			if (WindowMessageReceived is { } handler)
			{
				var args = new WindowMessageEventArgs
				{
					Message = new() { MessageId = param1, WParam = param2.Value, LParam = param3.Value },
				};

				// A subscriber exception must not unwind through the native window procedure (fail-fast under AOT)
				try
				{
					handler(this, args);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex);
				}

				if (args.Handled)
					return new(args.Result);
			}

			var pfnOldWndProc = (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)_oldWndProc;

			return PInvoke.CallWindowProc(pfnOldWndProc, param0, param1, param2, param3);
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			_disposed = true;

			PInvoke.SetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, _oldWndProc);
		}
	}
}
