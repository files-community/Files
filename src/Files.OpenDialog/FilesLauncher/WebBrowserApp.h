// Copyright (c) 2023 Files
// Licensed under the MIT License. See the LICENSE.

#pragma once

#include "wil/cppwinrt.h"
#include <winrt/base.h>
#include <exdisp.h>

// The IWebBrowserApp interface is used when finding shell windows.
class WebBrowserApp : public winrt::implements<WebBrowserApp, IWebBrowserApp>
{
public:
	WebBrowserApp(HWND hwnd, IDispatch *document);

	// IWebBrowserApp
	IFACEMETHODIMP Quit();
	IFACEMETHODIMP ClientToWindow(int *width, int *height);
	IFACEMETHODIMP PutProperty(BSTR property, VARIANT value);
	IFACEMETHODIMP GetProperty(BSTR property, VARIANT *value);
	IFACEMETHODIMP get_Name(BSTR *name);
	IFACEMETHODIMP get_HWND(SHANDLE_PTR *hwnd);
	IFACEMETHODIMP get_FullName(BSTR *fullName);
	IFACEMETHODIMP get_Path(BSTR *path);
	IFACEMETHODIMP get_Visible(VARIANT_BOOL *visible);
	IFACEMETHODIMP put_Visible(VARIANT_BOOL visible);
	IFACEMETHODIMP get_StatusBar(VARIANT_BOOL *statusBar);
	IFACEMETHODIMP put_StatusBar(VARIANT_BOOL statusBar);
	IFACEMETHODIMP get_StatusText(BSTR *statusText);
	IFACEMETHODIMP put_StatusText(BSTR statusText);
	IFACEMETHODIMP get_ToolBar(int *toolBar);
	IFACEMETHODIMP put_ToolBar(int toolBar);
	IFACEMETHODIMP get_MenuBar(VARIANT_BOOL *menuBar);
	IFACEMETHODIMP put_MenuBar(VARIANT_BOOL menuBar);
	IFACEMETHODIMP get_FullScreen(VARIANT_BOOL *fullScreen);
	IFACEMETHODIMP put_FullScreen(VARIANT_BOOL fullScreen);

	// IWebBrowser
	IFACEMETHODIMP GoBack();
	IFACEMETHODIMP GoForward();
	IFACEMETHODIMP GoHome();
	IFACEMETHODIMP GoSearch();
	IFACEMETHODIMP Navigate(BSTR url, VARIANT *flags, VARIANT *targetFrameName, VARIANT *postData, VARIANT *headers);
	IFACEMETHODIMP Refresh();
	IFACEMETHODIMP Refresh2(VARIANT *level);
	IFACEMETHODIMP Stop();
	IFACEMETHODIMP get_Application(IDispatch **dispatch);
	IFACEMETHODIMP get_Parent(IDispatch **dispatch);
	IFACEMETHODIMP get_Container(IDispatch **dispatch);
	IFACEMETHODIMP get_Document(IDispatch **dispatch);
	IFACEMETHODIMP get_TopLevelContainer(VARIANT_BOOL *topLevelContainer);
	IFACEMETHODIMP get_Type(BSTR *type);
	IFACEMETHODIMP get_Left(long *left);
	IFACEMETHODIMP put_Left(long left);
	IFACEMETHODIMP get_Top(long *top);
	IFACEMETHODIMP put_Top(long top);
	IFACEMETHODIMP get_Width(long *width);
	IFACEMETHODIMP put_Width(long width);
	IFACEMETHODIMP get_Height(long *height);
	IFACEMETHODIMP put_Height(long height);
	IFACEMETHODIMP get_LocationName(BSTR *locationName);
	IFACEMETHODIMP get_LocationURL(BSTR *locationURL);
	IFACEMETHODIMP get_Busy(VARIANT_BOOL *busy);

	// IDispatch
	IFACEMETHODIMP GetTypeInfoCount(UINT *typeInfoCount);
	IFACEMETHODIMP GetTypeInfo(UINT type, LCID localeId, ITypeInfo **typeInfo);
	IFACEMETHODIMP GetIDsOfNames(REFIID riid, LPOLESTR *names, UINT numNames, LCID localeId, DISPID *dispId);
	IFACEMETHODIMP Invoke(DISPID dispIdMember, REFIID riid, LCID localeId, WORD flags, DISPPARAMS *dispParams, VARIANT *varResult, EXCEPINFO *exceptionInfo, UINT *argErr);

private:
	HWND m_hwnd;

	// This represents the view of the folder, rather than a web document.
	winrt::com_ptr<IDispatch> m_document;
};

namespace winrt
{
	template <>
	bool is_guid_of<IWebBrowserApp>(guid const &id) noexcept;
}
