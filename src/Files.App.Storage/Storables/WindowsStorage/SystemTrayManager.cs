// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Storage
{
	/// <summary>
	/// Exposes a manager to create or delete an system tray icon you provide.
	/// </summary>
	public unsafe partial class SystemTrayManager : IDisposable
	{
		string _szWndClassName = null!;
		string _szToolTip = null!;
		HICON _hIcon = default;
		private Guid _id;
		private uint _dwCallbackMsgId;
		Action<uint> _callback = null!;

		private HWND _hWnd = default;
		private WNDPROC? _wndProc;
		private bool _isInitialized;
		private uint _dwTaskbarRestartMsgId;
		private bool _isShown;

		public void Initialize(string szWndClassName, string szToolTip, HICON hIcon, Guid id, uint dwCallbackMsgId, Action<uint> callback)
		{
			_szWndClassName = szWndClassName;
			_szToolTip = szToolTip;
			_hIcon = hIcon;
			_id = id;
			_dwCallbackMsgId = dwCallbackMsgId;
			_callback = callback;

			_isInitialized = true;
		}

		public bool CreateIcon()
		{
			// Not expected usage
			if (!_isInitialized)
				throw new InvalidOperationException($"{nameof(SystemTrayManager)} is not initialized. Call {nameof(Initialize)}() before using this method.");

			if (_hWnd.IsNull)
				_hWnd = CreateIconWindow(_szWndClassName);

			NOTIFYICONDATAW data = default;
			data.cbSize = (uint)sizeof(NOTIFYICONDATAW);
			data.hWnd = _hWnd;
			data.uCallbackMessage = _dwCallbackMsgId;
			data.guidItem = _id;
			data.hIcon = _hIcon;
			data.uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_GUID | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP;
			data.szTip = _szToolTip;
			data.Anonymous.uVersion = 4u;

			if (_isShown)
			{
				return PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, &data);
			}
			else
			{
				bool fRes = PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, &data);
				if (!fRes) return false;

				fRes = PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, &data);
				if (!fRes) return false;

				fRes = PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_SETVERSION, &data);
				if (!fRes) return false;

				_isShown = true;

				return true;
			}
		}

		public bool DeleteIcon()
		{
			if (_isShown)
			{
				NOTIFYICONDATAW data = default;
				data.cbSize = (uint)sizeof(NOTIFYICONDATAW);
				data.hWnd = _hWnd;
				data.guidItem = _id;
				data.uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_GUID;
				data.Anonymous.uVersion = 4u;

				return PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, &data);
			}

			return true;
		}

		private HWND CreateIconWindow(string szWndClassName)
		{
			fixed (char* pszWndClassName = szWndClassName)
			{
				_wndProc ??= new(WndProc);

				WNDCLASSEXW wndClass = default;

				wndClass.cbSize = (uint)sizeof(WNDCLASSEXW);
				wndClass.style = WNDCLASS_STYLES.CS_DBLCLKS;
				wndClass.hInstance = PInvoke.GetModuleHandle(default(PCWSTR));
				wndClass.lpszClassName = pszWndClassName;
				wndClass.lpfnWndProc = (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)
					Marshal.GetFunctionPointerForDelegate(_wndProc);

				PInvoke.RegisterClassEx(&wndClass);

				_dwTaskbarRestartMsgId = PInvoke.RegisterWindowMessage("TaskbarCreated");

				return PInvoke.CreateWindowEx(
					WINDOW_EX_STYLE.WS_EX_LEFT, pszWndClassName, default,
					WINDOW_STYLE.WS_OVERLAPPED, 0, 0, 1, 1, HWND.Null, HMENU.Null, HINSTANCE.Null, null);
			}
		}

		private LRESULT WndProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
		{
			if (uMsg == _dwCallbackMsgId)
			{
				_callback((uint)(lParam.Value & 0xFFFF));

				return default;
			}
			else if (uMsg is PInvoke.WM_DESTROY)
			{
				DeleteIcon();

				return default;
			}
			else if (uMsg == _dwTaskbarRestartMsgId && _isInitialized)
			{
				DeleteIcon();
				CreateIcon();
			}

			return PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);
		}

		public void Dispose()
		{
			if (!_hWnd.IsNull)
				PInvoke.DestroyWindow(_hWnd);

			if (!_hIcon.IsNull)
				PInvoke.DestroyIcon(_hIcon);

			_wndProc = null;
		}
	}
}
