// Copyright (c) 2023 Files
// Licensed under the MIT License. See the LICENSE.

#pragma once

#include "wil/cppwinrt.h"
#include <winrt/base.h>
#include <memory>
#include <windows.h>
#include <ShObjIdl_core.h>

class OpenInFolder;

// This isn't a complete implementation. There's only enough functionality to support the "New Folder"
// item shown on the background context menu in a phone's virtual folder (when that phone is connected via USB).
class ShellView : public winrt::implements<ShellView, IShellView>
{
public:
	ShellView(OpenInFolder *pThis, PCIDLIST_ABSOLUTE directory);

	// IShellView
	IFACEMETHODIMP TranslateAccelerator(MSG *msg);
	IFACEMETHODIMP EnableModeless(BOOL enable);
	IFACEMETHODIMP UIActivate(UINT state);
	IFACEMETHODIMP Refresh();
	IFACEMETHODIMP CreateViewWindow(IShellView *previous, LPCFOLDERSETTINGS folderSettings, IShellBrowser *shellBrowser, RECT *view, HWND *hwnd);
	IFACEMETHODIMP DestroyViewWindow();
	IFACEMETHODIMP GetCurrentInfo(LPFOLDERSETTINGS folderSettings);
	IFACEMETHODIMP AddPropertySheetPages(DWORD reserved, LPFNSVADDPROPSHEETPAGE callback, LPARAM lParam);
	IFACEMETHODIMP SaveViewState();
	IFACEMETHODIMP SelectItem(PCUITEMID_CHILD pidlItem, SVSIF flags);
	IFACEMETHODIMP GetItemObject(UINT item, REFIID riid, void **ppv);

	// IOleWindow
	IFACEMETHODIMP GetWindow(HWND *hwnd);
	IFACEMETHODIMP ContextSensitiveHelp(BOOL enterMode);

private:
	PCIDLIST_ABSOLUTE m_directory;
	OpenInFolder* m_pThis;
};

namespace winrt
{
	template <>
	bool is_guid_of<IShellView>(guid const &id) noexcept;
}
