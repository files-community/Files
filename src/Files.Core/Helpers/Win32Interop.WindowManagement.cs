// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Xml.Linq;

namespace Files.Core.Helpers
{
	public partial class Win32Interop
	{
		public static readonly Guid DataTransferManagerInteropIID =
			new(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36, 0x67, 0xd2, 0x5a, 0x8d, 0xa0, 0x0c);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern bool SetPropW(IntPtr hWnd, string lpString, IntPtr hData);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetCursorPos(out POINT point);

		[DllImport("kernel32.dll")]
		public static extern bool SetEvent(IntPtr hEvent);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern void SwitchToThisWindow(IntPtr hWnd, bool altTab);

		[DllImport("ole32.dll")]
		public static extern uint CoWaitForMultipleObjects(uint dwFlags, uint dwMilliseconds, ulong nHandles, IntPtr[] pHandles, out uint dwIndex);

		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int X;

			public int Y;

			public POINT(int x, int y) => (X, Y) = (x, y);
		}


		[DllImport("User32.dll")]
		public extern static short GetKeyState(int n);
	}
}
