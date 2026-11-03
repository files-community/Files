// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace Files.Storage.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper), Guid("00020400-0000-0000-C000-000000000046")]
public unsafe partial interface IDispatch
{
	[PreserveSig]
	HRESULT GetTypeInfoCount(uint* count);

	[PreserveSig]
	HRESULT GetTypeInfo(uint index, uint localeId, void** typeInfo);

	[PreserveSig]
	HRESULT GetIDsOfNames(Guid* interfaceId, PWSTR* names, uint count, uint localeId, int* dispatchIds);

	[PreserveSig]
	HRESULT Invoke(int dispatchId, Guid* interfaceId, uint localeId, ushort flags, DISPPARAMS* parameters, ComVariant* result, EXCEPINFO* exceptionInfo, uint* argumentError);
}
