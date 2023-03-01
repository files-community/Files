// Copyright (c) 2023 Files
// Licensed under the MIT License. See the LICENSE.

#include "WebBrowserApp.h"
#include <iostream>

WebBrowserApp::WebBrowserApp(HWND hwnd, IDispatch *document) : m_hwnd(hwnd)
{
	m_document.copy_from(document);
}

// IWebBrowserApp
IFACEMETHODIMP WebBrowserApp::Quit()
{
	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::ClientToWindow(int *width, int *height)
{
	UNREFERENCED_PARAMETER(width);
	UNREFERENCED_PARAMETER(height);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::PutProperty(BSTR property, VARIANT value)
{
	UNREFERENCED_PARAMETER(property);
	UNREFERENCED_PARAMETER(value);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::GetProperty(BSTR property, VARIANT *value)
{
	UNREFERENCED_PARAMETER(property);
	UNREFERENCED_PARAMETER(value);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::get_Name(BSTR *name)
{
	UNREFERENCED_PARAMETER(name);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::get_HWND(SHANDLE_PTR *hwnd)
{
	*hwnd = reinterpret_cast<SHANDLE_PTR>(m_hwnd);

	return S_OK;
}

IFACEMETHODIMP WebBrowserApp::get_FullName(BSTR *fullName)
{
	UNREFERENCED_PARAMETER(fullName);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::get_Path(BSTR *path)
{
	UNREFERENCED_PARAMETER(path);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::get_Visible(VARIANT_BOOL *visible)
{
	UNREFERENCED_PARAMETER(visible);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::put_Visible(VARIANT_BOOL visible)
{
	UNREFERENCED_PARAMETER(visible);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::get_StatusBar(VARIANT_BOOL *statusBar)
{
	UNREFERENCED_PARAMETER(statusBar);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::put_StatusBar(VARIANT_BOOL statusBar)
{
	UNREFERENCED_PARAMETER(statusBar);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::get_StatusText(BSTR *statusText)
{
	UNREFERENCED_PARAMETER(statusText);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::put_StatusText(BSTR statusText)
{
	UNREFERENCED_PARAMETER(statusText);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::get_ToolBar(int *toolBar)
{
	UNREFERENCED_PARAMETER(toolBar);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::put_ToolBar(int toolBar)
{
	UNREFERENCED_PARAMETER(toolBar);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::get_MenuBar(VARIANT_BOOL *menuBar)
{
	UNREFERENCED_PARAMETER(menuBar);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::put_MenuBar(VARIANT_BOOL menuBar)
{
	UNREFERENCED_PARAMETER(menuBar);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::get_FullScreen(VARIANT_BOOL *fullScreen)
{
	UNREFERENCED_PARAMETER(fullScreen);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::put_FullScreen(VARIANT_BOOL fullScreen)
{
	UNREFERENCED_PARAMETER(fullScreen);

	return E_NOTIMPL;
}

// IWebBrowser
IFACEMETHODIMP WebBrowserApp::GoBack()
{
	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::GoForward()
{
	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::GoHome()
{
	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::GoSearch()
{
	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::Navigate(BSTR url, VARIANT *flags, VARIANT *targetFrameName, VARIANT *postData, VARIANT *headers)
{
	UNREFERENCED_PARAMETER(url);
	UNREFERENCED_PARAMETER(flags);
	UNREFERENCED_PARAMETER(targetFrameName);
	UNREFERENCED_PARAMETER(postData);
	UNREFERENCED_PARAMETER(headers);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::Refresh()
{
	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::Refresh2(VARIANT *level)
{
	UNREFERENCED_PARAMETER(level);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::Stop()
{
	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::get_Application(IDispatch **dispatch)
{
	UNREFERENCED_PARAMETER(dispatch);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::get_Parent(IDispatch **dispatch)
{
	UNREFERENCED_PARAMETER(dispatch);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::get_Container(IDispatch **dispatch)
{
	UNREFERENCED_PARAMETER(dispatch);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::get_Document(IDispatch **dispatch)
{
	m_document.copy_to(dispatch);
	return S_OK;
}

IFACEMETHODIMP WebBrowserApp::get_TopLevelContainer(VARIANT_BOOL *topLevelContainer)
{
	UNREFERENCED_PARAMETER(topLevelContainer);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::get_Type(BSTR *type)
{
	UNREFERENCED_PARAMETER(type);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::get_Left(long *left)
{
	UNREFERENCED_PARAMETER(left);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::put_Left(long left)
{
	UNREFERENCED_PARAMETER(left);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::get_Top(long *top)
{
	UNREFERENCED_PARAMETER(top);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::put_Top(long top)
{
	UNREFERENCED_PARAMETER(top);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::get_Width(long *width)
{
	UNREFERENCED_PARAMETER(width);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::put_Width(long width)
{
	UNREFERENCED_PARAMETER(width);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::get_Height(long *height)
{
	UNREFERENCED_PARAMETER(height);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::put_Height(long height)
{
	UNREFERENCED_PARAMETER(height);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::get_LocationName(BSTR *locationName)
{
	UNREFERENCED_PARAMETER(locationName);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::get_LocationURL(BSTR *locationURL)
{
	UNREFERENCED_PARAMETER(locationURL);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::get_Busy(VARIANT_BOOL *busy)
{
	UNREFERENCED_PARAMETER(busy);

	return E_NOTIMPL;
}

// IDispatch
IFACEMETHODIMP WebBrowserApp::GetTypeInfoCount(UINT *typeInfoCount)
{
	UNREFERENCED_PARAMETER(typeInfoCount);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::GetTypeInfo(UINT type, LCID localeId, ITypeInfo **typeInfo)
{
	UNREFERENCED_PARAMETER(type);
	UNREFERENCED_PARAMETER(localeId);
	UNREFERENCED_PARAMETER(typeInfo);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::GetIDsOfNames(REFIID riid, LPOLESTR *names, UINT numNames, LCID localeId, DISPID *dispId)
{
	UNREFERENCED_PARAMETER(riid);
	UNREFERENCED_PARAMETER(names);
	UNREFERENCED_PARAMETER(numNames);
	UNREFERENCED_PARAMETER(localeId);
	UNREFERENCED_PARAMETER(dispId);

	return E_NOTIMPL;
}

IFACEMETHODIMP WebBrowserApp::Invoke(DISPID dispIdMember, REFIID riid, LCID localeId, WORD flags, DISPPARAMS *dispParams, VARIANT *varResult, EXCEPINFO *exceptionInfo, UINT *argErr)
{
	UNREFERENCED_PARAMETER(dispIdMember);
	UNREFERENCED_PARAMETER(riid);
	UNREFERENCED_PARAMETER(localeId);
	UNREFERENCED_PARAMETER(flags);
	UNREFERENCED_PARAMETER(dispParams);
	UNREFERENCED_PARAMETER(varResult);
	UNREFERENCED_PARAMETER(exceptionInfo);
	UNREFERENCED_PARAMETER(argErr);

	return E_NOTIMPL;
}

namespace winrt
{
	template <>

	bool is_guid_of<IWebBrowserApp>(guid const &id) noexcept
	{
		auto res = is_guid_of<IWebBrowserApp, IWebBrowser, IDispatch>(id);

		//OLECHAR* guidString;
		//(void)StringFromCLSID(id, &guidString);
		//std::wcout << L"QueryInterface: " << guidString << L" = " << res << std::endl;
		//CoTaskMemFree(guidString);

		return res;
	}
}
