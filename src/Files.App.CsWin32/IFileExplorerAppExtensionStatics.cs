// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT;

/// <summary>
/// Statics of the internal <c>Windows.Internal.FileExplorerAppExtension</c> class (windows.storage.dll), which lists the
/// <c>windows.fileExplorerContextMenus</c> registrations of the installed packages. Undocumented.
/// </summary>
[GeneratedComInterface, Guid("104C1AFF-F09F-5AA1-945F-78737EE0FE45"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface IFileExplorerAppExtensionStatics
{
	[PreserveSig]
	HRESULT GetIids(out uint iidCount, out /* IID** */ nint iids);

	[PreserveSig]
	HRESULT GetRuntimeClassName([MarshalUsing(typeof(global::Windows.Win32.HStringStringMarshaller))] out string? className);

	[PreserveSig]
	HRESULT GetTrustLevel(out TrustLevel trustLevel);

	/// <summary>
	/// Gets the extensions registered for an item type, e.g. <c>*</c>, <c>.txt</c>, <c>Directory</c> or <c>Directory\Background</c>.
	/// </summary>
	/// <param name="flags">1 waits until the registrations have been read; 0 returns what has been read so far.</param>
	/// <param name="extensions">An <see cref="IFileExplorerAppExtensionVectorView2"/> or an <see cref="IFileExplorerAppExtensionVectorView"/>.</param>
	[PreserveSig]
	HRESULT GetExtensions(
		[MarshalUsing(typeof(global::Windows.Win32.HStringStringMarshaller))] string? itemType,
		uint flags,
		[MarshalAs(UnmanagedType.Interface)] out object? extensions);
}
