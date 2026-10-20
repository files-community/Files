// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

using System.Drawing;
using System.Runtime.InteropServices;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Server
{
	/// <summary>
	/// Represents a tray icon of Notification Area so-called System Tray.
	/// </summary>
	internal sealed partial class SystemTrayIcon : IDisposable
	{
		// Constants

		private const uint WM_FILES_UNIQUE_MESSAGE = 2048u;
		private const uint WM_FILES_CONTEXTMENU_DOCSLINK = 1u;
		private const uint WM_FILES_CONTEXTMENU_QUIT = 3u;

		// Fields

		private static readonly string appEnvironment = "cd_app_env_placeholder".ToLowerInvariant();
		private readonly ResourceLoader resources = ResourceLoader.GetForViewIndependentUse();

		private readonly SystemTrayIconWindow _IconWindow;

		private readonly uint _taskbarRestartMessageId;

		private bool _notifyIconCreated;

		private DateTime _lastLaunchDate;

		// Properties

		public Guid Id { get; private set; }

		private bool _IsVisible;
		public bool IsVisible
		{
			get
			{
				return _IsVisible;
			}
			private set
			{
				if (_IsVisible != value)
				{
					_IsVisible = value;

					if (!value)
						DeleteNotifyIcon();
					else
						CreateOrModifyNotifyIcon();
				}
			}
		}

		private string _Tooltip;
		public string Tooltip
		{
			get
			{
				return _Tooltip;
			}
			set
			{
				if (_Tooltip != value)
				{
					_Tooltip = value;

					CreateOrModifyNotifyIcon();
				}
			}
		}

		private Icon _Icon;
		public Icon Icon
		{
			get
			{
				return _Icon;
			}
			set
			{
				if (_Icon != value)
				{
					_Icon = value;

					CreateOrModifyNotifyIcon();
				}
			}
		}

		private Rect Position
		{
			get
			{
				if (!IsVisible)
					return default;

				NOTIFYICONIDENTIFIER identifier = default;
				identifier.cbSize = (uint)Marshal.SizeOf<NOTIFYICONIDENTIFIER>();
				identifier.hWnd = _IconWindow.WindowHandle;
				identifier.guidItem = Id;

				// Get RECT
				PInvoke.Shell_NotifyIconGetRect(in identifier, out RECT _IconLocation);

				return new Rect(
					_IconLocation.left,
					_IconLocation.top,
					_IconLocation.right - _IconLocation.left,
					_IconLocation.bottom - _IconLocation.top);
			}
		}

		// Constructor

		/// <summary>
		/// Initializes an instance of <see cref="SystemTrayIcon"/>.
		/// </summary>
		/// <remarks>
		/// Note that initializing an instance won't make the icon visible.
		/// </remarks>
		public SystemTrayIcon()
		{
			var assetFolder = appEnvironment.Contains("preview", StringComparison.Ordinal) ? "Preview"
				: appEnvironment.Contains("stable", StringComparison.Ordinal) ? "Release" : "Dev";
			_Icon = new(System.IO.Path.Combine(Package.Current.InstalledPath, "Assets", "AppTiles", assetFolder, "Logo.ico"));
			_Tooltip = $"{Package.Current.DisplayName}{(Environment.IsPrivilegedProcess ? $" ({resources.GetString("FilesRunningAsAdmin")})" : string.Empty)}";
			_taskbarRestartMessageId = PInvoke.RegisterWindowMessage("TaskbarCreated");

			Id = (appEnvironment, Environment.IsPrivilegedProcess) switch
			{
				("sideloadpreview", false) => new("684F2832-AC2B-4630-98C2-73D6AEBD4002"),
				("storepreview", false) => new("684F2832-AC2B-4630-98C2-73D6AEBD4003"),
				("sideloadstable", false) => new("684F2832-AC2B-4630-98C2-73D6AEBD4004"),
				("storestable", false) => new("684F2832-AC2B-4630-98C2-73D6AEBD4005"),
				("sideloadpreview", true) => new("684F2832-AC2B-4630-98C2-73D6AEBD5002"),
				("storepreview", true) => new("684F2832-AC2B-4630-98C2-73D6AEBD5003"),
				("sideloadstable", true) => new("684F2832-AC2B-4630-98C2-73D6AEBD5004"),
				("storestable", true) => new("684F2832-AC2B-4630-98C2-73D6AEBD5005"),
				(_, true) => new("684F2832-AC2B-4630-98C2-73D6AEBD5001"),
				_ => new("684F2832-AC2B-4630-98C2-73D6AEBD4001"),
			};
			_IconWindow = new SystemTrayIconWindow(this);

			CreateOrModifyNotifyIcon();
		}

		// Public Methods

		/// <summary>
		/// Shows the tray icon.
		/// </summary>
		public SystemTrayIcon Show()
		{
			IsVisible = true;

			return this;
		}

		/// <summary>
		/// Hides the tray icon.
		/// </summary>
		public SystemTrayIcon Hide()
		{
			IsVisible = false;

			return this;
		}

		// Private Methods

		private void CreateOrModifyNotifyIcon()
		{
			if (IsVisible)
			{
				NOTIFYICONDATAW lpData = default;

				lpData.cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>();
				lpData.hWnd = _IconWindow.WindowHandle;
				lpData.uCallbackMessage = WM_FILES_UNIQUE_MESSAGE;
				lpData.hIcon = (Icon != null) ? new HICON(Icon.Handle) : default;
				lpData.guidItem = Id;
				lpData.uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_GUID | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP;
				lpData.szTip = _Tooltip ?? string.Empty;

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

				lpData.cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>();
				lpData.hWnd = _IconWindow.WindowHandle;
				lpData.guidItem = Id;
				lpData.uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_GUID | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP;

				// Delete the existing icon
				PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, in lpData);
			}
		}

		private void ShowContextMenu()
		{
			PInvoke.GetCursorPos(out var lpPoint);

			using DestroyMenuSafeHandle hMenu = PInvoke.CreatePopupMenu_SafeHandle();

			// Generate the classic context menu
			PInvoke.AppendMenu(hMenu, MENU_ITEM_FLAGS.MF_BYCOMMAND, WM_FILES_CONTEXTMENU_DOCSLINK, resources.GetString("Documentation"));
			PInvoke.AppendMenu(hMenu, MENU_ITEM_FLAGS.MF_SEPARATOR, 0u, string.Empty);
			PInvoke.AppendMenu(hMenu, MENU_ITEM_FLAGS.MF_BYCOMMAND, WM_FILES_CONTEXTMENU_QUIT, resources.GetString("Quit"));
			PInvoke.SetForegroundWindow(_IconWindow.WindowHandle);

			TRACK_POPUP_MENU_FLAGS tRACK_POPUP_MENU_FLAGS =
				TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD |
				(PInvoke.GetSystemMetricsForDpi(SYSTEM_METRICS_INDEX.SM_MENUDROPALIGNMENT, PInvoke.GetDpiForWindow(_IconWindow.WindowHandle)) != 0
					? TRACK_POPUP_MENU_FLAGS.TPM_RIGHTALIGN
					: TRACK_POPUP_MENU_FLAGS.TPM_LEFTBUTTON);

			switch (PInvoke.TrackPopupMenuEx(hMenu, (uint)tRACK_POPUP_MENU_FLAGS, lpPoint.X, lpPoint.Y, _IconWindow.WindowHandle, null).Value)
			{
				case 1:
					OnDocumentationClicked();
					break;
				case 3:
					OnQuitClicked();
					break;
			}
		}

		private void OnLeftClicked()
		{
			if (DateTime.Now - _lastLaunchDate < TimeSpan.FromSeconds(1))
				return;

			_lastLaunchDate = DateTime.Now;
			if (!ServerHost.ActivateWindow())
				_ = SafetyExtensions.IgnoreExceptions(() => Launcher.LaunchUriAsync(new Uri("files-dev:")).AsTask(), Program.Logger);
		}

		private void OnDocumentationClicked()
		{
			_ = SafetyExtensions.IgnoreExceptions(() => Launcher.LaunchUriAsync(new Uri("https://files.community/docs")).AsTask(), Program.Logger);
		}

		private void OnQuitClicked() => ServerHost.RequestQuit();

		internal LRESULT WindowProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
		{
			try
			{
				switch (uMsg)
				{
					case WM_FILES_UNIQUE_MESSAGE:
						{
							switch ((uint)(lParam.Value & 0xFFFF))
							{
								case PInvoke.WM_LBUTTONUP:
								case PInvoke.NIN_SELECT:
								case PInvoke.NIN_SELECT | 1:
									{
										PInvoke.SetForegroundWindow(hWnd);
										OnLeftClicked();

										break;
									}
								case PInvoke.WM_RBUTTONUP:
								case PInvoke.WM_CONTEXTMENU:
									{
										ShowContextMenu();

										break;
									}
							}

							break;
						}
					case PInvoke.WM_DESTROY:
						{
							DeleteNotifyIcon();

							break;
						}
					default:
						{
							if (uMsg == _taskbarRestartMessageId)
							{
								DeleteNotifyIcon();
								CreateOrModifyNotifyIcon();
							}

							return PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);
						}
				}
			}
			catch (Exception ex)
			{
				// A managed exception escaping this native message-pump callback would crash the process.
				Program.Logger?.LogWarning(ex, "Unhandled exception in the system tray WindowProc.");
			}

			return default;
		}

		public void Dispose()
		{
			_IconWindow.Dispose();
			_Icon.Dispose();
		}
	}
}
