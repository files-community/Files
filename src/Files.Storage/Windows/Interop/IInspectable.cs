// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.WinRT;

namespace Files.Storage.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper), Guid("AF86E2E0-B12D-4C6A-9C5A-D7AA65101E90")]
public partial interface IInspectable
{
	[PreserveSig]
	HRESULT GetIids(out uint iidCount, [MarshalUsing(CountElementName = nameof(iidCount))] out Guid[]? iids);

	[PreserveSig]
	HRESULT GetRuntimeClassName([MarshalUsing(typeof(HStringStringMarshaller))] out string? className);

	[PreserveSig]
	HRESULT GetTrustLevel(out TrustLevel trustLevel);
}
