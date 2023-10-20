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
	/// <summary>
	/// Represents an icon of Notification Area so-called System Tray.
	/// </summary>
	public class SystemTrayIcon : IDisposable
	{
		private const uint WM_TRAYMOUSEMESSAGE = 2048u;

		private readonly static Guid _trayIconGuid = new("6CDEC4D3-9697-40DF-B6C2-96E9ED842C0C");

		private readonly SystemTrayIconWindow _iconWindow;

		private readonly uint _taskbarRestartMessageId;

		private bool _notifyIconCreated;

		private bool _isDoubleClick;

		public Guid Id { get; private set; }

		private bool _isVisible;
		public bool IsVisible
		{
			get
			{
				return _isVisible;
			}
			private set
			{
				if (_isVisible != value)
				{
					_isVisible = value;

					if (!value)
						DeleteNotifyIcon();
					else
						CreateOrModifyNotifyIcon();
				}
			}
		}

		private string _tooltip;
		public string Tooltip
		{
			get
			{
				return _tooltip;
			}
			set
			{
				if (_tooltip != value)
				{
					_tooltip = value;

					CreateOrModifyNotifyIcon();
				}
			}
		}

		private Icon _icon;
		public Icon Icon
		{
			get
			{
				return _icon;
			}
			set
			{
				if (_icon != value)
				{
					_icon = value;

					CreateOrModifyNotifyIcon();
				}
			}
		}

		public Rect Position
		{
			get
			{
				if (!IsVisible)
					return default;

				NOTIFYICONIDENTIFIER identifier = default;
				identifier.cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONIDENTIFIER));
				identifier.hWnd = _iconWindow.WindowHandle;
				identifier.guidItem = Id;

				// Get RECT
				PInvoke.Shell_NotifyIconGetRect(in identifier, out RECT _iconLocation);

				return new Rect(
					_iconLocation.left,
					_iconLocation.top,
					_iconLocation.right - _iconLocation.left,
					_iconLocation.bottom - _iconLocation.top);
			}
		}

		/// <summary>
		/// Initializes an instance of <see cref="SystemTrayIcon"/>.
		/// </summary>
		/// <remarks>
		/// Note that initializing an instance won't make the icon visible.
		/// </remarks>
		public SystemTrayIcon()
		{
			var applicationService = Ioc.Default.GetRequiredService<IApplicationService>();
			var iconPath = SystemIO.Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, applicationService.AppIcoPath);

			_icon = new(iconPath);
			_tooltip = string.Empty;
			_taskbarRestartMessageId = PInvoke.RegisterWindowMessage("TaskbarCreated");

			Id = _trayIconGuid;
			_iconWindow = new SystemTrayIconWindow(this);

			CreateOrModifyNotifyIcon();
		}

		/// <summary>
		/// Initializes an instance of <see cref="SystemTrayIcon"/>.
		/// </summary>
		/// <remarks>
		/// Note that initializing an instance won't make the icon visible.
		/// </remarks>
		/// <param name="tooltip">String of tooltip to show on hover.</param>
		/// <param name="iconPath">Icon to be shown on the tray rect.</param>
		public SystemTrayIcon(string tooltip, string iconPath) : this()
		{
			_icon = new(iconPath);
			_tooltip = tooltip;
		}

		public SystemTrayIcon Show()
		{
			IsVisible = true;

			return this;
		}

		public SystemTrayIcon Remove()
		{
			IsVisible = false;

			return this;
		}

		private void CreateOrModifyNotifyIcon()
		{
			if (IsVisible)
			{
				NOTIFYICONDATAW lpData = default;

				lpData.cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATAW));
				lpData.hWnd = _iconWindow.WindowHandle;
				lpData.uCallbackMessage = WM_TRAYMOUSEMESSAGE;
				lpData.hIcon = (Icon != null) ? new HICON(Icon.Handle) : default;
				lpData.guidItem = Id;
				lpData.uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_GUID | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP;
				lpData.szTip = _tooltip ?? string.Empty;

				if (!_notifyIconCreated)
				{
					// Delete the existing icon
					PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, in lpData);

					_notifyIconCreated = true;

					// Add a new icon
					PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, in lpData);

					lpData.Anonymous.uVersion = 4u;

					// Set the icon handler version
					// NOTE: Do not omit this code. If you remove, the icon won't be shown.
					PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_SETVERSION, in lpData);
				}
				else
				{
					// Modify the existing icon
					PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, in lpData);
				}
			}
		}

		private void DeleteNotifyIcon()
		{
			if (_notifyIconCreated)
			{
				_notifyIconCreated = false;

				NOTIFYICONDATAW lpData = default;

				lpData.cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATAW));
				lpData.hWnd = _iconWindow.WindowHandle;
				lpData.guidItem = Id;
				lpData.uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_GUID | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP;

				// Delete the existing icon
				PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, in lpData);
			}
		}

		private void ShowContextMenu()
		{
			PInvoke.GetCursorPos(out var lpPoint);

			DestroyMenuSafeHandle hMenu = PInvoke.CreatePopupMenu_SafeHandle();

			// Generate the classic context menu
			PInvoke.AppendMenu(hMenu, MENU_ITEM_FLAGS.MF_BYCOMMAND, 1u, "Restart".GetLocalizedResource());
			PInvoke.AppendMenu(hMenu, MENU_ITEM_FLAGS.MF_BYCOMMAND, 2u, "Quit".GetLocalizedResource());
			PInvoke.SetForegroundWindow(_iconWindow.WindowHandle);

			TRACK_POPUP_MENU_FLAGS tRACK_POPUP_MENU_FLAGS =
				TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD |
				(PInvoke.GetSystemMetricsForDpi((int)SYSTEM_METRICS_INDEX.SM_MENUDROPALIGNMENT, PInvoke.GetDpiForWindow(_iconWindow.WindowHandle)) != 0
					? TRACK_POPUP_MENU_FLAGS.TPM_RIGHTALIGN
					: TRACK_POPUP_MENU_FLAGS.TPM_LEFTBUTTON);

			switch (PInvoke.TrackPopupMenuEx(hMenu, (uint)tRACK_POPUP_MENU_FLAGS, lpPoint.x, lpPoint.y, _iconWindow.WindowHandle, null).Value)
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
			// Add the functionality
		}

		private void OnDoubleClick()
		{
			// Preserved
		}

		private void OnRestart()
		{
			// Add the functionality
		}

		private void OnQuit()
		{
			// Add the functionality
		}

		internal LRESULT WindowProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
		{
			switch (uMsg)
			{
				case WM_TRAYMOUSEMESSAGE:
					switch ((uint)(lParam.Value & 0xFFFF))
					{
						case 515u:
							_isDoubleClick = true;
							PInvoke.SetForegroundWindow(hWnd);
							OnDoubleClick();
							break;
						case 514u:
							if (!_isDoubleClick)
							{
								PInvoke.SetForegroundWindow(hWnd);
								OnClick();
							}
							_isDoubleClick = false;
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
					if (uMsg == _taskbarRestartMessageId)
					{
						DeleteNotifyIcon();
						CreateOrModifyNotifyIcon();
					}

					return PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);
			}

			return default;
		}

		public void Dispose()
		{
			_iconWindow.Dispose();
		}
	}
}
