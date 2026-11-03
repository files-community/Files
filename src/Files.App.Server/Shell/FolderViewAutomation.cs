// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Storage.Interop;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using IDispatch = Files.Storage.Interop.IDispatch;

namespace Files.App.Server;

/// <summary>
/// The folder view's scripting object, used by some commands to access the background folder.
/// </summary>
/// <param name="folderPath"></param>
/// <remarks>It has no selection and implements only the members that commands use in practice.</remarks>
[GeneratedComClass]
internal sealed partial class FolderViewAutomation(string folderPath) : IShellFolderViewDual
{
	private static readonly Guid ShellApplicationClassId = new("13709620-C279-11CE-A49E-444553540000");

	public HRESULT GetTypeInfoCount(out uint count)
		=> DualInterfaceDispatch<IShellFolderViewDual>.GetTypeInfoCount(out count);

	public HRESULT GetTypeInfo(uint index, uint localeId, out ITypeInfo? typeInfo)
		=> DualInterfaceDispatch<IShellFolderViewDual>.GetTypeInfo(index, out typeInfo);

	public HRESULT GetIDsOfNames(in Guid interfaceId, PWSTR[] names, uint count, uint localeId, int[] dispatchIds)
		=> DualInterfaceDispatch<IShellFolderViewDual>.GetIDsOfNames(names, dispatchIds);

	public HRESULT Invoke(int dispatchId, in Guid interfaceId, uint localeId, ushort flags, ref DISPPARAMS parameters, nint result, nint exceptionInfo, nint argumentError)
		=> DualInterfaceDispatch<IShellFolderViewDual>.Invoke(this, dispatchId, flags, ref parameters, result, exceptionInfo, argumentError);

	public HRESULT get_Application(out IDispatch? application)
		=> CreateApplication(out application);

	public HRESULT get_Parent(out IDispatch? parent)
	{
		parent = null;
		return HRESULT.E_NOTIMPL;
	}

	public HRESULT get_Folder(out Folder? folder)
	{
		folder = null;
		var result = CreateApplication(out IShellDispatch? shell);
		if (result.Failed || shell is null)
			return result;

		using var path = ComVariant.Create(folderPath);
		return shell.NameSpace(path, out folder);
	}

	public HRESULT SelectedItems(out IFolderItems? items)
	{
		items = new EmptyFolderItems();
		return HRESULT.S_OK;
	}

	public HRESULT get_FocusedItem(out FolderItem? item)
	{
		item = null;
		return HRESULT.S_FALSE;
	}

	public HRESULT SelectItem(in ComVariant item, int flags)
		=> HRESULT.E_NOTIMPL;

	public HRESULT PopupItemMenu(FolderItem? item, ComVariant x, ComVariant y, out string? command)
	{
		command = null;
		return HRESULT.E_NOTIMPL;
	}

	public HRESULT get_Script(out IDispatch? script)
	{
		script = null;
		return HRESULT.E_NOTIMPL;
	}

	public HRESULT get_ViewOptions(out int options)
	{
		options = 0;
		return HRESULT.E_NOTIMPL;
	}

	// Shell.Application, the root of the scripting objects
	private static HRESULT CreateApplication<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>(out T? application) where T : class
		=> PInvoke.CoCreateInstance(ShellApplicationClassId, null, CLSCTX.CLSCTX_INPROC_SERVER, out application);

	[GeneratedComClass]
	private sealed partial class EmptyFolderItems : IFolderItems
	{
		public HRESULT GetTypeInfoCount(out uint count)
			=> DualInterfaceDispatch<IFolderItems>.GetTypeInfoCount(out count);

		public HRESULT GetTypeInfo(uint index, uint localeId, out ITypeInfo? typeInfo)
			=> DualInterfaceDispatch<IFolderItems>.GetTypeInfo(index, out typeInfo);

		public HRESULT GetIDsOfNames(in Guid interfaceId, PWSTR[] names, uint count, uint localeId, int[] dispatchIds)
			=> DualInterfaceDispatch<IFolderItems>.GetIDsOfNames(names, dispatchIds);

		public HRESULT Invoke(int dispatchId, in Guid interfaceId, uint localeId, ushort flags, ref DISPPARAMS parameters, nint result, nint exceptionInfo, nint argumentError)
			=> DualInterfaceDispatch<IFolderItems>.Invoke(this, dispatchId, flags, ref parameters, result, exceptionInfo, argumentError);

		public HRESULT get_Count(out int count)
		{
			count = 0;
			return HRESULT.S_OK;
		}

		public HRESULT get_Application(out IDispatch? application)
			=> CreateApplication(out application);

		public HRESULT get_Parent(out IDispatch? parent)
		{
			parent = null;
			return HRESULT.E_NOTIMPL;
		}

		public HRESULT Item(ComVariant index, out FolderItem? item)
		{
			item = null;
			return HRESULT.S_FALSE;
		}

		public HRESULT _NewEnum(out object? enumerator)
		{
			enumerator = null;
			return HRESULT.E_NOTIMPL;
		}
	}
}
