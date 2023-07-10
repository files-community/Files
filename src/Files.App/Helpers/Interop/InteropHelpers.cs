// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Files.App.Helpers
{
	public static class InteropHelpers
	{
		public static readonly Guid DataTransferManagerInteropIID =
			new(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36, 0x67, 0xd2, 0x5a, 0x8d, 0xa0, 0x0c);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetCursorPos(out POINT point);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		public static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

		[DllImport("kernel32.dll")]
		public static extern bool SetEvent(IntPtr hEvent);

		[DllImport("ole32.dll")]
		public static extern uint CoWaitForMultipleObjects(uint dwFlags, uint dwMilliseconds, ulong nHandles, IntPtr[] pHandles, out uint dwIndex);

		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int X;

			public int Y;

			public POINT(int x, int y) => (X, Y) = (x, y);
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

	[ComImport]
	[Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface IDataTransferManagerInterop
	{
		IntPtr GetForWindow([In] IntPtr appWindow, [In] ref Guid riid);

		void ShowShareUIForWindow(IntPtr appWindow);
	}
}
