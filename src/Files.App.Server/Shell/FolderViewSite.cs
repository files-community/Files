// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Ole;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Server;

/// <summary>
/// Explorer uses the folder view as the site for commands. Some commands get the target folder from the site rather than the selection, as do all background commands.
/// </summary>
/// <param name="folderPath"></param>
[GeneratedComClass]
internal sealed partial class FolderViewSite(string folderPath) : global::Windows.Win32.System.Com.IServiceProvider, IFolderView, IShellBrowser, IShellView
{
	private static readonly Guid TopLevelBrowserService = new("4C96BE40-915C-11CF-99D3-00AA004AE837");

	public unsafe HRESULT QueryService(Guid* guidService, Guid* riid, out void* ppvObject)
	{
		ppvObject = null;
		if (*guidService != typeof(IFolderView).GUID && *guidService != typeof(IShellBrowser).GUID && *guidService != TopLevelBrowserService)
			return HRESULT.E_NOINTERFACE;

		return QueryInterface(this, riid, out ppvObject);
	}

	public unsafe HRESULT GetFolder(Guid* riid, out void* ppv)
	{
		ppv = null;
		var result = PInvoke.SHCreateItemFromParsingName(folderPath, null, out IShellItem folder);
		if (result.Failed)
			return result;

		if (QueryInterface(folder, riid, out ppv).Succeeded)
			return HRESULT.S_OK;

		var handler = PInvoke.BHID_SFObject;
		return folder.BindToHandler(null, &handler, riid, out ppv);
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

	public HRESULT QueryActiveShellView(out IShellView ppshv)
	{
		ppshv = this;
		return HRESULT.S_OK;
	}

	public unsafe HRESULT GetWindow(HWND* phwnd)
		=> HRESULT.E_NOTIMPL;

	public HRESULT ContextSensitiveHelp(BOOL fEnterMode)
		=> HRESULT.E_NOTIMPL;

	public unsafe HRESULT InsertMenusSB(HMENU hmenuShared, OLEMENUGROUPWIDTHS* lpMenuWidths)
		=> HRESULT.E_NOTIMPL;

	public HRESULT SetMenuSB(HMENU hmenuShared, nint holemenuRes, HWND hwndActiveObject)
		=> HRESULT.E_NOTIMPL;

	public HRESULT RemoveMenusSB(HMENU hmenuShared)
		=> HRESULT.E_NOTIMPL;

	public HRESULT SetStatusTextSB(PCWSTR pszStatusText)
		=> HRESULT.E_NOTIMPL;

	public HRESULT EnableModelessSB(BOOL fEnable)
		=> HRESULT.E_NOTIMPL;

	public unsafe HRESULT TranslateAcceleratorSB(MSG* pmsg, ushort wID)
		=> HRESULT.E_NOTIMPL;

	public unsafe HRESULT BrowseObject(ITEMIDLIST* pidl, uint wFlags)
		=> HRESULT.E_NOTIMPL;

	public HRESULT GetViewStateStream(uint grfMode, out global::Windows.Win32.System.Com.IStream ppStrm)
	{
		ppStrm = null!;
		return HRESULT.E_NOTIMPL;
	}

	public unsafe HRESULT GetControlWindow(uint id, HWND* phwnd)
		=> HRESULT.E_NOTIMPL;

	public unsafe HRESULT SendControlMsg(uint id, uint uMsg, WPARAM wParam, LPARAM lParam, LRESULT* pret)
		=> HRESULT.E_NOTIMPL;

	public HRESULT OnViewWindowActive(IShellView pshv)
		=> HRESULT.E_NOTIMPL;

	public unsafe HRESULT SetToolbarItems(TBBUTTON* lpButtons, uint nButtons, uint uFlags)
		=> HRESULT.E_NOTIMPL;

	public unsafe HRESULT TranslateAccelerator(MSG* pmsg)
		=> HRESULT.E_NOTIMPL;

	public HRESULT EnableModeless(BOOL fEnable)
		=> HRESULT.E_NOTIMPL;

	public HRESULT UIActivate(uint uState)
		=> HRESULT.E_NOTIMPL;

	public HRESULT Refresh()
		=> HRESULT.E_NOTIMPL;

	public unsafe HRESULT CreateViewWindow(IShellView psvPrevious, FOLDERSETTINGS* pfs, IShellBrowser psb, RECT* prcView, HWND* phWnd)
		=> HRESULT.E_NOTIMPL;

	public HRESULT DestroyViewWindow()
		=> HRESULT.E_NOTIMPL;

	public unsafe HRESULT GetCurrentInfo(FOLDERSETTINGS* pfs)
		=> HRESULT.E_NOTIMPL;

	public unsafe HRESULT AddPropertySheetPages(uint dwReserved, delegate* unmanaged[Stdcall]<HPROPSHEETPAGE, LPARAM, BOOL> pfn, LPARAM lparam)
		=> HRESULT.E_NOTIMPL;

	public HRESULT SaveViewState()
		=> HRESULT.E_NOTIMPL;

	public unsafe HRESULT SelectItem(ITEMIDLIST* pidlItem, uint uFlags)
		=> HRESULT.E_NOTIMPL;

	public unsafe HRESULT GetItemObject(_SVGIO uItem, Guid* riid, out void* ppv)
	{
		ppv = null;
		return (uItem & _SVGIO.SVGIO_TYPE_MASK) == _SVGIO.SVGIO_BACKGROUND ? QueryInterface(new FolderViewAutomation(folderPath), riid, out ppv) : HRESULT.E_NOTIMPL;
	}

	private static unsafe HRESULT QueryInterface(object instance, Guid* riid, out void* ppv)
	{
		var unknown = (nint)ComInterfaceMarshaller<object>.ConvertToUnmanaged(instance);
		try
		{
			var result = Marshal.QueryInterface(unknown, *riid, out var pointer);
			ppv = (void*)pointer;
			return (HRESULT)result;
		}
		finally
		{
			Marshal.Release(unknown);
		}
	}
}
