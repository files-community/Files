// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace Files.Storage.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper | ComInterfaceOptions.ComObjectWrapper), Guid("00020400-0000-0000-C000-000000000046")]
public partial interface IDispatch
{
	[PreserveSig]
	HRESULT GetTypeInfoCount(out uint count);

	[PreserveSig]
	HRESULT GetTypeInfo(uint index, uint localeId, out ITypeInfo? typeInfo);

	[PreserveSig]
	HRESULT GetIDsOfNames(in Guid interfaceId, [MarshalUsing(CountElementName = nameof(count))] PWSTR[] names, uint count, uint localeId, [Out, MarshalUsing(CountElementName = nameof(count))] int[] dispatchIds);

	[PreserveSig]
	HRESULT Invoke(int dispatchId, in Guid interfaceId, uint localeId, ushort flags, ref DISPPARAMS parameters, /* VARIANT* */ nint result, /* EXCEPINFO* */ nint exceptionInfo, /* UINT* */ nint argumentError);
}
