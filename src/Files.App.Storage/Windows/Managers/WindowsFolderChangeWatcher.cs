// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using Windows.Win32.UI.WindowsAndMessaging;
using WNDPROC = Windows.Win32.Extras.ManagedWNDPROC;

namespace Files.App.Storage
{
	internal unsafe sealed class WindowsFolderChangeWatcher : IDisposable
	{
		private const int MaxPath = 260;
		private const string WindowClassPrefix = "FilesFolderChangeNotificationWindow";

		private readonly Guid _knownFolderId;
		private readonly SHCNE_ID _changeMask;
		private readonly bool _recursive;
		private readonly string _windowClassName;

		private HWND _hWnd;
		private WNDPROC? _wndProc;
		private ITEMIDLIST* _folderPidl;
		private uint _notificationMessage;
		private uint _registrationId;
		private int _startCount;
		private bool _isDisposed;

		public event EventHandler<WindowsFolderChangeEventArgs>? FolderChanged;

		public WindowsFolderChangeWatcher(Guid knownFolderId, SHCNE_ID changeMask, bool recursive)
		{
			_knownFolderId = knownFolderId;
			_changeMask = changeMask;
			_recursive = recursive;
			_windowClassName = $"{WindowClassPrefix}_{Guid.NewGuid():N}";
		}

		public void Start()
		{
			ObjectDisposedException.ThrowIf(_isDisposed, this);

			if (Interlocked.Increment(ref _startCount) != 1)
				return;

			EnsureWindow();
			RegisterShellNotifications();
		}

		public void Stop()
		{
			if (_startCount <= 0 || Interlocked.Decrement(ref _startCount) != 0)
				return;

			UnregisterShellNotifications();
		}

		private void EnsureWindow()
		{
			if (!_hWnd.IsNull)
				return;

			fixed (char* pszWndClassName = _windowClassName)
			{
				_wndProc ??= new(WndProc);

				WNDCLASSEXW wndClass = default;
				wndClass.cbSize = (uint)sizeof(WNDCLASSEXW);
				wndClass.hInstance = PInvoke.GetModuleHandle(default(PCWSTR));
				wndClass.lpszClassName = pszWndClassName;
				wndClass.lpfnWndProc = (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)
					Marshal.GetFunctionPointerForDelegate(_wndProc);

				PInvoke.RegisterClassEx(&wndClass);

				_notificationMessage = PInvoke.RegisterWindowMessage(_windowClassName);
				_hWnd = PInvoke.CreateWindowEx(
					WINDOW_EX_STYLE.WS_EX_LEFT,
					pszWndClassName,
					default,
					WINDOW_STYLE.WS_OVERLAPPED,
					0,
					0,
					1,
					1,
					HWND.Null,
					HMENU.Null,
					HINSTANCE.Null,
					null);
			}
		}

		private void RegisterShellNotifications()
		{
			if (_registrationId != 0)
				return;

			HRESULT hr = PInvoke.SHGetKnownFolderIDList(
				in _knownFolderId,
				0,
				null,
				out ITEMIDLIST* pidl);
			if (hr.Failed || pidl is null)
				return;

			_folderPidl = pidl;

			SHChangeNotifyEntry entry = new()
			{
				pidl = _folderPidl,
				fRecursive = _recursive,
			};

			_registrationId = PInvoke.SHChangeNotifyRegister(
				_hWnd,
				SHCNRF_SOURCE.SHCNRF_ShellLevel | SHCNRF_SOURCE.SHCNRF_InterruptLevel | SHCNRF_SOURCE.SHCNRF_NewDelivery,
				(int)_changeMask,
				_notificationMessage,
				1,
				in entry);
		}

		private void UnregisterShellNotifications()
		{
			if (_registrationId != 0)
			{
				PInvoke.SHChangeNotifyDeregister(_registrationId);
				_registrationId = 0;
			}

			if (_folderPidl is not null)
			{
				PInvoke.CoTaskMemFree(_folderPidl);
				_folderPidl = null;
			}
		}

		private LRESULT WndProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
		{
			if (uMsg == _notificationMessage)
			{
				ProcessShellNotification(wParam, lParam);
				return default;
			}

			return PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);
		}

		private void ProcessShellNotification(WPARAM wParam, LPARAM lParam)
		{
			ITEMIDLIST** ppidls = null;
			int eventId = 0;
			HANDLE lockHandle = PInvoke.SHChangeNotification_Lock(
				new HANDLE((nint)wParam.Value),
				unchecked((uint)lParam.Value),
				&ppidls,
				&eventId);
			if (lockHandle.IsNull)
				return;

			try
			{
				string? path = ppidls is null ? null : GetPathFromPidl(ppidls[0]);
				string? otherPath = ppidls is null ? null : GetPathFromPidl(ppidls[1]);
				FolderChanged?.Invoke(this, new WindowsFolderChangeEventArgs((SHCNE_ID)eventId, path, otherPath));
			}
			finally
			{
				PInvoke.SHChangeNotification_Unlock(lockHandle);
			}
		}

		private static string? GetPathFromPidl(ITEMIDLIST* pidl)
		{
			if (pidl is null)
				return null;

			Span<char> path = stackalloc char[MaxPath];
			fixed (char* pathBuffer = path)
			{
				if (!PInvoke.SHGetPathFromIDList(pidl, pathBuffer))
					return null;
			}

			int length = path.IndexOf('\0');
			return path[..(length < 0 ? path.Length : length)].ToString();
		}

		public void Dispose()
		{
			if (_isDisposed)
				return;

			UnregisterShellNotifications();

			if (!_hWnd.IsNull)
			{
				PInvoke.DestroyWindow(_hWnd);
				_hWnd = HWND.Null;
			}

			fixed (char* pszWndClassName = _windowClassName)
				PInvoke.UnregisterClass(pszWndClassName, PInvoke.GetModuleHandle(default(PCWSTR)));

			_wndProc = null;
			_isDisposed = true;
		}
	}

	internal sealed class WindowsFolderChangeEventArgs : EventArgs
	{
		public SHCNE_ID ChangeType { get; }

		public string? Path { get; }

		public string? OtherPath { get; }

		public WindowsFolderChangeEventArgs(SHCNE_ID changeType, string? path, string? otherPath)
		{
			ChangeType = changeType;
			Path = path;
			OtherPath = otherPath;
		}
	}
}
