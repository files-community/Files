// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Items
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

	// Same definition of Vanara.PInvoke.Shell32.CMF
	public enum CMF : uint
	{
		CMF_NORMAL = 0x00000000,
		CMF_DEFAULTONLY = 0x00000001,
		CMF_VERBSONLY = 0x00000002,
		CMF_EXPLORE = 0x00000004,
		CMF_NOVERBS = 0x00000008,
		CMF_CANRENAME = 0x00000010,
		CMF_NODEFAULT = 0x00000020,
		CMF_EXTENDEDVERBS = 0x00000100,
		CMF_INCLUDESTATIC = 0x00000040,
		CMF_ITEMMENU = 0x00000080,
		CMF_DISABLEDVERBS = 0x00000200,
		CMF_ASYNCVERBSTATE = 0x00000400,
		CMF_OPTIMIZEFORINVOKE = 0x00000800,
		CMF_SYNCCASCADEMENU = 0x00001000,
		CMF_DONOTPICKDEFAULT = 0x00002000,
		CMF_RESERVED = 0xffff0000,
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
