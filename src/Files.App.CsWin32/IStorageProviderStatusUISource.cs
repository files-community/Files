// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Attributes;
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT
{
	[GeneratedComInterface, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("A306C249-3D66-5E70-9007-E43DF96051FF")]
	public partial interface IStorageProviderStatusUISource
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
		HRESULT GetStatusUI([MarshalAs(UnmanagedType.Interface)] out IStorageProviderStatusUI value);
	}
}
