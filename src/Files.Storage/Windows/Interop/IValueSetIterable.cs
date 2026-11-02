// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.WinRT;

namespace Files.Storage.Interop;

// Closed WinRT IIDs derived from the SDK's ValueSet runtime-class signature using CsWinRT.
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper), Guid("4CE87327-00C7-52AC-A483-BEA25D17014A")]
public unsafe partial interface IValueSetIterable : IInspectable
{
	[PreserveSig]
	HRESULT First(out IValueSetIterator? iterator);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper), Guid("55FA69DE-9353-5932-8F03-DCF9BD3793D6")]
public unsafe partial interface IValueSetIterator : IInspectable
{
	[PreserveSig]
	HRESULT GetCurrent([MarshalUsing(typeof(ComOrWinRTObjectMarshaller))] out object? valueSet);
	[PreserveSig]
	HRESULT GetHasCurrent([MarshalAs(UnmanagedType.U1)] out bool hasCurrent);
	[PreserveSig]
	HRESULT MoveNext([MarshalAs(UnmanagedType.U1)] out bool hasCurrent);
	[PreserveSig]
	HRESULT GetMany(uint capacity, nint* valueSets, out uint actual);
}
