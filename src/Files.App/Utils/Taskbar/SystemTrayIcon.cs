// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace Files.App.Utils.Taskbar
{
	public class SystemTrayIcon
	{
		public bool TryCreate()
		{
			var hWnd = MainWindow.Instance.WindowHandle;
			var hIcon = GetAppIconHICON();

			var data = new Shell32.NOTIFYICONDATA
			{
				cbSize = (uint)Marshal.SizeOf(typeof(Shell32.NOTIFYICONDATA)),
				uFlags =
					Shell32.NIF.NIF_ICON |
					Shell32.NIF.NIF_TIP |
					Shell32.NIF.NIF_SHOWTIP |
					Shell32.NIF.NIF_MESSAGE,
				guidItem = new Guid(),
				uCallbackMessage = 0x400,
				hwnd = new HWND(hWnd),
				hIcon = new HICON(hIcon),
				szTip = $"Files"
			};

			bool res = Shell32.Shell_NotifyIcon(Shell32.NIM.NIM_ADD, in data);

			// Set the version
			data.uTimeoutOrVersion = 4;
			res = Shell32.Shell_NotifyIcon(Shell32.NIM.NIM_SETVERSION, in data);

			var error = Kernel32.GetLastError();

			return res;
		}

		public nint GetAppIconHICON()
		{
			var applicationService = Ioc.Default.GetRequiredService<IApplicationService>();
			var iconPath = SystemIO.Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, applicationService.AppIcoPath);

			var hIcon = User32.LoadImage(
				IntPtr.Zero,
				iconPath,
				User32.LoadImageType.IMAGE_ICON,
				0,
				0,
				User32.LoadImageOptions.LR_LOADFROMFILE |
				User32.LoadImageOptions.LR_DEFAULTSIZE);

			return hIcon;
		}

		public void AddContextMenu()
		{
			var menu = User32.CreatePopupMenu();
		}
	}
}
