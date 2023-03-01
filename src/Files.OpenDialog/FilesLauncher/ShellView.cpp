// Copyright (c) 2023 Files
// Licensed under the MIT License. See the LICENSE.

#include "ShellView.h"
#include <ShlObj_core.h>
#include <wil/resource.h>
#include <iostream>

#include "OpenInFolder.h"

using unique_pidl_absolute = wil::unique_cotaskmem_ptr<std::remove_pointer_t<PIDLIST_ABSOLUTE>>;
using unique_pidl_relative = wil::unique_cotaskmem_ptr<std::remove_pointer_t<PIDLIST_RELATIVE>>;
using unique_pidl_child = wil::unique_cotaskmem_ptr<std::remove_pointer_t<PITEMID_CHILD>>;

ShellView::ShellView(OpenInFolder* pThis, PCIDLIST_ABSOLUTE directory) : m_directory(directory), m_pThis(pThis)
{
}

// IShellView
IFACEMETHODIMP ShellView::TranslateAccelerator(MSG* msg)
{
	UNREFERENCED_PARAMETER(msg);

	return E_NOTIMPL;
}

IFACEMETHODIMP ShellView::EnableModeless(BOOL enable)
{
	UNREFERENCED_PARAMETER(enable);

	return E_NOTIMPL;
}

IFACEMETHODIMP ShellView::UIActivate(UINT state)
{
	UNREFERENCED_PARAMETER(state);

	return E_NOTIMPL;
}

IFACEMETHODIMP ShellView::Refresh()
{
	return E_NOTIMPL;
}

IFACEMETHODIMP ShellView::CreateViewWindow(IShellView* previous, LPCFOLDERSETTINGS folderSettings, IShellBrowser* shellBrowser, RECT* view, HWND* hwnd)
{
	UNREFERENCED_PARAMETER(previous);
	UNREFERENCED_PARAMETER(folderSettings);
	UNREFERENCED_PARAMETER(shellBrowser);
	UNREFERENCED_PARAMETER(view);
	UNREFERENCED_PARAMETER(hwnd);

	return E_NOTIMPL;
}

IFACEMETHODIMP ShellView::DestroyViewWindow()
{
	return E_NOTIMPL;
}

IFACEMETHODIMP ShellView::GetCurrentInfo(LPFOLDERSETTINGS folderSettings)
{
	UNREFERENCED_PARAMETER(folderSettings);

	return E_NOTIMPL;
}

IFACEMETHODIMP ShellView::AddPropertySheetPages(DWORD reserved, LPFNSVADDPROPSHEETPAGE callback, LPARAM lParam)
{
	UNREFERENCED_PARAMETER(reserved);
	UNREFERENCED_PARAMETER(callback);
	UNREFERENCED_PARAMETER(lParam);

	return E_NOTIMPL;
}

IFACEMETHODIMP ShellView::SaveViewState()
{
	return E_NOTIMPL;
}

IFACEMETHODIMP ShellView::SelectItem(PCUITEMID_CHILD pidlItem, SVSIF flags)
{
	if (flags == SVSI_EDIT)
	{
		auto pidlComplete = unique_pidl_absolute(ILCombine(m_directory, pidlItem));

		return S_OK;
	}
	else if (WI_IsFlagSet(flags, SVSI_SELECT))
	{
		auto pidlComplete = unique_pidl_absolute(ILCombine(m_directory, pidlItem));

		if (m_pThis)
			m_pThis->OnItemSelected(pidlComplete.get());

		return S_OK;
	}

	return E_NOTIMPL;
}

IFACEMETHODIMP ShellView::GetItemObject(UINT item, REFIID riid, void** ppv)
{
	UNREFERENCED_PARAMETER(item);
	UNREFERENCED_PARAMETER(riid);
	UNREFERENCED_PARAMETER(ppv);

	return E_NOTIMPL;
}

// IOleWindow
IFACEMETHODIMP ShellView::GetWindow(HWND* hwnd)
{
	UNREFERENCED_PARAMETER(hwnd);

	return E_NOTIMPL;
}

IFACEMETHODIMP ShellView::ContextSensitiveHelp(BOOL enterMode)
{
	UNREFERENCED_PARAMETER(enterMode);

	return E_NOTIMPL;
}

namespace winrt
{
	template <>

	bool is_guid_of<IShellView>(guid const& id) noexcept
	{
		return is_guid_of<IShellView, IOleWindow>(id);
	}
}
