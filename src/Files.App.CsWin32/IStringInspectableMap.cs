// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT;

/// <summary>
/// <c>IMap&lt;String, Object&gt;</c>, as implemented by <c>IPropertySet</c>; only <see cref="Lookup"/> is declared.
/// </summary>
[GeneratedComInterface, Guid("1B0D3570-0877-5EC2-8A2C-3B9539506ACA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IStringInspectableMap
{
	[PreserveSig]
	HRESULT GetIids(out uint iidCount, out /* IID** */ nint iids);

	[PreserveSig]
	HRESULT GetRuntimeClassName([MarshalUsing(typeof(global::Windows.Win32.HStringStringMarshaller))] out string? className);

	[PreserveSig]
	HRESULT GetTrustLevel(out TrustLevel trustLevel);

	/// <param name="value">The value, an <see cref="IPropertyValue"/> for strings, numbers and GUIDs.</param>
	[PreserveSig]
	HRESULT Lookup(
		[MarshalUsing(typeof(global::Windows.Win32.HStringStringMarshaller))] string? key,
		[MarshalAs(UnmanagedType.Interface)] out object? value);
}
