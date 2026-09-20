// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT;

[GeneratedComInterface, Guid("12E46B74-4E5A-58D1-A62F-0376E8EE7DD8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IStorageProviderStatusUISourceFactory
{
	[PreserveSig]
	HRESULT GetIids(out uint iidCount, out /* IID** */ nint iids);

	[PreserveSig]
	HRESULT GetRuntimeClassName([MarshalUsing(typeof(global::Windows.Win32.HStringStringMarshaller))] out string? className);

	[PreserveSig]
	HRESULT GetTrustLevel(out TrustLevel trustLevel);

	[PreserveSig]
	HRESULT GetStatusUISource(
		[MarshalUsing(typeof(global::Windows.Win32.HStringStringMarshaller))] string? syncRootId,
		[MarshalAs(UnmanagedType.Interface)] out IStorageProviderStatusUISource result);
}
