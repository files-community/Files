// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT;

/// <summary>
/// <c>IVectorView&lt;IPropertySet&gt;</c>; only the methods up to <see cref="GetSize"/> are declared.
/// </summary>
[GeneratedComInterface, Guid("A79C3578-C5C8-5B61-AFC7-8DA842914434"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IPropertySetVectorView
{
	[PreserveSig]
	HRESULT GetIids(out uint iidCount, out /* IID** */ nint iids);

	[PreserveSig]
	HRESULT GetRuntimeClassName([MarshalUsing(typeof(global::Windows.Win32.HStringStringMarshaller))] out string? className);

	[PreserveSig]
	HRESULT GetTrustLevel(out TrustLevel trustLevel);

	/// <param name="value">An <c>IPropertySet</c>, which is also an <see cref="IStringInspectableMap"/>.</param>
	[PreserveSig]
	HRESULT GetAt(uint index, [MarshalAs(UnmanagedType.Interface)] out object? value);

	[PreserveSig]
	HRESULT GetSize(out uint size);
}
