using System;

namespace Files.App.Commands
{
	[Flags]
	public enum KeyModifiers : ushort
	{
		None = 0x0000,
		Ctrl = 0x0001,
		Menu = 0x0002,
		Shift = 0x0004,
		Win = 0x0008,
		CtrlMenu = Ctrl + Menu,
		CtrlShift = Ctrl + Shift,
		CtrlWin = Ctrl + Win,
		MenuShift = Menu + Shift,
		MenuWin = Menu + Win,
		ShiftWin = Shift + Win,
		CtrlMenuShift = Ctrl + Menu + Shift,
		CtrlMenuWin = Ctrl + Menu + Win,
		CtrlShiftWin = Ctrl + Shift + Win,
		MenuShiftWin = Menu + Shift + Win,
		CtrlMenuShiftWin = Ctrl + Menu + Shift + Win,
	}
}
