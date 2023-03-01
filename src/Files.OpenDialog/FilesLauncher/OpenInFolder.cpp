// Copyright (c) 2023 Files
// Licensed under the MIT License. See the LICENSE.

#include "OpenInFolder.h"

OpenInFolder::OpenInFolder() : m_hwnd(NULL)
{
	m_shellWindows = winrt::create_instance<IShellWindows>(CLSID_ShellWindows, CLSCTX_ALL);
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

	IShellItem* psi;
	PIDLIST_ABSOLUTE pidlDirectory = NULL;

	if (!SUCCEEDED(SHCreateItemFromParsingName(openDirectory, NULL, IID_IShellItem, (void**)&psi)))
	{
		return;
	}
	if (!SUCCEEDED(SHGetIDListFromObject(psi, &pidlDirectory)))
	{
		psi->Release();
		return;
	}

	psi->Release();

	if (!SUCCEEDED(NotifyShellOfNavigation(pidlDirectory)))
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
	RETURN_IF_FAILED(m_shellWindows->RegisterPending(GetCurrentThreadId(), &pidlVariant, &empty, SWC_BROWSER, &m_shellWindowCookie));

//	auto document = winrt::make_self<DocumentServiceProvider>();
//	auto shellView = winrt::make_self<ShellView>(this, pidl);
//	document->RegisterService(IID_IFolderView, shellView.get());
//	auto browserApp = winrt::make_self<WebBrowserApp>(m_hwnd, document.get());
//	long registeredCookie;
//
//// Supress:
////  - 'reinterpret_cast': pointer truncation from 'HWND' to 'long',
////  - 'reinterpret_cast': truncation from 'HWND' to 'long'
//#pragma warning(push)
//#pragma warning(disable : 4311 4302)
//	RETURN_IF_FAILED(m_shellWindows->Register(browserApp.get(), reinterpret_cast<long>(m_hwnd), SWC_BROWSER, &registeredCookie));
//#pragma warning(pop)

	m_shellWindows->OnNavigate(m_shellWindowCookie, &pidlVariant);
	//m_shellWindows->OnActivated(m_shellWindowCookie, VARIANT_TRUE);

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

OpenInFolder::~OpenInFolder()
{
	if (m_shellWindows)
		m_shellWindows->Revoke(m_shellWindowCookie);
}
