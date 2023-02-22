// Copyright (c) 2023 Files Community
// Licensed under the MIT license.

// Abstract:
// - Declaration of CFilesOpenDialog

#pragma once
#include "resource.h"       // Main symbols


//#define DEBUGLOG


#include "CustomOpenDialog_i.h"
#include "UndefInterfaces.h"
#include <iostream>
#include <string>
#include <vector>


#if defined(_WIN32_WCE) && !defined(_CE_DCOM) && !defined(_CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA)
#error "Single-threaded COM objects are not supported properly on the Windows CE platform, for example Windows Mobile platforms do not include full DCOM support. Define _CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA to make ATL support the creation of single-threaded COM objects and allow implementations with single-threaded COM objects. The threading model in the RGS file has been set to 'Free' as it is the only threading model supported on non-DCOM Windows CE platforms."
#endif

using namespace ATL;


// CFilesOpenDialog

class ATL_NO_VTABLE CFilesOpenDialog :
	public CComObjectRootEx<CComSingleThreadModel>,
	public CComCoClass<CFilesOpenDialog, &CLSID_FilesOpenDialog>,
	public IFileDialog,
	public IFileDialog2,
	public IFileOpenDialog,
	public IFileDialogCustomize,
	public IObjectWithSite,
	public IFileDialogPrivate
{
public:
	CFilesOpenDialog();

DECLARE_REGISTRY_RESOURCEID(106)

CUSTOM_BEGIN_COM_MAP(CFilesOpenDialog)
	COM_INTERFACE_ENTRY(IFileDialog)
	COM_INTERFACE_ENTRY(IFileDialog2)
	COM_INTERFACE_ENTRY(IFileOpenDialog)
	COM_INTERFACE_ENTRY(IFileDialogCustomize)
	COM_INTERFACE_ENTRY(IObjectWithSite)
	COM_INTERFACE_ENTRY(IFileDialogPrivate)
END_COM_MAP()

	DECLARE_PROTECT_FINAL_CONSTRUCT()

	HRESULT FinalConstruct()
	{
		return S_OK;
	}

	void FinalRelease();

	CComPtr<IFileOpenDialog> _systemDialog;

	FILEOPENDIALOGOPTIONS _fos;

	std::vector<std::wstring> _selectedItems;
	std::wstring _outputPath;
	IShellItem* _initFolder;
	IFileDialogEvents* _dialogEvents;

	FILE* _debugStream;

public:
	// Inherited through IFileOpenDialog
	HRESULT __stdcall Show(HWND hwndOwner) override;

	HRESULT __stdcall SetFileTypes(UINT cFileTypes, const COMDLG_FILTERSPEC* rgFilterSpec) override;

	HRESULT __stdcall SetFileTypeIndex(UINT iFileType) override;

	HRESULT __stdcall GetFileTypeIndex(UINT* piFileType) override;

	HRESULT __stdcall Advise(IFileDialogEvents* pfde, DWORD* pdwCookie) override;

	HRESULT __stdcall Unadvise(DWORD dwCookie) override;

	HRESULT __stdcall SetOptions(FILEOPENDIALOGOPTIONS fos) override;

	HRESULT __stdcall GetOptions(FILEOPENDIALOGOPTIONS* pfos) override;

	HRESULT __stdcall SetDefaultFolder(IShellItem* psi) override;

	HRESULT __stdcall SetFolder(IShellItem* psi) override;

	HRESULT __stdcall GetFolder(IShellItem** ppsi) override;

	HRESULT __stdcall GetCurrentSelection(IShellItem** ppsi) override;

	HRESULT __stdcall SetFileName(LPCWSTR pszName) override;

	HRESULT __stdcall GetFileName(LPWSTR* pszName) override;

	HRESULT __stdcall SetTitle(LPCWSTR pszTitle) override;

	HRESULT __stdcall SetOkButtonLabel(LPCWSTR pszText) override;

	HRESULT __stdcall SetFileNameLabel(LPCWSTR pszLabel) override;

	HRESULT __stdcall GetResult(IShellItem** ppsi) override;

	HRESULT __stdcall AddPlace(IShellItem* psi, FDAP fdap) override;

	HRESULT __stdcall SetDefaultExtension(LPCWSTR pszDefaultExtension) override;

	HRESULT __stdcall Close(HRESULT hr) override;

	HRESULT __stdcall SetClientGuid(REFGUID guid) override;

	HRESULT __stdcall ClearClientData(void) override;

	HRESULT __stdcall SetFilter(IShellItemFilter* pFilter) override;

	HRESULT __stdcall GetResults(IShellItemArray** ppenum) override;

	HRESULT __stdcall GetSelectedItems(IShellItemArray** ppsai) override;


	// Inherited through IFileDialogCustomize
	HRESULT __stdcall EnableOpenDropDown(DWORD dwIDCtl) override;

	HRESULT __stdcall AddMenu(DWORD dwIDCtl, LPCWSTR pszLabel) override;

	HRESULT __stdcall AddPushButton(DWORD dwIDCtl, LPCWSTR pszLabel) override;

	HRESULT __stdcall AddComboBox(DWORD dwIDCtl) override;

	HRESULT __stdcall AddRadioButtonList(DWORD dwIDCtl) override;

	HRESULT __stdcall AddCheckButton(DWORD dwIDCtl, LPCWSTR pszLabel, BOOL bChecked) override;

	HRESULT __stdcall AddEditBox(DWORD dwIDCtl, LPCWSTR pszText) override;

	HRESULT __stdcall AddSeparator(DWORD dwIDCtl) override;

	HRESULT __stdcall AddText(DWORD dwIDCtl, LPCWSTR pszText) override;

	HRESULT __stdcall SetControlLabel(DWORD dwIDCtl, LPCWSTR pszLabel) override;

	HRESULT __stdcall GetControlState(DWORD dwIDCtl, CDCONTROLSTATEF* pdwState) override;

	HRESULT __stdcall SetControlState(DWORD dwIDCtl, CDCONTROLSTATEF dwState) override;

	HRESULT __stdcall GetEditBoxText(DWORD dwIDCtl, WCHAR** ppszText) override;

	HRESULT __stdcall SetEditBoxText(DWORD dwIDCtl, LPCWSTR pszText) override;

	HRESULT __stdcall GetCheckButtonState(DWORD dwIDCtl, BOOL* pbChecked) override;

	HRESULT __stdcall SetCheckButtonState(DWORD dwIDCtl, BOOL bChecked) override;

	HRESULT __stdcall AddControlItem(DWORD dwIDCtl, DWORD dwIDItem, LPCWSTR pszLabel) override;

	HRESULT __stdcall RemoveControlItem(DWORD dwIDCtl, DWORD dwIDItem) override;

	HRESULT __stdcall RemoveAllControlItems(DWORD dwIDCtl) override;

	HRESULT __stdcall GetControlItemState(DWORD dwIDCtl, DWORD dwIDItem, CDCONTROLSTATEF* pdwState) override;

	HRESULT __stdcall SetControlItemState(DWORD dwIDCtl, DWORD dwIDItem, CDCONTROLSTATEF dwState) override;

	HRESULT __stdcall GetSelectedControlItem(DWORD dwIDCtl, DWORD* pdwIDItem) override;

	HRESULT __stdcall SetSelectedControlItem(DWORD dwIDCtl, DWORD dwIDItem) override;

	HRESULT __stdcall StartVisualGroup(DWORD dwIDCtl, LPCWSTR pszLabel) override;

	HRESULT __stdcall EndVisualGroup(void) override;

	HRESULT __stdcall MakeProminent(DWORD dwIDCtl) override;

	HRESULT __stdcall SetControlItemText(DWORD dwIDCtl, DWORD dwIDItem, LPCWSTR pszLabel) override;


	// Inherited through IFileDialog2
	HRESULT __stdcall SetCancelButtonLabel(LPCWSTR pszLabel) override;

	HRESULT __stdcall SetNavigationRoot(IShellItem* psi) override;

	// Inherited through IObjectWithSite
	HRESULT __stdcall SetSite(IUnknown* pUnkSite) override;
	HRESULT __stdcall GetSite(REFIID riid, void** ppvSite) override;

	// Inherited through IFileDialogPrivate
	HRESULT __stdcall HideControlsForHostedPickerProviderApp(void) override;
	HRESULT __stdcall EnableControlsForHostedPickerProviderApp(void) override;
	HRESULT __stdcall GetPrivateOptions(unsigned long*) override;
	HRESULT __stdcall SetPrivateOptions(unsigned long) override;
	HRESULT __stdcall SetPersistenceKey(unsigned short const*) override;
	HRESULT __stdcall HasPlaces(void) override;
	HRESULT __stdcall EnumPlaces(int, _GUID const&, void**) override;
	HRESULT __stdcall EnumControls(void**) override;
	HRESULT __stdcall GetPersistRegkey(unsigned short**) override;
	HRESULT __stdcall GetSavePropertyStore(IPropertyStore**, IPropertyDescriptionList**) override;
	HRESULT __stdcall GetSaveExtension(unsigned short**) override;
	HRESULT __stdcall GetFileTypeControl(void**) override;
	HRESULT __stdcall GetFileNameControl(void**) override;
	HRESULT __stdcall GetFileProtectionControl(void**) override;
	HRESULT __stdcall SetFolderPrivate(IShellItem*, int) override;
	HRESULT __stdcall SetCustomControlAreaHeight(unsigned int) override;
	HRESULT __stdcall GetDialogState(unsigned long, unsigned long*) override;
	HRESULT __stdcall SetAppControlsModule(void*) override;
	HRESULT __stdcall SetUserEditedSaveProperties(void) override;
	HRESULT __stdcall ShouldShowStandardNavigationRoots(void) override;
	HRESULT __stdcall GetNavigationRoot(_GUID const&, void**) override;
	HRESULT __stdcall ShouldShowFileProtectionControl(int*) override;
	HRESULT __stdcall GetCurrentDialogView(_GUID const&, void**) override;
	HRESULT __stdcall SetSaveDialogEditBoxTextAndFileType(int, unsigned short const*) override;
	HRESULT __stdcall MoveFocusFromBrowser(int) override;
	HRESULT __stdcall EnableOkButton(int) override;
	HRESULT __stdcall InitEnterpriseId(unsigned short const*) override;
	HRESULT __stdcall AdviseFirst(IFileDialogEvents*, unsigned long*) override;
	HRESULT __stdcall HandleTab(void) override;
};

OBJECT_ENTRY_AUTO(__uuidof(FilesOpenDialog), CFilesOpenDialog)
