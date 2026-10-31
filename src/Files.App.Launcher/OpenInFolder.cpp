// Copyright (c) Files Community
// Licensed under the MIT License.

#include "OpenInFolder.h"

#pragma comment(lib, "oleaut32.lib")

OpenInFolder::OpenInFolder()
{
	m_shellWindows = winrt::create_instance<IShellWindows>(CLSID_ShellWindows, CLSCTX_ALL);
}

HRESULT STDMETHODCALLTYPE OpenInFolder::QueryInterface(REFIID riid, void** ppvObject)
{
	RETURN_HR_IF_NULL(E_POINTER, ppvObject);
	*ppvObject = nullptr;

	if (riid == IID_IUnknown || riid == IID_IDispatch || riid == IID_IWebBrowser || riid == IID_IWebBrowserApp)
		*ppvObject = static_cast<IWebBrowserApp*>(this);
	else if (riid == IID_IServiceProvider)
		*ppvObject = static_cast<IServiceProvider*>(this);
	else if (riid == IID_IShellView || riid == IID_IOleWindow)
		*ppvObject = static_cast<IShellView*>(this);
	else
		return E_NOINTERFACE;

	AddRef();
	return S_OK;
}

ULONG STDMETHODCALLTYPE OpenInFolder::AddRef()
{
	return ++m_referenceCount;
}

ULONG STDMETHODCALLTYPE OpenInFolder::Release()
{
	ULONG referenceCount = --m_referenceCount;
	if (!referenceCount)
		delete this;

	return referenceCount;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::GetTypeInfoCount(UINT* pctinfo)
{
	RETURN_HR_IF_NULL(E_POINTER, pctinfo);
	*pctinfo = 0;
	return S_OK;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::GetTypeInfo(UINT, LCID, ITypeInfo** ppTInfo)
{
	if (ppTInfo)
		*ppTInfo = nullptr;

	return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::GetIDsOfNames(REFIID, LPOLESTR*, UINT, LCID, DISPID*)
{
	return DISP_E_UNKNOWNNAME;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::Invoke(DISPID, REFIID, LCID, WORD, DISPPARAMS*, VARIANT*, EXCEPINFO*, UINT*)
{
	return DISP_E_MEMBERNOTFOUND;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::GoBack()
{
	return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::GoForward()
{
	return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::GoHome()
{
	return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::GoSearch()
{
	return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::Navigate(BSTR, VARIANT*, VARIANT*, VARIANT*, VARIANT*)
{
	return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::Refresh2(VARIANT*)
{
	return S_OK;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::Stop()
{
	return S_OK;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::get_Application(IDispatch** ppDisp)
{
	RETURN_HR_IF_NULL(E_POINTER, ppDisp);
	*ppDisp = static_cast<IWebBrowserApp*>(this);
	AddRef();
	return S_OK;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::get_Parent(IDispatch** ppDisp)
{
	return get_Application(ppDisp);
}

HRESULT STDMETHODCALLTYPE OpenInFolder::get_Container(IDispatch** ppDisp)
{
	return get_Application(ppDisp);
}

HRESULT STDMETHODCALLTYPE OpenInFolder::get_Document(IDispatch** ppDisp)
{
	return get_Application(ppDisp);
}

HRESULT STDMETHODCALLTYPE OpenInFolder::get_TopLevelContainer(VARIANT_BOOL* pBool)
{
	RETURN_HR_IF_NULL(E_POINTER, pBool);
	*pBool = VARIANT_TRUE;
	return S_OK;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::get_Type(BSTR* type)
{
	RETURN_HR_IF_NULL(E_POINTER, type);
	*type = SysAllocString(L"Files");
	return *type ? S_OK : E_OUTOFMEMORY;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::get_Left(long* value)
{
	RETURN_HR_IF_NULL(E_POINTER, value);
	*value = 0;
	return S_OK;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::put_Left(long)
{
	return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::get_Top(long* value)
{
	return get_Left(value);
}

HRESULT STDMETHODCALLTYPE OpenInFolder::put_Top(long)
{
	return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::get_Width(long* value)
{
	return get_Left(value);
}

HRESULT STDMETHODCALLTYPE OpenInFolder::put_Width(long)
{
	return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::get_Height(long* value)
{
	return get_Left(value);
}

HRESULT STDMETHODCALLTYPE OpenInFolder::put_Height(long)
{
	return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::get_LocationName(BSTR* locationName)
{
	RETURN_HR_IF_NULL(E_POINTER, locationName);
	*locationName = SysAllocString(L"");
	return *locationName ? S_OK : E_OUTOFMEMORY;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::get_LocationURL(BSTR* locationUrl)
{
	return get_LocationName(locationUrl);
}

HRESULT STDMETHODCALLTYPE OpenInFolder::get_Busy(VARIANT_BOOL* pBool)
{
	RETURN_HR_IF_NULL(E_POINTER, pBool);
	*pBool = VARIANT_FALSE;
	return S_OK;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::Quit()
{
	PostMessage(m_hwnd, WM_CLOSE, 0, 0);
	return S_OK;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::ClientToWindow(int* width, int* height)
{
	return width && height ? S_OK : E_POINTER;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::PutProperty(BSTR, VARIANT)
{
	return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::GetProperty(BSTR, VARIANT* value)
{
	RETURN_HR_IF_NULL(E_POINTER, value);
	VariantInit(value);
	return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::get_Name(BSTR* name)
{
	return get_Type(name);
}

HRESULT STDMETHODCALLTYPE OpenInFolder::get_HWND(SHANDLE_PTR* hwnd)
{
	RETURN_HR_IF_NULL(E_POINTER, hwnd);
	*hwnd = reinterpret_cast<SHANDLE_PTR>(m_hwnd);
	return S_OK;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::get_FullName(BSTR* fullName)
{
	RETURN_HR_IF_NULL(E_POINTER, fullName);
	wchar_t modulePath[MAX_PATH];
	if (!GetModuleFileNameW(nullptr, modulePath, _countof(modulePath)))
		return HRESULT_FROM_WIN32(GetLastError());

	*fullName = SysAllocString(modulePath);
	return *fullName ? S_OK : E_OUTOFMEMORY;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::get_Path(BSTR* path)
{
	return get_FullName(path);
}

HRESULT STDMETHODCALLTYPE OpenInFolder::get_Visible(VARIANT_BOOL* value)
{
	RETURN_HR_IF_NULL(E_POINTER, value);
	*value = VARIANT_FALSE;
	return S_OK;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::put_Visible(VARIANT_BOOL)
{
	return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::get_StatusBar(VARIANT_BOOL* value)
{
	return get_Visible(value);
}

HRESULT STDMETHODCALLTYPE OpenInFolder::put_StatusBar(VARIANT_BOOL)
{
	return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::get_StatusText(BSTR* statusText)
{
	return get_LocationName(statusText);
}

HRESULT STDMETHODCALLTYPE OpenInFolder::put_StatusText(BSTR)
{
	return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::get_ToolBar(int* value)
{
	RETURN_HR_IF_NULL(E_POINTER, value);
	*value = 0;
	return S_OK;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::put_ToolBar(int)
{
	return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::get_MenuBar(VARIANT_BOOL* value)
{
	return get_Visible(value);
}

HRESULT STDMETHODCALLTYPE OpenInFolder::put_MenuBar(VARIANT_BOOL)
{
	return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::get_FullScreen(VARIANT_BOOL* value)
{
	return get_Visible(value);
}

HRESULT STDMETHODCALLTYPE OpenInFolder::put_FullScreen(VARIANT_BOOL)
{
	return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::QueryService(REFGUID, REFIID riid, void** ppvObject)
{
	RETURN_HR_IF_NULL(E_POINTER, ppvObject);
	*ppvObject = nullptr;
	return QueryInterface(riid, ppvObject);
}

HRESULT STDMETHODCALLTYPE OpenInFolder::GetWindow(HWND* phwnd)
{
	RETURN_HR_IF_NULL(E_POINTER, phwnd);
	*phwnd = m_hwnd;
	return m_hwnd ? S_OK : E_FAIL;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::ContextSensitiveHelp(BOOL)
{
	return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::TranslateAccelerator(MSG*)
{
	return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::EnableModeless(BOOL)
{
	return S_OK;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::UIActivate(UINT)
{
	return S_OK;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::Refresh()
{
	return S_OK;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::CreateViewWindow(IShellView*, LPCFOLDERSETTINGS, IShellBrowser*, RECT*, HWND* phWnd)
{
	if (phWnd)
		*phWnd = NULL;

	return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::DestroyViewWindow()
{
	return S_OK;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::GetCurrentInfo(LPFOLDERSETTINGS pfs)
{
	RETURN_HR_IF_NULL(E_POINTER, pfs);
	*pfs = {};
	return S_OK;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::AddPropertySheetPages(DWORD, LPFNSVADDPROPSHEETPAGE, LPARAM)
{
	return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::SaveViewState()
{
	return S_OK;
}

HRESULT OpenInFolder::GetSelectedItem(PCUITEMID_CHILD pidlItem, PIDLIST_ABSOLUTE* pidlAbsolute)
{
	RETURN_HR_IF_NULL(E_INVALIDARG, pidlItem);
	RETURN_HR_IF_NULL(E_POINTER, pidlAbsolute);
	*pidlAbsolute = nullptr;
	RETURN_HR_IF_NULL(E_UNEXPECTED, m_folderPidl);

	*pidlAbsolute = ILCombine(m_folderPidl, pidlItem);
	return *pidlAbsolute ? S_OK : E_OUTOFMEMORY;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::SelectItem(PCUITEMID_CHILD pidlItem, SVSIF uFlags)
{
	if (!pidlItem || !(uFlags & (SVSI_SELECT | SVSI_FOCUSED | SVSI_ENSUREVISIBLE | SVSI_EDIT)))
		return S_OK;

	PIDLIST_ABSOLUTE pidlAbsolute = nullptr;
	RETURN_IF_FAILED(GetSelectedItem(pidlItem, &pidlAbsolute));

	OnItemSelected(pidlAbsolute);
	CoTaskMemFree(pidlAbsolute);
	return S_OK;
}

HRESULT STDMETHODCALLTYPE OpenInFolder::GetItemObject(UINT, REFIID, void** ppv)
{
	if (ppv)
		*ppv = nullptr;

	return E_NOINTERFACE;
}

void OpenInFolder::SetWindow(HWND hwnd)
{
	m_hwnd = hwnd;
}

void OpenInFolder::OnCreate()
{
	int numArgs = 0;
	LPWSTR* szArglist = CommandLineToArgvW(GetCommandLine(), &numArgs);
	WCHAR openDirectory[MAX_PATH];

	if (numArgs < 2)
	{
		LocalFree(szArglist);
		return;
	}
	else
	{
		wsprintf(openDirectory, L"%s", szArglist[1]);
	}

	LocalFree(szArglist);

	winrt::com_ptr<IShellFolder> desktop;
	if (FAILED(SHGetDesktopFolder(desktop.put())))
		return;

	if (FAILED(desktop->ParseDisplayName(
		nullptr,
		nullptr,
		openDirectory,
		nullptr,
		&m_folderPidl,
		nullptr)))
		return;

	if (!SUCCEEDED(NotifyShellOfNavigation(m_folderPidl)))
		return;
}

LRESULT CALLBACK OpenInFolder::WindowProcedure(HWND hwnd, UINT Msg, WPARAM wParam, LPARAM lParam)
{
	switch (Msg)
	{
	case WM_CREATE:
		OnCreate();
		break;

	case WM_CLOSE:
		DestroyWindow(hwnd);
		break;

	case WM_DESTROY:
		PostQuitMessage(0);
		break;
	}

	return DefWindowProc(hwnd, Msg, wParam, lParam);
}

HRESULT OpenInFolder::NotifyShellOfNavigation(PCIDLIST_ABSOLUTE pidl)
{
	wil::unique_variant pidlVariant;
	RETURN_IF_FAILED(InitVariantFromBuffer(pidl, ILGetSize(pidl), &pidlVariant));

	wil::unique_variant empty;
	RETURN_IF_FAILED(m_shellWindows->RegisterPending(GetCurrentThreadId(), &pidlVariant, &empty, SWC_BROWSER, &m_pendingCookie));
	m_isPendingRegistered = true;
	RETURN_IF_FAILED(m_shellWindows->Register(static_cast<IDispatch*>(this), HandleToLong(m_hwnd), SWC_BROWSER, &m_shellWindowCookie));
	m_isRegistered = true;

	RETURN_IF_FAILED(m_shellWindows->OnNavigate(m_shellWindowCookie, &pidlVariant));

	return S_OK;
}

void OpenInFolder::OnItemSelected(PIDLIST_ABSOLUTE pidl)
{
	IShellItem* item = NULL;
	if (SUCCEEDED(SHCreateItemFromIDList(pidl, IID_IShellItem, (void**)&item)))
	{
		PWSTR pszPath = NULL;
		if (SUCCEEDED(item->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING, &pszPath)))
		{
			m_selectedItem = pszPath;
			PostMessage(m_hwnd, WM_CLOSE, 0, 0);
			CoTaskMemFree(pszPath);
		}

		item->Release();
	}
}

std::wstring OpenInFolder::GetResult()
{
	return m_selectedItem;
}

void OpenInFolder::RevokeShellWindow()
{
	if (!m_shellWindows)
		return;

	if (m_isRegistered)
	{
		m_isRegistered = false;
		m_shellWindows->Revoke(m_shellWindowCookie);
	}

	if (m_isPendingRegistered)
	{
		m_isPendingRegistered = false;

		// Register may complete the pending registration under the same cookie
		if (m_pendingCookie != m_shellWindowCookie)
			m_shellWindows->Revoke(m_pendingCookie);
	}
}

OpenInFolder::~OpenInFolder()
{
	CoTaskMemFree(m_folderPidl);
}
