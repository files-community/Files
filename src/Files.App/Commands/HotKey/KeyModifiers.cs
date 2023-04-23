// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

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
		MenuCtrl = Ctrl + Menu,
		CtrlShift = Ctrl + Shift,
		CtrlWin = Ctrl + Win,
		MenuShift = Menu + Shift,
		MenuWin = Menu + Win,
		ShiftWin = Shift + Win,
		MenuCtrlShift = Ctrl + Menu + Shift,
		MenuCtrlWin = Ctrl + Menu + Win,
		CtrlShiftWin = Ctrl + Shift + Win,
		MenuShiftWin = Menu + Shift + Win,
		MenuCtrlShiftWin = Ctrl + Menu + Shift + Win,
	}
}
