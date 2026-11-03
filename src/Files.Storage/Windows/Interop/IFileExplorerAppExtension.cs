// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Files.Storage.Interop;

// Windows.Internal.FileExplorerAppExtension is optional. Query the revision's IID before using it.
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper), Guid("104C1AFF-F09F-5AA1-945F-78737EE0FE45")]
public partial interface IFileExplorerAppExtensionStatics : IInspectable
{
	[PreserveSig]
	HRESULT GetExtensions([MarshalUsing(typeof(HStringStringMarshaller))] string itemType, uint flags, out IInspectable? extensions);

	[PreserveSig]
	HRESULT GetAllExtensions(uint flags, out IInspectable? extensions);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper), Guid("66C23D78-8E71-51EF-A184-ED7B6E6CDC90")]
public partial interface IFileExplorerAppExtension : IInspectable
{
	[PreserveSig]
	HRESULT GetDisplayName([MarshalUsing(typeof(HStringStringMarshaller))] out string? name);
	[PreserveSig]
	HRESULT GetPackageFullName([MarshalUsing(typeof(HStringStringMarshaller))] out string? name);
	[PreserveSig]
	HRESULT GetLogo([MarshalUsing(typeof(HStringStringMarshaller))] out string? path);
	[PreserveSig]
	HRESULT GetVerbs(uint flags, out IValueSetIterable? verbs);
	[PreserveSig]
	HRESULT GetVerbsEx(uint flags, out IInspectable? verbs);
	[PreserveSig]
	HRESULT GetConditions(out IInspectable? conditions);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper), Guid("EB16945B-3A5D-5A8A-B666-8DFE27E5C818")]
public partial interface IFileExplorerAppExtension2 : IInspectable
{
	[PreserveSig]
	HRESULT GetDisplayName([MarshalUsing(typeof(HStringStringMarshaller))] out string? name);
	[PreserveSig]
	HRESULT GetPackageFullName([MarshalUsing(typeof(HStringStringMarshaller))] out string? name);
	[PreserveSig]
	HRESULT GetLogo([MarshalUsing(typeof(HStringStringMarshaller))] out string? path);
	[PreserveSig]
	HRESULT GetIsL1Default([MarshalAs(UnmanagedType.U1)] out bool isDefault);
	[PreserveSig]
	HRESULT GetVerbs(uint flags, out IValueSetIterable? verbs);
	[PreserveSig]
	HRESULT GetVerbsEx(uint flags, out IInspectable? verbs);
	[PreserveSig]
	HRESULT GetConditions(out IInspectable? conditions);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper), Guid("08ED2B5C-6EE2-5C20-B0E1-A1C0C739F17E")]
public partial interface IFileExplorerAppExtensionVectorView : IInspectable
{
	[PreserveSig]
	HRESULT GetAt(uint index, out IFileExplorerAppExtension? extension);
	[PreserveSig]
	HRESULT GetSize(out uint size);
	[PreserveSig]
	HRESULT IndexOf(IFileExplorerAppExtension? extension, out uint index, [MarshalAs(UnmanagedType.U1)] out bool found);
	[PreserveSig]
	HRESULT GetMany(uint startIndex, uint capacity, [Out, MarshalUsing(CountElementName = nameof(capacity))] IFileExplorerAppExtension?[] extensions, out uint actual);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper), Guid("0E5F24ED-9BC4-5B7B-9DDB-46150318F868")]
public partial interface IFileExplorerAppExtensionVectorView2 : IInspectable
{
	[PreserveSig]
	HRESULT GetAt(uint index, out IFileExplorerAppExtension2? extension);
	[PreserveSig]
	HRESULT GetSize(out uint size);
	[PreserveSig]
	HRESULT IndexOf(IFileExplorerAppExtension2? extension, out uint index, [MarshalAs(UnmanagedType.U1)] out bool found);
	[PreserveSig]
	HRESULT GetMany(uint startIndex, uint capacity, [Out, MarshalUsing(CountElementName = nameof(capacity))] IFileExplorerAppExtension2?[] extensions, out uint actual);
}
