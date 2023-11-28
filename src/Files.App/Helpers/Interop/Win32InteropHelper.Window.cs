// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using System.Reflection;
using System.Runtime.InteropServices;
using Vanara.PInvoke;

namespace Files.App.Helpers
{
	public static partial class Win32InteropHelper
	{
		[DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLong")]
		private static extern int SetWindowLongPtr32(HWND hWnd, User32.WindowLongFlags nIndex, IntPtr dwNewLong);

		[DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLongPtr")]
		private static extern IntPtr SetWindowLongPtr64(HWND hWnd, User32.WindowLongFlags nIndex, IntPtr dwNewLong);

		public static IntPtr SetWindowLong(HWND hWnd, User32.WindowLongFlags nIndex, IntPtr dwNewLong)
		{
			if (IntPtr.Size == 4)
				return SetWindowLongPtr32(hWnd, nIndex, dwNewLong);
			else
				return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
		}

		public static void ChangeCursor(this UIElement uiElement, InputCursor cursor)
		{
			Type type = typeof(UIElement);

			type.InvokeMember(
				"ProtectedCursor",
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance,
				null,
				uiElement,
				new object[] { cursor }
			);
		}
	}
}
