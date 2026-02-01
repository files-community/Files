// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT
{
	[GeneratedComInterface, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("12E46B74-4E5A-58D1-A62F-0376E8EE7DD8")]
	public partial interface IStorageProviderQuotaUI
	{
		// Slot: 3
		[PreserveSig]
		[return: MarshalAs(UnmanagedType.Error)]
		HRESULT GetIids(out uint iidCount, out nint iids);

		// Slot: 4
		[PreserveSig]
		[return: MarshalAs(UnmanagedType.Error)]
		HRESULT GetRuntimeClassName(out HSTRING className);

		// Slot: 5
		[PreserveSig]
		[return: MarshalAs(UnmanagedType.Error)]
		HRESULT GetTrustLevel(out TrustLevel trustLevel);

		// Slot: 6
		[PreserveSig]
		[return: MarshalAs(UnmanagedType.Error)]
		HRESULT get_QuotaTotalInBytes(out ulong value);

		// Slot: 7
		[PreserveSig]
		[return: MarshalAs(UnmanagedType.Error)]
		HRESULT put_QuotaTotalInBytes(ulong value);

		// Slot: 8
		[PreserveSig]
		[return: MarshalAs(UnmanagedType.Error)]
		HRESULT get_QuotaUsedInBytes(out ulong value);
	}
}
