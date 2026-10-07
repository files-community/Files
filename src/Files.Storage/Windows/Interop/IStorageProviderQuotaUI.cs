// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT
{
	[GeneratedComInterface, Guid("BA6295C3-312E-544F-9FD5-1F81B21F3649"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public partial interface IStorageProviderQuotaUI
	{
		[PreserveSig]
		HRESULT GetIids(out uint iidCount, out nint iids);

		[PreserveSig]
		HRESULT GetRuntimeClassName([MarshalUsing(typeof(global::Windows.Win32.HStringStringMarshaller))] out string? className);

		[PreserveSig]
		HRESULT GetTrustLevel(out TrustLevel trustLevel);

		[PreserveSig]
		HRESULT GetQuotaTotalInBytes(out ulong value);

		[PreserveSig]
		HRESULT PutQuotaTotalInBytes(ulong value);

		[PreserveSig]
		HRESULT GetQuotaUsedInBytes(out ulong value);

		[PreserveSig]
		HRESULT PutQuotaUsedInBytes(ulong value);

		[PreserveSig]
		HRESULT GetQuotaUsedLabel([MarshalUsing(typeof(global::Windows.Win32.HStringStringMarshaller))] out string? value);

		[PreserveSig]
		HRESULT PutQuotaUsedLabel([MarshalUsing(typeof(global::Windows.Win32.HStringStringMarshaller))] string? value);

		[PreserveSig]
		HRESULT GetQuotaUsedColor(out nint value);

		[PreserveSig]
		HRESULT PutQuotaUsedColor(nint value);
	}
}
