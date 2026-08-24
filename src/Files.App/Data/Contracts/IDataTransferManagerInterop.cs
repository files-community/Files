// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Files.App.Data.Contracts
{
	[GeneratedComInterface]
	[Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal partial interface IDataTransferManagerInterop
	{
		IntPtr GetForWindow(IntPtr appWindow, in Guid riid);

		void ShowShareUIForWindow(IntPtr appWindow);
	}
}
