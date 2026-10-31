// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;
using WinRT;

namespace Windows.Win32.System.WinRT;

[GeneratedComInterface, Guid("A306C249-3D66-5E70-9007-E43DF96051FF"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IStorageProviderStatusUISource
{
	[PreserveSig]
	HRESULT GetIids(out uint iidCount, out /* IID** */ nint iids);

	[PreserveSig]
	HRESULT GetRuntimeClassName([MarshalUsing(typeof(global::Windows.Win32.HStringStringMarshaller))] out string? className);

	[PreserveSig]
	HRESULT GetTrustLevel(out TrustLevel trustLevel);

	[PreserveSig]
	HRESULT GetStatusUI([MarshalAs(UnmanagedType.Interface)] out IStorageProviderStatusUI result);

	[PreserveSig]
	HRESULT AddStatusUIChanged(
		/* TypedEventHandler<IStorageProviderStatusUISource*, IInspectable*>* */ nint handler,
		out EventRegistrationToken token);

	[PreserveSig]
	HRESULT RemoveStatusUIChanged(EventRegistrationToken token);
}
