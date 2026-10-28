// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Win32;

namespace Files.App.Data.Items
{
	/// <summary>
	/// Presents the window's Win32 system menu using WinUI controls, without replacing
	/// the underlying system commands or the existing WindowEx window procedure.
	/// </summary>
	public unsafe partial class WindowEx
	{
		private const uint WmContextMenu = 0x007B;
		private const uint WmNcRightButtonUp = 0x00A5;
		private const uint WmNcHitTest = 0x0084;
		private const uint WmSysCommand = 0x0112;
		private const uint WmInitMenu = 0x0116;
		private const uint ScKeyMenu = 0xF100;
		private const uint ScRestore = 0xF120;
		private const uint ScMove = 0xF010;
		private const uint ScSize = 0xF000;
		private const uint ScMinimize = 0xF020;
		private const uint ScMaximize = 0xF030;
		private const uint ScClose = 0xF060;
		private const uint MfByPosition = 0x0400;
		private const uint MfDisabledOrGrayed = 0x0003;
		private const uint MfSeparator = 0x0800;
		private const int HtCaption = 2;
		private const int HtSysMenu = 3;
		private const int VkSpace = 0x20;
		private const int StandardMenuItemCount = 7;

		private static readonly uint[] StandardCommands =
		[
			ScRestore, ScMove, ScSize, ScMinimize, ScMaximize, ScClose
		];

		private MenuFlyout? _modernSystemMenu;
		private ResourceDictionary? _modernSystemMenuResources;

		// These declarations are local to the port; the app already uses user32.dll.
		[DllImport("user32.dll", ExactSpelling = true)]
		private static extern nint GetSystemMenu(nint hWnd, [MarshalAs(UnmanagedType.Bool)] bool revert);

		[DllImport("user32.dll", ExactSpelling = true)]
		private static extern int GetMenuItemCount(nint hMenu);

		[DllImport("user32.dll", ExactSpelling = true)]
		private static extern uint GetMenuItemID(nint hMenu, int position);

		[DllImport("user32.dll", ExactSpelling = true)]
		private static extern uint GetMenuState(nint hMenu, uint item, uint flags);

		[DllImport("user32.dll", EntryPoint = "GetMenuStringW", CharSet = CharSet.Unicode, ExactSpelling = true)]
		private static extern int GetMenuString(nint hMenu, uint item, [Out] char[] buffer, int capacity, uint flags);

		[DllImport("user32.dll", EntryPoint = "SendMessageW", ExactSpelling = true)]
		private static extern nint SendWindowMessage(nint hWnd, uint message, nint wParam, nint lParam);

		[DllImport("user32.dll", EntryPoint = "PostMessageW", ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool PostWindowMessage(nint hWnd, uint message, nint wParam, nint lParam);

		[DllImport("user32.dll", ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool ScreenToClient(nint hWnd, ref NativePoint point);

		[StructLayout(LayoutKind.Sequential)]
		private struct NativePoint
		{
			public int X;
			public int Y;
		}

		/// <summary>
		/// Handle title-bar right-click and Alt+Space. When XAML is not ready or the
		/// native system menu cannot be mirrored, allow Win32 to show its own menu.
		/// </summary>
		private bool TryHandleModernSystemMenuMessage(uint message, Windows.Win32.Foundation.WPARAM wParam, Windows.Win32.Foundation.LPARAM lParam)
		{
			try
			{
				if (message == WmSysCommand)
				{
					// SC_KEYMENU with Space (Alt+Space); keep the native minimized menu.
					if (((ulong)wParam.Value & 0xFFF0) != ScKeyMenu ||
						(long)lParam.Value != VkSpace ||
						PInvoke.IsIconic(new(WindowHandle)))
						return false;

					return ShowModernSystemMenu(null);
				}

				if (message == WmNcRightButtonUp)
				{
					if ((long)wParam.Value is not (HtCaption or HtSysMenu))
						return false;

					return ShowModernSystemMenu(lParam.Value);
				}

				if (message == WmContextMenu && (nint)wParam.Value == WindowHandle)
				{
					// WM_CONTEXTMENU also occurs inside client content. Do not steal
					// the file list's own menus or the keyboard context-menu key.
					if ((long)lParam.Value == -1)
						return false;

					nint hit = SendWindowMessage(WindowHandle, WmNcHitTest, 0, lParam.Value);
					if (hit != HtCaption && hit != HtSysMenu)
						return false;

					return ShowModernSystemMenu(lParam.Value);
				}
			}
			catch (Exception ex)
			{
				// Managed exceptions must not unwind through a native WNDPROC.
				Debug.WriteLine($"Modern system menu unavailable: {ex}");
			}

			return false;
		}

		private bool ShowModernSystemMenu(nint? screenCoordinates)
		{
			if (Content is not FrameworkElement target || target.XamlRoot is null)
				return false;

			var hMenu = GetSystemMenu(WindowHandle, false);
			if (hMenu == 0 || GetMenuItemCount(hMenu) < 2)
				return false;

			// The native menu is owned by the window; never destroy or reset it.
			// Notify the window so applications that modify the system menu during
			// WM_INITMENU can refresh its state before we take a snapshot.
			SendWindowMessage(WindowHandle, WmInitMenu, hMenu, 0);

			var menu = GetOrCreateModernSystemMenu();
			if (!RefreshModernSystemMenu(menu, hMenu))
				return false;

			float dpiScale = 96f / PInvoke.GetDpiForWindow(new(WindowHandle));
			Point position;

			if (screenCoordinates is nint packed)
			{
				long value = (long)packed;
				NativePoint nativePoint = new()
				{
					X = unchecked((short)(value & 0xFFFF)),
					Y = unchecked((short)((value >> 16) & 0xFFFF))
				};

				if (!ScreenToClient(WindowHandle, ref nativePoint))
					return false;

				position = new Point(nativePoint.X * dpiScale, nativePoint.Y * dpiScale);
			}
			else
			{
				// Extended content begins at the top edge of the window. Ordinary
				// window content already begins below the non-client title bar.
				float titleBarHeight = ExtendsContentIntoTitleBar ? AppWindow.TitleBar.Height * dpiScale : 0;
				position = new Point(0, titleBarHeight);
			}

			menu.XamlRoot = target.XamlRoot;
			menu.ShowAt(target, new FlyoutShowOptions
			{
				Position = position,
				ShowMode = FlyoutShowMode.Standard
			});
			return true;
		}

		private MenuFlyout GetOrCreateModernSystemMenu()
		{
			if (_modernSystemMenu is not null)
				return _modernSystemMenu;

			// Load a separate instance of the reference markup for each window.
			_modernSystemMenuResources = new ResourceDictionary
			{
				Source = new Uri("ms-appx:///Styles/ModernStandardWindowContextMenu.xaml")
			};
			var menu = (MenuFlyout)_modernSystemMenuResources["WindowExModernSystemMenu"];
			for (int index = 0; index < StandardCommands.Length; index++)
			{
				int menuIndex = index == StandardCommands.Length - 1 ? StandardMenuItemCount - 1 : index;
				uint command = StandardCommands[index];
				((MenuFlyoutItem)menu.Items[menuIndex]).Click += (_, _) => ExecuteSystemMenuCommand(command);
			}

			_modernSystemMenu = menu;
			return menu;
		}

		private bool RefreshModernSystemMenu(MenuFlyout menu, nint hMenu)
		{
			// Remove custom items created for the previous presentation.
			while (menu.Items.Count > StandardMenuItemCount)
				menu.Items.RemoveAt(StandardMenuItemCount - 1);

			for (int index = 0; index < StandardCommands.Length; index++)
			{
				uint command = StandardCommands[index];
				int menuIndex = index == StandardCommands.Length - 1 ? StandardMenuItemCount - 1 : index;
				var item = (MenuFlyoutItem)menu.Items[menuIndex];
				uint state = GetMenuState(hMenu, command, 0);
				string name = GetNativeMenuText(hMenu, command, 0);
				bool exists = state != uint.MaxValue && !string.IsNullOrEmpty(name);

				item.Visibility = exists ? Visibility.Visible : Visibility.Collapsed;
				if (exists)
				{
					item.Text = name;
					item.IsEnabled = (state & MfDisabledOrGrayed) == 0;
				}
			}

			// Native menus may be extended by the app. Preserve simple custom
			// commands and separators; let Windows render popup submenus itself.
			int count = GetMenuItemCount(hMenu);
			for (int i = StandardMenuItemCount - 1; i < count - 1; i++)
			{
				uint state = GetMenuState(hMenu, (uint)i, MfByPosition);
				uint id = GetMenuItemID(hMenu, i);
				if ((state & MfSeparator) == 0 && id == uint.MaxValue)
					return false;
			}

			for (int i = StandardMenuItemCount - 1; i < count - 1; i++)
			{
				uint state = GetMenuState(hMenu, (uint)i, MfByPosition);
				int insertionIndex = menu.Items.Count - 1;
				if ((state & MfSeparator) != 0)
				{
					menu.Items.Insert(insertionIndex, new MenuFlyoutSeparator());
					continue;
				}

				uint id = GetMenuItemID(hMenu, i);
				string text = GetNativeMenuText(hMenu, (uint)i, MfByPosition);
				if (id == uint.MaxValue || string.IsNullOrWhiteSpace(text))
					continue;

				var item = new MenuFlyoutItem
				{
					Text = text,
					IsEnabled = (state & MfDisabledOrGrayed) == 0,
					Style = ((MenuFlyoutItem)menu.Items[0]).Style
				};
				uint command = id;
				item.Click += (_, _) => ExecuteSystemMenuCommand(command);
				menu.Items.Insert(insertionIndex, item);
			}

			((MenuFlyoutSeparator)menu.Items[5]).Visibility =
				menu.Items.Take(5).Any(item => item.Visibility == Visibility.Visible)
				? Visibility.Visible : Visibility.Collapsed;

			return true;
		}

		private static string GetNativeMenuText(nint menu, uint item, uint flags)
		{
			char[] buffer = new char[256];
			int count = GetMenuString(menu, item, buffer, buffer.Length, flags);
			if (count <= 0)
				return string.Empty;

			// Win32 ampersands mark mnemonic letters; WinUI renders them literally.
			string text = new string(buffer, 0, count).Replace("&&", "\u0001").Replace("&", "").Replace("\u0001", "&");
			int accelerator = text.IndexOf('\t');
			return accelerator >= 0 ? text[..accelerator] : text;
		}

		private void ExecuteSystemMenuCommand(uint command)
		{
			// Hide first: SC_MOVE and SC_SIZE enter native modal loops, and
			// minimizing while a WinUI flyout is open can misbehave.
			_modernSystemMenu?.Hide();
			PostWindowMessage(WindowHandle, WmSysCommand, (nint)command, 0);
		}
	}
}
