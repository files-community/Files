// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace Files.App.Server;

// Explorer uses its folder view as the site for packaged app commands. Some commands, such as PowerToys New+,
// obtain the target folder from the site rather than the selection.
[GeneratedComClass]
internal sealed partial class FolderViewSite(string folderPath) : global::Windows.Win32.System.Com.IServiceProvider, IFolderView
{
	public unsafe HRESULT QueryService(Guid* guidService, Guid* riid, out void* ppvObject)
	{
		ppvObject = null;
		if (*guidService != typeof(IFolderView).GUID)
			return HRESULT.E_NOINTERFACE;

		var unknown = (nint)ComInterfaceMarshaller<object>.ConvertToUnmanaged(this);
		try
		{
			var result = Marshal.QueryInterface(unknown, *riid, out var instance);
			ppvObject = (void*)instance;
			return (HRESULT)result;
		}
		finally
		{
			Marshal.Release(unknown);
		}
	}

	public unsafe HRESULT GetFolder(Guid* riid, out void* ppv)
	{
		ppv = null;
		var result = PInvoke.SHCreateItemFromParsingName(folderPath, null, out IShellItem folder);
		if (result.Failed)
			return result;

		try
		{
			var handler = PInvoke.BHID_SFObject;
			return folder.BindToHandler(null, &handler, riid, out ppv);
		}
		finally
		{
			if ((object)folder is ComObject comObject)
				comObject.FinalRelease();
		}
	}

	public HRESULT GetCurrentViewMode(out uint pViewMode)
	{
		pViewMode = 0;
		return HRESULT.E_NOTIMPL;
	}

	public HRESULT SetCurrentViewMode(uint ViewMode)
		=> HRESULT.E_NOTIMPL;

	public unsafe HRESULT Item(int iItemIndex, ITEMIDLIST** ppidl)
		=> HRESULT.E_NOTIMPL;

	public HRESULT ItemCount(_SVGIO uFlags, out int pcItems)
	{
		pcItems = 0;
		return HRESULT.E_NOTIMPL;
	}

	public unsafe HRESULT Items(_SVGIO uFlags, Guid* riid, out void* ppv)
	{
		ppv = null;
		return HRESULT.E_NOTIMPL;
	}

	public HRESULT GetSelectionMarkedItem(out int piItem)
	{
		piItem = -1;
		return HRESULT.E_NOTIMPL;
	}

	public HRESULT GetFocusedItem(out int piItem)
	{
		piItem = -1;
		return HRESULT.E_NOTIMPL;
	}

	public unsafe HRESULT GetItemPosition(ITEMIDLIST* pidl, global::System.Drawing.Point* ppt)
		=> HRESULT.E_NOTIMPL;

	public unsafe HRESULT GetSpacing(global::System.Drawing.Point* ppt)
		=> HRESULT.E_NOTIMPL;

	public unsafe HRESULT GetDefaultSpacing(global::System.Drawing.Point* ppt)
		=> HRESULT.E_NOTIMPL;

	public HRESULT GetAutoArrange()
		=> HRESULT.E_NOTIMPL;

	public HRESULT SelectItem(int iItem, uint dwFlags)
		=> HRESULT.E_NOTIMPL;

	public unsafe HRESULT SelectAndPositionItems(uint cidl, ITEMIDLIST** apidl, global::System.Drawing.Point* apt, uint dwFlags)
		=> HRESULT.E_NOTIMPL;
}
