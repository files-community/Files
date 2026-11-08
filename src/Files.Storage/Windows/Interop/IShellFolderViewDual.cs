// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace Files.Storage.Interop;

/// <summary>
/// The scripting object of a folder view
/// </summary>
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper), Guid("E7A1AF80-4D96-11CF-960C-0080C7F4EE85")]
public partial interface IShellFolderViewDual : IDispatch
{
	[PreserveSig]
	HRESULT get_Application(out IDispatch? application);

	[PreserveSig]
	HRESULT get_Parent(out IDispatch? parent);

	[PreserveSig]
	HRESULT get_Folder(out Folder? folder);

	[PreserveSig]
	HRESULT SelectedItems(out IFolderItems? items);

	[PreserveSig]
	HRESULT get_FocusedItem(out FolderItem? item);

	[PreserveSig]
	HRESULT SelectItem(in ComVariant item, int flags);

	[PreserveSig]
	HRESULT PopupItemMenu(FolderItem? item, ComVariant x, ComVariant y, [MarshalAs(UnmanagedType.BStr)] out string? command);

	[PreserveSig]
	HRESULT get_Script(out IDispatch? script);

	[PreserveSig]
	HRESULT get_ViewOptions(out int options);
}

/// <summary>
/// A collection of shell items, such as the selection of a folder view.
/// </summary>
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper), Guid("744129E0-CBE5-11CE-8350-444553540000")]
public partial interface IFolderItems : IDispatch
{
	[PreserveSig]
	HRESULT get_Count(out int count);

	[PreserveSig]
	HRESULT get_Application(out IDispatch? application);

	[PreserveSig]
	HRESULT get_Parent(out IDispatch? parent);

	[PreserveSig]
	HRESULT Item(ComVariant index, out FolderItem? item);

	[PreserveSig]
	HRESULT _NewEnum([MarshalAs(UnmanagedType.Interface)] out object? enumerator);
}
