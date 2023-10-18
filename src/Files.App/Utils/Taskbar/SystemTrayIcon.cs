// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Windows.AppLifecycle;
using System.Runtime.InteropServices;
using System.Drawing;
using Windows.Foundation;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Utils.Taskbar
{
	public class SystemTrayIcon : IDisposable
	{
		private static Guid trayIconGuid = new("6CDEC4D3-9697-40DF-B6C2-96E9ED842C0C");

		private const uint WM_TRAYMOUSEMESSAGE = 2048u;

		private readonly SystemTrayIconWindow iconWindow;

		private readonly uint taskbarRestartMessageId;

		private bool notifyIconCreated;

		private bool isDoubleClick;

		public Guid Id { get; private set; }

		private bool isVisible;
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
						DeleteNotifyIcon();
					else
						CreateOrUpdateNotifyIcon();
				}
			}
		}

		private string tooltip;
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

		private Icon icon;
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
					return default(Rect);

				RECT iconLocation = default(RECT);
				NOTIFYICONIDENTIFIER identifier = default(NOTIFYICONIDENTIFIER);
				identifier.cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONIDENTIFIER));
				identifier.hWnd = iconWindow.WindowHandle;
				identifier.guidItem = Id;
				PInvoke.Shell_NotifyIconGetRect(in identifier, out iconLocation);
				return new Rect(iconLocation.left, iconLocation.top, iconLocation.right - iconLocation.left, iconLocation.bottom - iconLocation.top);
			}
		}

		public SystemTrayIcon()
		{
			Id = trayIconGuid;

			iconWindow = new SystemTrayIconWindow(this);

			taskbarRestartMessageId = PInvoke.RegisterWindowMessage("TaskbarCreated");

			CreateOrUpdateNotifyIcon();
		}

		private void CreateOrUpdateNotifyIcon()
		{
			if (IsVisible)
			{
				NOTIFYICONDATAW lpData = default(NOTIFYICONDATAW);

				lpData.cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATAW));
				lpData.hWnd = iconWindow.WindowHandle;
				lpData.uCallbackMessage = 2048u;
				lpData.hIcon = ((Icon != null) ? new HICON(Icon.Handle) : default(HICON));
				lpData.guidItem = Id;
				lpData.uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_GUID | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP;
				lpData.szTip = tooltip ?? string.Empty;

				if (!notifyIconCreated)
				{
					PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, in lpData);

					notifyIconCreated = true;

					PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, in lpData);

					lpData.Anonymous.uVersion = 4u;

					PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_SETVERSION, in lpData);
				}
				else
				{
					PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, in lpData);
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

				PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, in lpData);
			}
		}

		private void ShowContextMenu()
		{
			PInvoke.GetCursorPos(out var lpPoint);

			DestroyMenuSafeHandle hMenu = PInvoke.CreatePopupMenu_SafeHandle();

			PInvoke.AppendMenu(hMenu, MENU_ITEM_FLAGS.MF_BYCOMMAND, 1u, "Restart".GetLocalizedResource());
			PInvoke.AppendMenu(hMenu, MENU_ITEM_FLAGS.MF_BYCOMMAND, 2u, "Quit".GetLocalizedResource());
			PInvoke.SetForegroundWindow(iconWindow.WindowHandle);

			TRACK_POPUP_MENU_FLAGS tRACK_POPUP_MENU_FLAGS = TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD;

			tRACK_POPUP_MENU_FLAGS |= ((PInvoke.GetSystemMetricsForDpi((int)SYSTEM_METRICS_INDEX.SM_MENUDROPALIGNMENT, PInvoke.GetDpiForWindow(iconWindow.WindowHandle)) != 0) ? TRACK_POPUP_MENU_FLAGS.TPM_RIGHTALIGN : TRACK_POPUP_MENU_FLAGS.TPM_LEFTBUTTON);

			switch (PInvoke.TrackPopupMenuEx(hMenu, (uint)tRACK_POPUP_MENU_FLAGS, lpPoint.x, lpPoint.y, iconWindow.WindowHandle, null).Value)
			{
				case 1:
					OnRestart();
					break;
				case 2:
					OnQuit();
					break;
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

		internal LRESULT WindowProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
		{
			switch (uMsg)
			{
				case 2048u:
					switch ((uint)(lParam.Value & 0xFFFF))
					{
						case 515u:
							isDoubleClick = true;
							PInvoke.SetForegroundWindow(hWnd);
							OnDoubleClick();
							break;
						case 514u:
							if (!isDoubleClick)
							{
								PInvoke.SetForegroundWindow(hWnd);
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
					return PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);
			}
			return default(LRESULT);
		}

		public void Dispose()
		{
			iconWindow.Dispose();
		}
	}
}
