// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;

namespace Files.Storage.Interop;

/// <summary>
/// The scripting object of a folder view
/// </summary>
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper), Guid("E7A1AF80-4D96-11CF-960C-0080C7F4EE85")]
public unsafe partial interface IShellFolderViewDual : IDispatch
{
	[PreserveSig]
	HRESULT get_Application(void** application);

	[PreserveSig]
	HRESULT get_Parent(void** parent);

	[PreserveSig]
	HRESULT get_Folder(void** folder);

	[PreserveSig]
	HRESULT SelectedItems(void** items);

	[PreserveSig]
	HRESULT get_FocusedItem(void** item);

	[PreserveSig]
	HRESULT SelectItem(ComVariant* item, int flags);

	[PreserveSig]
	HRESULT PopupItemMenu(void* item, ComVariant x, ComVariant y, BSTR* command);

	[PreserveSig]
	HRESULT get_Script(void** script);

	[PreserveSig]
	HRESULT get_ViewOptions(int* options);
}

/// <summary>
/// A collection of shell items, such as the selection of a folder view.
/// </summary>
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper), Guid("744129E0-CBE5-11CE-8350-444553540000")]
public unsafe partial interface IFolderItems : IDispatch
{
	[PreserveSig]
	HRESULT get_Count(int* count);

	[PreserveSig]
	HRESULT get_Application(void** application);

	[PreserveSig]
	HRESULT get_Parent(void** parent);

	[PreserveSig]
	HRESULT Item(ComVariant index, void** item);

	[PreserveSig]
	HRESULT _NewEnum(void** enumerator);
}
