// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT;

/// <summary>
/// A read-only vector of <see cref="IFileExplorerAppExtension"/>, the first revision of the interface. Undocumented.
/// </summary>
[GeneratedComInterface, Guid("08ED2B5C-6EE2-5C20-B0E1-A1C0C739F17E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IFileExplorerAppExtensionVectorView
{
	[PreserveSig]
	HRESULT GetIids(out uint iidCount, out /* IID** */ nint iids);

	[PreserveSig]
	HRESULT GetRuntimeClassName([MarshalUsing(typeof(global::Windows.Win32.HStringStringMarshaller))] out string? className);

	[PreserveSig]
	HRESULT GetTrustLevel(out TrustLevel trustLevel);

	[PreserveSig]
	HRESULT GetAt(uint index, out IFileExplorerAppExtension? extension);

	[PreserveSig]
	HRESULT GetSize(out uint size);
}
