// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;

namespace Files.App.Utils.Taskbar
{
	public class SystemTrayIconWindow : IDisposable
	{
		private SystemTrayIcon icon;

		private Windows.Win32.Foundation.HWND windowHandle;

		private readonly Windows.Win32.UI.WindowsAndMessaging.WNDPROC windowProcedure;

		internal Windows.Win32.Foundation.HWND WindowHandle => windowHandle;

		public unsafe SystemTrayIconWindow(SystemTrayIcon icon)
		{
			windowProcedure = WindowProc;
			this.icon = icon;
			string text = "FilesTrayIcon_" + this.icon.Id;
			fixed (char* ptr = text)
			{
				Windows.Win32.UI.WindowsAndMessaging.WNDCLASSEXW param = new Windows.Win32.UI.WindowsAndMessaging.WNDCLASSEXW
				{
					cbSize = (uint)Marshal.SizeOf(typeof(Windows.Win32.UI.WindowsAndMessaging.WNDCLASSEXW)),
					style = Windows.Win32.UI.WindowsAndMessaging.WNDCLASS_STYLES.CS_DBLCLKS,
					lpfnWndProc = windowProcedure,
					cbClsExtra = 0,
					cbWndExtra = 0,
					hInstance = Windows.Win32.PInvoke.GetModuleHandle(default(Windows.Win32.Foundation.PCWSTR)),
					lpszClassName = ptr
				};
				Windows.Win32.PInvoke.RegisterClassEx(in param);
			}
			windowHandle = Windows.Win32.PInvoke.CreateWindowEx(Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE.WS_EX_LEFT, text, string.Empty, Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE.WS_OVERLAPPED, 0, 0, 1, 1, default(Windows.Win32.Foundation.HWND), null, null, null);
			if (windowHandle == default(Windows.Win32.Foundation.HWND))
			{
				throw new Win32Exception("Message window handle was not a valid pointer");
			}
		}

		public void Dispose()
		{
			if (windowHandle != default(Windows.Win32.Foundation.HWND))
			{
				Windows.Win32.PInvoke.DestroyWindow(windowHandle);
				windowHandle = default(Windows.Win32.Foundation.HWND);
			}
			icon = null;
		}

		private Windows.Win32.Foundation.LRESULT WindowProc(Windows.Win32.Foundation.HWND hWnd, uint uMsg, Windows.Win32.Foundation.WPARAM wParam, Windows.Win32.Foundation.LPARAM lParam)
		{
			return icon.WindowProc(hWnd, uMsg, wParam, lParam);
		}
	}
}
