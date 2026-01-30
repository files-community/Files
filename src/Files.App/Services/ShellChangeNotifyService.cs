// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Services
{
	public sealed partial class ShellChangeNotifyService : IShellChangeNotifyService
	{
		private readonly ILogger _logger = Ioc.Default.GetRequiredService<ILogger<ShellChangeNotifyService>>();
		private readonly WNDPROC _windowProcedure;

		private HWND _windowHandle;
		private uint _notifyId;
		private bool _disposed;

		public event Action<string>? ItemUpdated;

		public event Action<string>? AttributesChanged;

		public ShellChangeNotifyService()
		{
			_windowProcedure = WindowProc;
		}

		public unsafe void StartMonitoring(string path)
		{
			StopMonitoring();

			string className = "FilesShellNotify_" + Environment.TickCount64;

			fixed (char* ptr = className)
			{
				var pWindProc = Marshal.GetFunctionPointerForDelegate(_windowProcedure);
				var pfnWndProc = (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)pWindProc;

				WNDCLASSEXW param = new()
				{
					cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEXW)),
					lpfnWndProc = pfnWndProc,
					hInstance = PInvoke.GetModuleHandle(default(PCWSTR)),
					lpszClassName = ptr
				};

				PInvoke.RegisterClassEx(in param);
			}

			_windowHandle = PInvoke.CreateWindowEx(
				WINDOW_EX_STYLE.WS_EX_LEFT,
				className,
				string.Empty,
				WINDOW_STYLE.WS_OVERLAPPED,
				0, 0, 0, 0,
				new HWND(-3),
				null, null, null);

			if (_windowHandle == default)
				return;

			IntPtr pidl = Win32PInvoke.ILCreateFromPath(path);
			if (pidl == IntPtr.Zero)
				return;

			try
			{
				var entry = new Win32PInvoke.SHChangeNotifyEntry
				{
					pidl = pidl,
					fRecursive = false
				};

				_notifyId = Win32PInvoke.SHChangeNotifyRegister(
					_windowHandle,
					Win32PInvoke.SHCNRF_SHELLLEVEL | Win32PInvoke.SHCNRF_INTERRUPTLEVEL,
					Win32PInvoke.SHCNE_UPDATEITEM | Win32PInvoke.SHCNE_ATTRIBUTES,
					(uint)Win32PInvoke.WM_SHNOTIFY,
					1,
					ref entry);

				if (_notifyId != 0)
					_logger.LogInformation("Shell notify registered for {Path} (ID: {Id})", path, _notifyId);
				else
					_logger.LogWarning("Shell notify registration failed for {Path}", path);
			}
			finally
			{
				Win32PInvoke.ILFree(pidl);
			}
		}

		public void StopMonitoring()
		{
			if (_notifyId != 0)
			{
				Win32PInvoke.SHChangeNotifyDeregister(_notifyId);
				_notifyId = 0;
			}

			if (_windowHandle != default)
			{
				PInvoke.DestroyWindow(_windowHandle);
				_windowHandle = default;
			}
		}

		private LRESULT WindowProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
		{
			if (uMsg == (uint)Win32PInvoke.WM_SHNOTIFY)
				ProcessNotification(wParam, lParam);

			return PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);
		}

		private void ProcessNotification(WPARAM wParam, LPARAM lParam)
		{
			uint eventId = (uint)lParam.Value;
			string path = GetPathFromNotification(wParam);

			if (string.IsNullOrEmpty(path))
				return;

			if (eventId == Win32PInvoke.SHCNE_UPDATEITEM)
			{
				_logger.LogInformation("SHCNE_UPDATEITEM: {Path}", path);
				ItemUpdated?.Invoke(path);
			}
			else if (eventId == Win32PInvoke.SHCNE_ATTRIBUTES)
			{
				_logger.LogInformation("SHCNE_ATTRIBUTES: {Path}", path);
				AttributesChanged?.Invoke(path);
			}
		}

		private static string GetPathFromNotification(WPARAM wParam)
		{
			if (wParam.Value == 0)
				return string.Empty;

			var notifyStruct = Marshal.PtrToStructure<Win32PInvoke.SHNOTIFYSTRUCT>((nint)wParam.Value);
			if (notifyStruct.dwItem1 == IntPtr.Zero)
				return string.Empty;

			IntPtr buffer = Marshal.AllocHGlobal(Win32PInvoke.MAX_PATH * 2);
			try
			{
				if (Win32PInvoke.SHGetPathFromIDList(notifyStruct.dwItem1, buffer))
					return Marshal.PtrToStringUni(buffer) ?? string.Empty;

				return string.Empty;
			}
			finally
			{
				Marshal.FreeHGlobal(buffer);
			}
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			_logger.LogInformation("Shell notify service disposed");
			StopMonitoring();
			_disposed = true;
		}
	}
}
