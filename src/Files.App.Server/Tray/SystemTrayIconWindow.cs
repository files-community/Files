// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.ComponentModel;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WNDPROC = Windows.Win32.Extras.ManagedWNDPROC;

namespace Files.App.Server
{
	/// <summary>
	/// Represents an icon window of Notification Area so-called System Tray.
	/// <br/>
	/// This is provided to handle context menu and retrieve mouse events, not a regular window class.
	/// </summary>
	internal sealed partial class SystemTrayIconWindow : IDisposable
	{
		private SystemTrayIcon? _trayIcon;

		private readonly WNDPROC _windowProcedure;
		private readonly string className;

		private HWND _windowHandle;
		internal HWND WindowHandle
			=> _windowHandle;

		public unsafe SystemTrayIconWindow(SystemTrayIcon icon)
		{
			_windowProcedure = WindowProc;
			_trayIcon = icon;
			className = "FilesTrayIcon_" + _trayIcon.Id;

			fixed (char* ptr = className)
			{
				var pWindProc = Marshal.GetFunctionPointerForDelegate<WNDPROC>(_windowProcedure);
				var pfnWndProc = (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)pWindProc;

				WNDCLASSEXW param = new()
				{
					cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
					style = WNDCLASS_STYLES.CS_DBLCLKS,
					lpfnWndProc = pfnWndProc,
					cbClsExtra = 0,
					cbWndExtra = 0,
					hInstance = PInvoke.GetModuleHandle(default(PCWSTR)),
					lpszClassName = ptr
				};

				if (PInvoke.RegisterClassEx(in param) == 0)
					throw new Win32Exception(Marshal.GetLastPInvokeError());
			}

			_windowHandle = PInvoke.CreateWindowEx(
				WINDOW_EX_STYLE.WS_EX_LEFT,
				className,
				string.Empty,
				WINDOW_STYLE.WS_OVERLAPPED,
				0,
				0,
				1,
				1,
				default,
				null,
				null,
				null);

			if (_windowHandle == default)
			{
				var error = Marshal.GetLastPInvokeError();
				UnregisterClass();
				throw new Win32Exception(error);
			}
		}

		private LRESULT WindowProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
		{
			return _trayIcon!.WindowProc(hWnd, uMsg, wParam, lParam);
		}

		private unsafe void UnregisterClass()
		{
			fixed (char* name = className)
				PInvoke.UnregisterClass(name, PInvoke.GetModuleHandle(default(PCWSTR)));
		}

		public void Dispose()
		{
			if (_windowHandle != default)
			{
				PInvoke.DestroyWindow(_windowHandle);
				_windowHandle = default;
				UnregisterClass();
			}

			_trayIcon = null;
		}
	}
}
