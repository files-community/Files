// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT;

/// <summary>
/// A read-only vector of <see cref="IFileExplorerAppExtension2"/>, the second revision of the interface. Undocumented.
/// </summary>
[GeneratedComInterface, Guid("0E5F24ED-9BC4-5B7B-9DDB-46150318F868"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IFileExplorerAppExtensionVectorView2
{
	[PreserveSig]
	HRESULT GetIids(out uint iidCount, out /* IID** */ nint iids);

	[PreserveSig]
	HRESULT GetRuntimeClassName([MarshalUsing(typeof(global::Windows.Win32.HStringStringMarshaller))] out string? className);

	[PreserveSig]
	HRESULT GetTrustLevel(out TrustLevel trustLevel);

	[PreserveSig]
	HRESULT GetAt(uint index, out IFileExplorerAppExtension2? extension);

	[PreserveSig]
	HRESULT GetSize(out uint size);
}
