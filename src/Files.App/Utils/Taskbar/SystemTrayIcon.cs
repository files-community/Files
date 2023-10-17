// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace Files.App.Utils.Taskbar
{
	public class SystemTrayIcon
	{
		public static void TryCreate(
			Guid id,
			Shell32.NIF additionalFlags,
			uint uCallbackMessage,
			nint iconHandle)
		{
			var data = new Shell32.NOTIFYICONDATA
			{
				cbSize = 1024u,
				uFlags =
					additionalFlags |
					Shell32.NIF.NIF_MESSAGE |
					Shell32.NIF.NIF_ICON |
					Shell32.NIF.NIF_TIP |
					Shell32.NIF.NIF_STATE |
					Shell32.NIF.NIF_GUID,
				guidItem = id,
				uCallbackMessage = uCallbackMessage,
				hIcon = new HICON(iconHandle),
				dwState = Shell32.NIS.NIS_HIDDEN,
				dwStateMask = Shell32.NIS.NIS_HIDDEN,
			};

			Shell32.Shell_NotifyIcon(Shell32.NIM.NIM_ADD, in data);
		}
	}
}
