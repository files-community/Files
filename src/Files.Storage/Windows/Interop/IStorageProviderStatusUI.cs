// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Storage.Provider;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT;

[GeneratedComInterface, Guid("D6B6A758-198D-5B80-977F-5FF73DA33118"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IStorageProviderStatusUI
{
	[PreserveSig]
	HRESULT GetIids(out uint iidCount, out /* IID** */ nint iids);

	[PreserveSig]
	HRESULT GetRuntimeClassName([MarshalUsing(typeof(global::Windows.Win32.HStringStringMarshaller))] out string? className);

	[PreserveSig]
	HRESULT GetTrustLevel(out TrustLevel trustLevel);

	[PreserveSig]
	HRESULT GetProviderState(out StorageProviderState value);

	[PreserveSig]
	HRESULT PutProviderState(StorageProviderState value);

	[PreserveSig]
	HRESULT GetProviderStateLabel([MarshalUsing(typeof(global::Windows.Win32.HStringStringMarshaller))] out string? value);

	[PreserveSig]
	HRESULT PutProviderStateLabel([MarshalUsing(typeof(global::Windows.Win32.HStringStringMarshaller))] string? value);

	[PreserveSig]
	HRESULT GetProviderStateIcon(out /* Windows.Foundation.Uri** */ nint value);

	[PreserveSig]
	HRESULT PutProviderStateIcon(/* Windows.Foundation.Uri* */ nint value);

	[PreserveSig]
	HRESULT GetSyncStatusCommand(out /* IStorageProviderUICommand** */ nint value);

	[PreserveSig]
	HRESULT PutSyncStatusCommand(/* IStorageProviderUICommand* */ nint value);

	[PreserveSig]
	HRESULT GetQuotaUI([MarshalAs(UnmanagedType.Interface)] out IStorageProviderQuotaUI value);

	[PreserveSig]
	HRESULT PutQuotaUI([MarshalAs(UnmanagedType.Interface)] IStorageProviderQuotaUI value);

	[PreserveSig]
	HRESULT GetMoreInfoUI(out /* IStorageProviderMoreInfoUI** */ nint value);

	[PreserveSig]
	HRESULT PutMoreInfoUI(/* IStorageProviderMoreInfoUI* */ nint value);

	[PreserveSig]
	HRESULT GetProviderPrimaryCommand(out /* IStorageProviderUICommand** */ nint value);

	[PreserveSig]
	HRESULT PutProviderPrimaryCommand(/* IStorageProviderUICommand* */ nint value);

	[PreserveSig]
	HRESULT GetProviderSecondaryCommands(out /* IVector<IStorageProviderUICommand*>** */ nint value);

	[PreserveSig]
	HRESULT PutProviderSecondaryCommands(/* IVector<IStorageProviderUICommand*>* */ nint value);
}
