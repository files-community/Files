// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT;

/// <summary>
/// A package's <c>windows.fileExplorerContextMenus</c> registration, the second revision of the interface,
/// which has <see cref="GetIsL1Default"/> before <see cref="GetVerbs"/>. Undocumented.
/// </summary>
[GeneratedComInterface, Guid("EB16945B-3A5D-5A8A-B666-8DFE27E5C818"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IFileExplorerAppExtension2
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

	[PreserveSig]
	HRESULT GetIsL1Default([MarshalAs(UnmanagedType.U1)] out bool isDefault);

	/// <param name="verbs">One property set per verb, with the verb's <c>Id</c> (string) and its <c>Verb</c> (CLSID).</param>
	[PreserveSig]
	HRESULT GetVerbs(uint flags, out IPropertySetVectorView? verbs);
}
