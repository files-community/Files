// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace Files.Storage.Interop;

/// <summary>
/// IClassFactory, creating uniquely owned instances.
/// </summary>
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper), Guid("00000001-0000-0000-C000-000000000046")]
public partial interface IUniqueClassFactory
{
	[PreserveSig]
	HRESULT CreateInstance([MarshalAs(UnmanagedType.Interface)] object? outer, in Guid interfaceId, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<object>))] out object? instance);

	[PreserveSig]
	HRESULT LockServer(BOOL @lock);
}

/// <summary>
/// IAgileReference, resolving to uniquely owned instances.
/// </summary>
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper), Guid("C03F6A43-65A4-9818-987E-E0B810D2A6F2")]
public partial interface IUniqueAgileReference
{
	[PreserveSig]
	HRESULT Resolve(in Guid interfaceId, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<object>))] out object? instance);
}

/// <summary>
/// IShellItemArray, binding to uniquely owned handlers.
/// </summary>
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper), Guid("B63EA76D-1F85-456F-A19C-48159EFA858B")]
public partial interface IUniqueShellItemArray
{
	[PreserveSig]
	HRESULT BindToHandler([MarshalAs(UnmanagedType.Interface)] IBindCtx? bindContext, in Guid handlerId, in Guid interfaceId, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<object>))] out object? handler);
}

/// <summary>
/// IShellItem, binding to uniquely owned handlers.
/// </summary>
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper), Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
public partial interface IUniqueShellItem
{
	[PreserveSig]
	HRESULT BindToHandler([MarshalAs(UnmanagedType.Interface)] IBindCtx? bindContext, in Guid handlerId, in Guid interfaceId, [MarshalUsing(typeof(UniqueComInterfaceMarshaller<object>))] out object? handler);
}
