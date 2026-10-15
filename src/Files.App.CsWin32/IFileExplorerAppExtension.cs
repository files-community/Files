// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT;

/// <summary>
/// A package's <c>windows.fileExplorerContextMenus</c> registration, the first revision of the interface. Undocumented.
/// </summary>
[GeneratedComInterface, Guid("66C23D78-8E71-51EF-A184-ED7B6E6CDC90"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IFileExplorerAppExtension
{
	[PreserveSig]
	HRESULT GetIids(out uint iidCount, out /* IID** */ nint iids);

	[PreserveSig]
	HRESULT GetRuntimeClassName([MarshalUsing(typeof(global::Windows.Win32.HStringStringMarshaller))] out string? className);

	[PreserveSig]
	HRESULT GetTrustLevel(out TrustLevel trustLevel);

	[PreserveSig]
	HRESULT GetDisplayName([MarshalUsing(typeof(global::Windows.Win32.HStringStringMarshaller))] out string? displayName);

	[PreserveSig]
	HRESULT GetPackageFullName([MarshalUsing(typeof(global::Windows.Win32.HStringStringMarshaller))] out string? packageFullName);

	[PreserveSig]
	HRESULT GetLogo([MarshalAs(UnmanagedType.Interface)] out object? logo);

	/// <param name="verbs">One property set per verb, with the verb's <c>Id</c> (string) and its <c>Verb</c> (CLSID).</param>
	[PreserveSig]
	HRESULT GetVerbs(uint flags, out IPropertySetVectorView? verbs);
}
