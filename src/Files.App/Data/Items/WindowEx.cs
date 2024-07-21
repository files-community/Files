// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Data.Items
{
	public unsafe class WindowEx : Window
	{
		private readonly WNDPROC _oldWndProc;
		private readonly WNDPROC _newWndProc;

		public nint WindowHandle { get; }

		public int MinWidth { get; set; }
		public int MinHeight { get; set; }

		private bool _IsMaximizable = true;
		public bool IsMaximizable
		{
			get => _IsMaximizable;
			set
			{
				_IsMaximizable = value;
				UpdateOverlappedPresenter((c) => c.IsMaximizable = value);

				if (value)
				{
					// NOTE:
					//  Indicates to the Shell that the window should not be treated as full-screen.
					// WORKAROUND:
					//  https://github.com/microsoft/microsoft-ui-xaml/issues/8431
					//  Not to mess up the taskbar when being full-screen mode.
					//  This property should only be set if the "Automatically hide the taskbar" in Windows 11,
					//  or "Automatically hide the taskbar in desktop mode" in Windows 10 is enabled.
					//  Setting this property when the setting is disabled will result in the taskbar overlapping the application.
					if (AppLifecycleHelper.IsAutoHideTaskbarEnabled())
						Win32PInvoke.SetPropW(WindowHandle, "NonRudeHWND", new IntPtr(1));
				}
			}
		}

		private bool _IsMinimizable = true;
		public bool IsMinimizable
		{
			get => _IsMinimizable;
			set
			{
				_IsMaximizable = value;
				UpdateOverlappedPresenter((c) => c.IsMinimizable = value);
			}
		}

		public unsafe WindowEx(int minWidth = 400, int minHeight = 300)
		{
			WindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
			MinWidth = minWidth;
			MinHeight = minHeight;

			_newWndProc = new(NewWindowProc);
			var pNewWndProc = Marshal.GetFunctionPointerForDelegate(_newWndProc);
			var pOldWndProc = PInvoke.SetWindowLongPtr(new(WindowHandle), WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, pNewWndProc);
			_oldWndProc = Marshal.GetDelegateForFunctionPointer<WNDPROC>(pOldWndProc);
		}

		private void UpdateOverlappedPresenter(Action<OverlappedPresenter> action)
		{
			if (AppWindow.Presenter is OverlappedPresenter overlapped)
				action(overlapped);
			else
				throw new NotSupportedException($"'{AppWindow.Presenter.Kind}' presenter is not supported.");
		}

		private LRESULT NewWindowProc(HWND param0, uint param1, WPARAM param2, LPARAM param3)
		{
			switch (param1)
			{
				case 0x0024: /*WM_GETMINMAXINFO*/
					{
						var dpi = PInvoke.GetDpiForWindow(new(param0));
						float scalingFactor = (float)dpi / 96;

						var minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(param3);
						minMaxInfo.ptMinTrackSize.X = (int)(MinWidth * scalingFactor);
						minMaxInfo.ptMinTrackSize.Y = (int)(MinHeight * scalingFactor);
						Marshal.StructureToPtr(minMaxInfo, param3, true);
						break;
					}
			}

			return PInvoke.CallWindowProc(_oldWndProc, param0, param1, param2, param3);
		}
	}
}
