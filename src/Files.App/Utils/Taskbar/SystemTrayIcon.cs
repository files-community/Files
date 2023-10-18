// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using System;
using System.ComponentModel;
using System.Drawing;
using Windows.Foundation;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.ApplicationModel.Activation;
using Microsoft.Windows.AppLifecycle;
using Windows.Storage;

namespace Files.App.Utils.Taskbar
{
	public class SystemTrayIcon
	{
		private static Guid trayIconGuid = new("6CDEC4D3-9697-40DF-B6C2-96E9ED842C0C");

		private const uint WM_TRAYMOUSEMESSAGE = 2048u;

		private readonly SystemTrayIconWindow iconWindow;

		private readonly uint taskbarRestartMessageId;

		private bool notifyIconCreated;

		private bool isDoubleClick;

		private bool isVisible;

		private string tooltip;

		private Icon icon;

		public Guid Id { get; private set; }

		public bool IsVisible
		{
			get
			{
				return isVisible;
			}
			set
			{
				if (isVisible != value)
				{
					isVisible = value;
					if (!value)
					{
						DeleteNotifyIcon();
					}
					else
					{
						CreateOrUpdateNotifyIcon();
					}
				}
			}
		}

		public string Tooltip
		{
			get
			{
				return tooltip;
			}
			set
			{
				if (tooltip != value)
				{
					tooltip = value;
					CreateOrUpdateNotifyIcon();
				}
			}
		}

		public Icon Icon
		{
			get
			{
				return icon;
			}
			set
			{
				if (icon != value)
				{
					icon = value;
					CreateOrUpdateNotifyIcon();
				}
			}
		}

		public Rect Position
		{
			get
			{
				if (!IsVisible)
				{
					return default(Rect);
				}
				Windows.Win32.Foundation.RECT iconLocation = default(Windows.Win32.Foundation.RECT);
				NOTIFYICONIDENTIFIER identifier = default(NOTIFYICONIDENTIFIER);
				identifier.cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONIDENTIFIER));
				identifier.hWnd = iconWindow.WindowHandle;
				identifier.guidItem = Id;
				Windows.Win32.PInvoke.Shell_NotifyIconGetRect(in identifier, out iconLocation);
				return new Rect(iconLocation.left, iconLocation.top, iconLocation.right - iconLocation.left, iconLocation.bottom - iconLocation.top);
			}
		}

		public SystemTrayIcon()
		{
			Id = trayIconGuid;
			iconWindow = new SystemTrayIconWindow(this);
			taskbarRestartMessageId = Windows.Win32.PInvoke.RegisterWindowMessage("TaskbarCreated");
			CreateOrUpdateNotifyIcon();
		}

		public void Dispose()
		{
			iconWindow.Dispose();
		}

		private void CreateOrUpdateNotifyIcon()
		{
			if (IsVisible)
			{
				NOTIFYICONDATAW lpData = default(NOTIFYICONDATAW);
				lpData.cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATAW));
				lpData.hWnd = iconWindow.WindowHandle;
				lpData.uCallbackMessage = 2048u;
				lpData.hIcon = ((Icon != null) ? new Windows.Win32.UI.WindowsAndMessaging.HICON(Icon.Handle) : default(Windows.Win32.UI.WindowsAndMessaging.HICON));
				lpData.guidItem = Id;
				lpData.uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_GUID | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP;
				lpData.szTip = tooltip ?? string.Empty;
				if (!notifyIconCreated)
				{
					Windows.Win32.PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, in lpData);
					notifyIconCreated = true;
					Windows.Win32.PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, in lpData);
					lpData.Anonymous.uVersion = 4u;
					Windows.Win32.PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_SETVERSION, in lpData);
				}
				else
				{
					Windows.Win32.PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, in lpData);
				}
			}
		}

		private void DeleteNotifyIcon()
		{
			if (notifyIconCreated)
			{
				notifyIconCreated = false;
				NOTIFYICONDATAW lpData = default(NOTIFYICONDATAW);
				lpData.cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATAW));
				lpData.hWnd = iconWindow.WindowHandle;
				lpData.guidItem = Id;
				lpData.uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_GUID | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP;
				Windows.Win32.PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, in lpData);
			}
		}

		private void OnClick()
		{
			var pool = new Semaphore(0, 1, $"Files-{ApplicationService.AppEnvironment}-Instance", out var isNew);

			if (!isNew)
			{
				// Resume cached instance
				pool.Release();
				var activePid = ApplicationData.Current.LocalSettings.Values.Get("INSTANCE_ACTIVE", -1);
				var instance = AppInstance.FindOrRegisterForKey(activePid.ToString());
				Program.RedirectActivationTo(instance, AppInstance.GetCurrent().GetActivatedEventArgs());

				// Exit
				Environment.Exit(0);
			}

			pool.Dispose();
		}

		private void OnDoubleClick()
		{
			// Preserved
		}

		private void OnRestart()
		{
			Program.Pool.Release();

			MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				await Windows.System.Launcher.LaunchUriAsync(new Uri("files-uwp:"));
			})
			.Wait(1000);
		}

		private void OnQuit()
		{
			Program.Pool.Release();
		}

		private void ShowContextMenu()
		{
			Windows.Win32.PInvoke.GetCursorPos(out var lpPoint);
			DestroyMenuSafeHandle hMenu = Windows.Win32.PInvoke.CreatePopupMenu_SafeHandle();
			Windows.Win32.PInvoke.AppendMenu(hMenu, MENU_ITEM_FLAGS.MF_BYCOMMAND, 1u, "Restart");
			Windows.Win32.PInvoke.AppendMenu(hMenu, MENU_ITEM_FLAGS.MF_BYCOMMAND, 2u, "Quit");
			Windows.Win32.PInvoke.SetForegroundWindow(iconWindow.WindowHandle);
			TRACK_POPUP_MENU_FLAGS tRACK_POPUP_MENU_FLAGS = TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD;
			tRACK_POPUP_MENU_FLAGS |= ((Windows.Win32.PInvoke.GetSystemMetricsForDpi((int)Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_MENUDROPALIGNMENT, Windows.Win32.PInvoke.GetDpiForWindow(iconWindow.WindowHandle)) != 0) ? TRACK_POPUP_MENU_FLAGS.TPM_RIGHTALIGN : TRACK_POPUP_MENU_FLAGS.TPM_LEFTBUTTON);
			switch (Windows.Win32.PInvoke.TrackPopupMenuEx(hMenu, (uint)tRACK_POPUP_MENU_FLAGS, lpPoint.x, lpPoint.y, iconWindow.WindowHandle, null).Value)
			{
				case 1:
					OnRestart();
					break;
				case 2:
					OnQuit();
					break;
			}
		}

		internal Windows.Win32.Foundation.LRESULT WindowProc(Windows.Win32.Foundation.HWND hWnd, uint uMsg, Windows.Win32.Foundation.WPARAM wParam, Windows.Win32.Foundation.LPARAM lParam)
		{
			switch (uMsg)
			{
				case 2048u:
					switch ((uint)(lParam.Value & 0xFFFF))
					{
						case 515u:
							isDoubleClick = true;
							Windows.Win32.PInvoke.SetForegroundWindow(hWnd);
							OnDoubleClick();
							break;
						case 514u:
							if (!isDoubleClick)
							{
								Windows.Win32.PInvoke.SetForegroundWindow(hWnd);
								OnClick();
							}
							isDoubleClick = false;
							break;
						case 517u:
							ShowContextMenu();
							break;
					}
					break;
				case 2u:
					DeleteNotifyIcon();
					break;
				default:
					if (uMsg == taskbarRestartMessageId)
					{
						DeleteNotifyIcon();
						CreateOrUpdateNotifyIcon();
					}
					return Windows.Win32.PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);
			}
			return default(Windows.Win32.Foundation.LRESULT);
		}
	}
}
