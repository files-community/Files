// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT;

/// <summary>
/// <c>Windows.Foundation.IPropertyValue</c>; only the methods up to <see cref="GetGuid"/> are declared.
/// </summary>
[GeneratedComInterface, Guid("4BD682DD-7554-40E9-9A9B-82654EDE7E62"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IPropertyValue
{
	[PreserveSig]
	HRESULT GetIids(out uint iidCount, out /* IID** */ nint iids);

	[PreserveSig]
	HRESULT GetRuntimeClassName([MarshalUsing(typeof(global::Windows.Win32.HStringStringMarshaller))] out string? className);

	[PreserveSig]
	HRESULT GetTrustLevel(out TrustLevel trustLevel);

	[PreserveSig]
	HRESULT GetPropertyType(out int type);

	[PreserveSig]
	HRESULT GetIsNumericScalar([MarshalAs(UnmanagedType.U1)] out bool isNumericScalar);

	[PreserveSig]
	HRESULT GetUInt8(out byte value);

	[PreserveSig]
	HRESULT GetInt16(out short value);

	[PreserveSig]
	HRESULT GetUInt16(out ushort value);

	[PreserveSig]
	HRESULT GetInt32(out int value);

	[PreserveSig]
	HRESULT GetUInt32(out uint value);

	[PreserveSig]
	HRESULT GetInt64(out long value);

	[PreserveSig]
	HRESULT GetUInt64(out ulong value);

	[PreserveSig]
	HRESULT GetSingle(out float value);

	[PreserveSig]
	HRESULT GetDouble(out double value);

	[PreserveSig]
	HRESULT GetChar16(out char value);

	[PreserveSig]
	HRESULT GetBoolean([MarshalAs(UnmanagedType.U1)] out bool value);

	[PreserveSig]
	HRESULT GetString([MarshalUsing(typeof(global::Windows.Win32.HStringStringMarshaller))] out string? value);

	[PreserveSig]
	HRESULT GetGuid(out Guid value);
}
