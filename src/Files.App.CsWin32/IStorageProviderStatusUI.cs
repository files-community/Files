// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Storage.Provider;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT
{
	[GeneratedComInterface, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("D6B6A758-198D-5B80-977F-5FF73DA33118")]
	public partial interface IStorageProviderStatusUI
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
		HRESULT get_ProviderState(out StorageProviderState value);

		// Slot: 7
		[PreserveSig]
		[return: MarshalAs(UnmanagedType.Error)]
		HRESULT put_ProviderState(StorageProviderState value);

		// Slot: 8
		[PreserveSig]
		[return: MarshalAs(UnmanagedType.Error)]
		HRESULT get_ProviderStateLabel(out HSTRING value);

		// Slot: 9
		[PreserveSig]
		[return: MarshalAs(UnmanagedType.Error)]
		HRESULT put_ProviderStateLabel(HSTRING value);

		// Slot: 10
		[PreserveSig]
		[return: MarshalAs(UnmanagedType.Error)]
		HRESULT get_ProviderStateIcon([MarshalAs(UnmanagedType.Interface)] out object /*IUriRuntimeClass*/ value);

		// Slot: 11
		[PreserveSig]
		[return: MarshalAs(UnmanagedType.Error)]
		HRESULT put_ProviderStateIcon([MarshalAs(UnmanagedType.Interface)] object /*IUriRuntimeClass*/ value);

		// Slot: 12
		[PreserveSig]
		[return: MarshalAs(UnmanagedType.Error)]
		HRESULT get_SyncStatusCommand([MarshalAs(UnmanagedType.Interface)] out IStorageProviderUICommand value);

		// Slot: 13
		[PreserveSig]
		[return: MarshalAs(UnmanagedType.Error)]
		HRESULT put_SyncStatusCommand([MarshalAs(UnmanagedType.Interface)] IStorageProviderUICommand value);

		// Slot: 14
		[PreserveSig]
		[return: MarshalAs(UnmanagedType.Error)]
		HRESULT get_QuotaUI([MarshalAs(UnmanagedType.Interface)] out IStorageProviderQuotaUI value);
	}
}
