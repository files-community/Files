// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;

namespace Windows.Win32.UI.Shell;

[GeneratedComInterface, Guid("1FDA955C-61FF-11DA-978C-0008744FAAB7"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IDetectionAndSharing
{
	[PreserveSig]
	HRESULT GetStatus(DTSH_TYPE type, out DTSH_STATE state, out DTSH_ACTION action);

	[PreserveSig]
	int TurnOn(nint hwnd, DTSH_TYPE type, int value);

	[PreserveSig]
	int GetCurrentFwProfile(out /*NetFwProfileType2*/ int profile);

	[PreserveSig]
	int GetStatusForProfile(/*NetFwProfileType2*/ int profile, DTSH_TYPE type, out DTSH_STATE state, out DTSH_ACTION action);

	[PreserveSig]
	int TurnOnForProfile(nint hwnd, /*NetFwProfileType2*/ int profile, DTSH_TYPE type, int value);
}

public enum DTSH_TYPE
{
	DTSH_NETWORK_DISCOVERY = 0,
	DTSH_FILE_SHARING = 1,
}

public enum DTSH_STATE
{
	DTSH_OFF = 0,
	DTSH_ON = 1,
}

public enum DTSH_ACTION
{
	DTSH_NONE = 0,
}
