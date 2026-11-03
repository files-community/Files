// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Storage.Interop;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace Files.App.Server;

/// <summary>
/// The folder view's scripting object, used by some commands to access the background folder.
/// </summary>
/// <param name="folderPath"></param>
/// <remarks>It has no selection and implements only the members that commands use in practice.</remarks>
[GeneratedComClass]
internal sealed unsafe partial class FolderViewAutomation(string folderPath) : IShellFolderViewDual
{
	private static readonly Guid ShellApplicationClassId = new("13709620-C279-11CE-A49E-444553540000");

	public HRESULT GetTypeInfoCount(uint* count)
		=> DualInterfaceDispatch<IShellFolderViewDual>.GetTypeInfoCount(count);

	public HRESULT GetTypeInfo(uint index, uint localeId, void** typeInfo)
		=> DualInterfaceDispatch<IShellFolderViewDual>.GetTypeInfo(index, typeInfo);

	public HRESULT GetIDsOfNames(Guid* interfaceId, PWSTR* names, uint count, uint localeId, int* dispatchIds)
		=> DualInterfaceDispatch<IShellFolderViewDual>.GetIDsOfNames(names, count, dispatchIds);

	public HRESULT Invoke(int dispatchId, Guid* interfaceId, uint localeId, ushort flags, DISPPARAMS* parameters, ComVariant* result, EXCEPINFO* exceptionInfo, uint* argumentError)
		=> DualInterfaceDispatch<IShellFolderViewDual>.Invoke(this, dispatchId, flags, parameters, result, exceptionInfo, argumentError);

	public HRESULT get_Application(void** application)
		=> GetApplication(application);

	public HRESULT get_Parent(void** parent)
		=> NotImplemented(parent);

	public HRESULT get_Folder(void** folder)
	{
		if (folder is null)
			return HRESULT.E_POINTER;

		*folder = null;
		var result = CreateApplication(out var shell);
		if (result.Failed)
			return result;

		try
		{
			using var path = ComVariant.Create(folderPath);
			result = shell.NameSpace(path, out var value);
			*folder = ComInterfaceMarshaller<Folder>.ConvertToUnmanaged(value);
			Release(value);
			return result;
		}
		catch (Exception ex)
		{
			return (HRESULT)ex.HResult;
		}
		finally
		{
			Release(shell);
		}
	}

	public HRESULT SelectedItems(void** items)
	{
		if (items is null)
			return HRESULT.E_POINTER;

		*items = ComInterfaceMarshaller<IFolderItems>.ConvertToUnmanaged(new EmptyFolderItems());
		return HRESULT.S_OK;
	}

	public HRESULT get_FocusedItem(void** item)
	{
		if (item is null)
			return HRESULT.E_POINTER;

		*item = null;
		return HRESULT.S_FALSE;
	}

	public HRESULT SelectItem(ComVariant* item, int flags)
		=> HRESULT.E_NOTIMPL;

	public HRESULT PopupItemMenu(void* item, ComVariant x, ComVariant y, BSTR* command)
	{
		if (command is not null)
			*command = default;

		return HRESULT.E_NOTIMPL;
	}

	public HRESULT get_Script(void** script)
		=> NotImplemented(script);

	public HRESULT get_ViewOptions(int* options)
	{
		if (options is not null)
			*options = 0;

		return HRESULT.E_NOTIMPL;
	}

	// Shell.Application, the root of the scripting objects
	private static HRESULT CreateApplication(out IShellDispatch shell)
		=> PInvoke.CoCreateInstance(ShellApplicationClassId, null, CLSCTX.CLSCTX_INPROC_SERVER, out shell);

	private static HRESULT GetApplication(void** application)
	{
		if (application is null)
			return HRESULT.E_POINTER;

		*application = null;
		var result = CreateApplication(out var shell);
		if (result.Failed)
			return result;

		*application = ComInterfaceMarshaller<IShellDispatch>.ConvertToUnmanaged(shell);
		Release(shell);
		return HRESULT.S_OK;
	}

	private static HRESULT NotImplemented(void** value)
	{
		if (value is not null)
			*value = null;

		return HRESULT.E_NOTIMPL;
	}

	private static void Release(object? value)
	{
		if (value is ComObject comObject)
			comObject.FinalRelease();
	}

	[GeneratedComClass]
	private sealed partial class EmptyFolderItems : IFolderItems
	{
		public HRESULT GetTypeInfoCount(uint* count)
			=> DualInterfaceDispatch<IFolderItems>.GetTypeInfoCount(count);

		public HRESULT GetTypeInfo(uint index, uint localeId, void** typeInfo)
			=> DualInterfaceDispatch<IFolderItems>.GetTypeInfo(index, typeInfo);

		public HRESULT GetIDsOfNames(Guid* interfaceId, PWSTR* names, uint count, uint localeId, int* dispatchIds)
			=> DualInterfaceDispatch<IFolderItems>.GetIDsOfNames(names, count, dispatchIds);

		public HRESULT Invoke(int dispatchId, Guid* interfaceId, uint localeId, ushort flags, DISPPARAMS* parameters, ComVariant* result, EXCEPINFO* exceptionInfo, uint* argumentError)
			=> DualInterfaceDispatch<IFolderItems>.Invoke(this, dispatchId, flags, parameters, result, exceptionInfo, argumentError);

		public HRESULT get_Count(int* count)
		{
			if (count is null)
				return HRESULT.E_POINTER;

			*count = 0;
			return HRESULT.S_OK;
		}

		public HRESULT get_Application(void** application)
			=> GetApplication(application);

		public HRESULT get_Parent(void** parent)
			=> NotImplemented(parent);

		public HRESULT Item(ComVariant index, void** item)
		{
			if (item is null)
				return HRESULT.E_POINTER;

			*item = null;
			return HRESULT.S_FALSE;
		}

		public HRESULT _NewEnum(void** enumerator)
			=> NotImplemented(enumerator);
	}
}
