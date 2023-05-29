// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Backend.Services;
using System.Runtime.InteropServices;
using Vanara.PInvoke;

namespace Files.App.ServicesImplementation.PreviewPopupProviders
{
	public struct COPYDATASTRUCT
	{
		public IntPtr dwData;
		public int cbData;
		public IntPtr lpData;
	}

	public class SeerProProvider : IPreviewPopupProvider
	{
		public static SeerProProvider Instance { get; } = new();

		public async Task TogglePreviewPopup(string path)
		{
			HWND Window = User32.FindWindow("SeerWindowClass", null);
			COPYDATASTRUCT data = new COPYDATASTRUCT();
			data.dwData = 5000;
			data.cbData = (path.Length + 1) * 2;
			data.lpData = Marshal.StringToHGlobalUni(path);
			User32.SendMessage(Window, (uint)User32.WindowMessage.WM_COPYDATA, 0, ref data);
		}

		public async Task SwitchPreview(string path)
		{
			// TODO
		}

		public async Task<bool> DetectAvailability()
		{
			var handle = User32.FindWindow("SeerWindowClass", null).DangerousGetHandle();
			return handle != IntPtr.Zero && handle.ToInt64() != -1;
		}
	}
}
