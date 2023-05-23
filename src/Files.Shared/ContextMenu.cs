// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Generic;

namespace Files.Shared
{
	// Same definition of Vanara.PInvoke.User32.MenuItemType
	public enum MenuItemType : uint
	{
		MFT_STRING = 0,
		MFT_BITMAP = 4,
		MFT_MENUBARBREAK = 32,
		MFT_MENUBREAK = 64,
		MFT_OWNERDRAW = 256,
		MFT_RADIOCHECK = 512,
		MFT_SEPARATOR = 2048,
		MFT_RIGHTORDER = 8192,
		MFT_RIGHTJUSTIFY = 16384
	}

	public enum HBITMAP_HMENU : long
	{
		HBMMENU_CALLBACK = -1,
		HBMMENU_MBAR_CLOSE = 5,
		HBMMENU_MBAR_CLOSE_D = 6,
		HBMMENU_MBAR_MINIMIZE = 3,
		HBMMENU_MBAR_MINIMIZE_D = 7,
		HBMMENU_MBAR_RESTORE = 2,
		HBMMENU_POPUP_CLOSE = 8,
		HBMMENU_POPUP_MAXIMIZE = 10,
		HBMMENU_POPUP_MINIMIZE = 11,
		HBMMENU_POPUP_RESTORE = 9,
		HBMMENU_SYSTEM = 1
	}

	public class Win32ContextMenu
	{
		public List<Win32ContextMenuItem> Items { get; set; }
	}

	public class Win32ContextMenuItem
	{
		public byte[] Icon { get; set; }
		public int ID { get; set; } // Valid only in current menu to invoke item
		public string Label { get; set; }
		public string CommandString { get; set; }
		public MenuItemType Type { get; set; }
		public List<Win32ContextMenuItem> SubItems { get; set; }
	}
}