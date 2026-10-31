// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;

namespace Windows.Win32.UI.Shell;

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16), Guid("D11AD862-66DE-4DF4-BF6C-1F5621996AF1"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public unsafe partial interface IOpenControlPanel
{
	[PreserveSig]
	HRESULT Open(PCWSTR name, PCWSTR page, void* site);

	[PreserveSig]
	HRESULT GetPath(string name, nint path, uint pathLength);
}
