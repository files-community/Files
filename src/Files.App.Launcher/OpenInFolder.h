// Copyright (c) Files Community
// Licensed under the MIT License.

#pragma once

#include <atomic>
#include <iostream>
#include <objbase.h>
#include <exdisp.h>
#include <propvarutil.h>
#include <shtypes.h>
#include <ShlObj_core.h>
#include <ShObjIdl_core.h>
#include <winrt/base.h>
#include <wil/resource.h>

class OpenInFolder final : public IWebBrowserApp, public IServiceProvider, public IShellView
{
	std::atomic<ULONG> m_referenceCount{ 1 };
	HWND m_hwnd = NULL;
	winrt::com_ptr<IShellWindows> m_shellWindows;
	PIDLIST_ABSOLUTE m_folderPidl = NULL;
	long m_pendingCookie = 0;
	long m_shellWindowCookie = 0;
	bool m_isPendingRegistered = false;
	bool m_isRegistered = false;

	HRESULT NotifyShellOfNavigation(PCIDLIST_ABSOLUTE pidl);
	HRESULT GetSelectedItem(PCUITEMID_CHILD pidlItem, PIDLIST_ABSOLUTE* pidlAbsolute);

	std::wstring m_selectedItem;

public:
	OpenInFolder();
	~OpenInFolder();

	// IUnknown
	HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject) override;
	ULONG STDMETHODCALLTYPE AddRef() override;
	ULONG STDMETHODCALLTYPE Release() override;

	// IDispatch
	HRESULT STDMETHODCALLTYPE GetTypeInfoCount(UINT* pctinfo) override;
	HRESULT STDMETHODCALLTYPE GetTypeInfo(UINT iTInfo, LCID lcid, ITypeInfo** ppTInfo) override;
	HRESULT STDMETHODCALLTYPE GetIDsOfNames(REFIID riid, LPOLESTR* rgszNames, UINT cNames, LCID lcid, DISPID* rgDispId) override;
	HRESULT STDMETHODCALLTYPE Invoke(DISPID dispIdMember, REFIID riid, LCID lcid, WORD wFlags, DISPPARAMS* pDispParams, VARIANT* pVarResult, EXCEPINFO* pExcepInfo, UINT* puArgErr) override;

	// IWebBrowserApp
	HRESULT STDMETHODCALLTYPE GoBack() override;
	HRESULT STDMETHODCALLTYPE GoForward() override;
	HRESULT STDMETHODCALLTYPE GoHome() override;
	HRESULT STDMETHODCALLTYPE GoSearch() override;
	HRESULT STDMETHODCALLTYPE Navigate(BSTR url, VARIANT* flags, VARIANT* targetFrameName, VARIANT* postData, VARIANT* headers) override;
	HRESULT STDMETHODCALLTYPE Refresh2(VARIANT* level) override;
	HRESULT STDMETHODCALLTYPE Stop() override;
	HRESULT STDMETHODCALLTYPE get_Application(IDispatch** ppDisp) override;
	HRESULT STDMETHODCALLTYPE get_Parent(IDispatch** ppDisp) override;
	HRESULT STDMETHODCALLTYPE get_Container(IDispatch** ppDisp) override;
	HRESULT STDMETHODCALLTYPE get_Document(IDispatch** ppDisp) override;
	HRESULT STDMETHODCALLTYPE get_TopLevelContainer(VARIANT_BOOL* pBool) override;
	HRESULT STDMETHODCALLTYPE get_Type(BSTR* type) override;
	HRESULT STDMETHODCALLTYPE get_Left(long* value) override;
	HRESULT STDMETHODCALLTYPE put_Left(long value) override;
	HRESULT STDMETHODCALLTYPE get_Top(long* value) override;
	HRESULT STDMETHODCALLTYPE put_Top(long value) override;
	HRESULT STDMETHODCALLTYPE get_Width(long* value) override;
	HRESULT STDMETHODCALLTYPE put_Width(long value) override;
	HRESULT STDMETHODCALLTYPE get_Height(long* value) override;
	HRESULT STDMETHODCALLTYPE put_Height(long value) override;
	HRESULT STDMETHODCALLTYPE get_LocationName(BSTR* locationName) override;
	HRESULT STDMETHODCALLTYPE get_LocationURL(BSTR* locationUrl) override;
	HRESULT STDMETHODCALLTYPE get_Busy(VARIANT_BOOL* pBool) override;
	HRESULT STDMETHODCALLTYPE Quit() override;
	HRESULT STDMETHODCALLTYPE ClientToWindow(int* width, int* height) override;
	HRESULT STDMETHODCALLTYPE PutProperty(BSTR property, VARIANT value) override;
	HRESULT STDMETHODCALLTYPE GetProperty(BSTR property, VARIANT* value) override;
	HRESULT STDMETHODCALLTYPE get_Name(BSTR* name) override;
	HRESULT STDMETHODCALLTYPE get_HWND(SHANDLE_PTR* hwnd) override;
	HRESULT STDMETHODCALLTYPE get_FullName(BSTR* fullName) override;
	HRESULT STDMETHODCALLTYPE get_Path(BSTR* path) override;
	HRESULT STDMETHODCALLTYPE get_Visible(VARIANT_BOOL* value) override;
	HRESULT STDMETHODCALLTYPE put_Visible(VARIANT_BOOL value) override;
	HRESULT STDMETHODCALLTYPE get_StatusBar(VARIANT_BOOL* value) override;
	HRESULT STDMETHODCALLTYPE put_StatusBar(VARIANT_BOOL value) override;
	HRESULT STDMETHODCALLTYPE get_StatusText(BSTR* statusText) override;
	HRESULT STDMETHODCALLTYPE put_StatusText(BSTR statusText) override;
	HRESULT STDMETHODCALLTYPE get_ToolBar(int* value) override;
	HRESULT STDMETHODCALLTYPE put_ToolBar(int value) override;
	HRESULT STDMETHODCALLTYPE get_MenuBar(VARIANT_BOOL* value) override;
	HRESULT STDMETHODCALLTYPE put_MenuBar(VARIANT_BOOL value) override;
	HRESULT STDMETHODCALLTYPE get_FullScreen(VARIANT_BOOL* value) override;
	HRESULT STDMETHODCALLTYPE put_FullScreen(VARIANT_BOOL value) override;

	// IServiceProvider
	HRESULT STDMETHODCALLTYPE QueryService(REFGUID guidService, REFIID riid, void** ppvObject) override;

	// IOleWindow
	HRESULT STDMETHODCALLTYPE GetWindow(HWND* phwnd) override;
	HRESULT STDMETHODCALLTYPE ContextSensitiveHelp(BOOL fEnterMode) override;

	// IShellView
	HRESULT STDMETHODCALLTYPE TranslateAccelerator(MSG* pmsg) override;
	HRESULT STDMETHODCALLTYPE EnableModeless(BOOL fEnable) override;
	HRESULT STDMETHODCALLTYPE UIActivate(UINT uState) override;
	HRESULT STDMETHODCALLTYPE Refresh() override;
	HRESULT STDMETHODCALLTYPE CreateViewWindow(IShellView* psvPrevious, LPCFOLDERSETTINGS pfs, IShellBrowser* psb, RECT* prcView, HWND* phWnd) override;
	HRESULT STDMETHODCALLTYPE DestroyViewWindow() override;
	HRESULT STDMETHODCALLTYPE GetCurrentInfo(LPFOLDERSETTINGS pfs) override;
	HRESULT STDMETHODCALLTYPE AddPropertySheetPages(DWORD dwReserved, LPFNSVADDPROPSHEETPAGE pfn, LPARAM lparam) override;
	HRESULT STDMETHODCALLTYPE SaveViewState() override;
	HRESULT STDMETHODCALLTYPE SelectItem(PCUITEMID_CHILD pidlItem, SVSIF uFlags) override;
	HRESULT STDMETHODCALLTYPE GetItemObject(UINT uItem, REFIID riid, void** ppv) override;

	LRESULT CALLBACK WindowProcedure(HWND hwnd, UINT Msg, WPARAM wParam, LPARAM lParam);
	void SetWindow(HWND hwnd);
	void OnItemSelected(PIDLIST_ABSOLUTE pidl);
	void OnCreate();
	void RevokeShellWindow();
	std::wstring GetResult();
};
