// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Storage
{
	public unsafe sealed class WindowsDriveManager : IDisposable
	{
		private const int MaxPath = 260;
		private const string WindowClassName = "FilesShellDriveNotificationWindow";

		private static readonly Lazy<WindowsDriveManager> lazy = new(() => new WindowsDriveManager());

		private HWND _hWnd;
		private WNDPROC? _wndProc;
		private ITEMIDLIST* _computerFolderPidl;
		private uint _notificationMessage;
		private uint _registrationId;
		private int _startCount;
		private bool _isDisposed;

		public event EventHandler<DeviceEventArgs>? DeviceAdded;
		public event EventHandler<DeviceEventArgs>? DeviceRemoved;
		public event EventHandler<DeviceEventArgs>? DeviceInserted;
		public event EventHandler<DeviceEventArgs>? DeviceEjected;

		public static WindowsDriveManager Default => lazy.Value;

		private WindowsDriveManager()
		{
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

			fixed (char* pszWndClassName = WindowClassName)
			{
				_wndProc ??= new(WndProc);

				WNDCLASSEXW wndClass = default;
				wndClass.cbSize = (uint)sizeof(WNDCLASSEXW);
				wndClass.hInstance = PInvoke.GetModuleHandle(default(PCWSTR));
				wndClass.lpszClassName = pszWndClassName;
				wndClass.lpfnWndProc = (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)
					Marshal.GetFunctionPointerForDelegate(_wndProc);

				PInvoke.RegisterClassEx(&wndClass);

				_notificationMessage = PInvoke.RegisterWindowMessage("FilesShellDriveNotification");
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

			Guid computerFolderId = *FOLDERID.FOLDERID_ComputerFolder;
			HRESULT hr = PInvoke.SHGetKnownFolderIDList(
				in computerFolderId,
				0,
				null,
				out ITEMIDLIST* pidl);
			if (hr.Failed || pidl is null)
				return;

			_computerFolderPidl = pidl;

			SHChangeNotifyEntry entry = new()
			{
				pidl = _computerFolderPidl,
				fRecursive = true,
			};

			_registrationId = PInvoke.SHChangeNotifyRegister(
				_hWnd,
				SHCNRF_SOURCE.SHCNRF_ShellLevel | SHCNRF_SOURCE.SHCNRF_InterruptLevel | SHCNRF_SOURCE.SHCNRF_NewDelivery,
				(int)(SHCNE_ID.SHCNE_DRIVEADD | SHCNE_ID.SHCNE_DRIVEREMOVED | SHCNE_ID.SHCNE_MEDIAINSERTED | SHCNE_ID.SHCNE_MEDIAREMOVED),
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

			if (_computerFolderPidl is not null)
			{
				PInvoke.ILFree(_computerFolderPidl);
				_computerFolderPidl = null;
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
				string? driveId = ppidls is null ? null : GetDriveIdFromPidl(ppidls[0]);
				if (string.IsNullOrEmpty(driveId))
					return;

				RaiseDriveEvent((SHCNE_ID)eventId, driveId);
			}
			finally
			{
				PInvoke.SHChangeNotification_Unlock(lockHandle);
			}
		}

		private static string? GetDriveIdFromPidl(ITEMIDLIST* pidl)
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
			string drivePath = path[..(length < 0 ? path.Length : length)].ToString();
			return NormalizeDriveId(drivePath);
		}

		private static string? NormalizeDriveId(string drivePath)
		{
			if (string.IsNullOrWhiteSpace(drivePath))
				return null;

			string root = SystemIO.Path.GetPathRoot(drivePath) ?? drivePath;
			return root.TrimEnd(SystemIO.Path.DirectorySeparatorChar, SystemIO.Path.AltDirectorySeparatorChar);
		}

		private void RaiseDriveEvent(SHCNE_ID eventId, string driveId)
		{
			DeviceEventArgs args = new(driveId, driveId);

			if ((eventId & SHCNE_ID.SHCNE_DRIVEADD) != 0)
				DeviceAdded?.Invoke(this, args);

			if ((eventId & SHCNE_ID.SHCNE_DRIVEREMOVED) != 0)
				DeviceRemoved?.Invoke(this, args);

			if ((eventId & SHCNE_ID.SHCNE_MEDIAINSERTED) != 0)
				DeviceInserted?.Invoke(this, args);

			if ((eventId & SHCNE_ID.SHCNE_MEDIAREMOVED) != 0)
				DeviceEjected?.Invoke(this, args);
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

			fixed (char* pszWndClassName = WindowClassName)
				PInvoke.UnregisterClass(pszWndClassName, PInvoke.GetModuleHandle(default(PCWSTR)));

			_wndProc = null;
			_isDisposed = true;
		}
	}
}
