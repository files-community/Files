// FilesSaveDialog.cpp: implementazione di CFilesSaveDialog

#include "pch.h"
#include "FilesSaveDialog.h"
#include <shlobj.h>
#include <iostream>
#include <fstream>
#include <cstdio>
#include <locale>
#include <codecvt>

//#define SYSTEMDIALOG

using std::cout;
using std::wcout;
using std::endl;

// CFilesSaveDialog

CComPtr<IFileSaveDialog> GetSystemDialog()
{
	HINSTANCE lib = CoLoadLibrary(L"C:\\Windows\\System32\\comdlg32.dll", false);
	BOOL(WINAPI * dllGetClassObject)(REFCLSID, REFIID, LPVOID*) =
		(BOOL(WINAPI*)(REFCLSID, REFIID, LPVOID*))GetProcAddress(lib, "DllGetClassObject");
	CComPtr<IClassFactory> pClassFactory;
	dllGetClassObject(CLSID_FileSaveDialog, IID_IClassFactory, (void**)&pClassFactory);
	CComPtr<IFileSaveDialog> systemDialog;
	pClassFactory->CreateInstance(NULL, IID_IFileSaveDialog, (void**)&systemDialog);
	//CoFreeLibrary(lib);
	return systemDialog;
}

IShellItem* CloneShellItem(IShellItem* psi)
{
	IShellItem* item = NULL;
	if (psi)
	{
		PIDLIST_ABSOLUTE pidl;
		if (SUCCEEDED(SHGetIDListFromObject(psi, &pidl)))
		{
			SHCreateItemFromIDList(pidl, IID_IShellItem, (void**)&item);
			CoTaskMemFree(pidl);
		}
	}
	return item;
}

template <typename T>
CComPtr<T> AsInterface(CComPtr<IFileSaveDialog> dialog)
{
	CComPtr<T> dialogInterface;
	dialog->QueryInterface<T>(&dialogInterface);
	return dialogInterface;
}

CFilesSaveDialog::CFilesSaveDialog()
{
}

void CFilesSaveDialog::FinalRelease()
{
	if (_systemDialog)
	{
		_systemDialog.Release();
	}
	if (_debugStream)
	{
		fclose(_debugStream);
	}
}

HRESULT __stdcall CFilesSaveDialog::SetSite(IUnknown* pUnkSite)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetSite(REFIID riid, void** ppvSite)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::EnableOpenDropDown(DWORD dwIDCtl)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::AddMenu(DWORD dwIDCtl, LPCWSTR pszLabel)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::AddPushButton(DWORD dwIDCtl, LPCWSTR pszLabel)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::AddComboBox(DWORD dwIDCtl)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::AddRadioButtonList(DWORD dwIDCtl)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::AddCheckButton(DWORD dwIDCtl, LPCWSTR pszLabel, BOOL bChecked)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::AddEditBox(DWORD dwIDCtl, LPCWSTR pszText)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::AddSeparator(DWORD dwIDCtl)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::AddText(DWORD dwIDCtl, LPCWSTR pszText)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetControlLabel(DWORD dwIDCtl, LPCWSTR pszLabel)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetControlState(DWORD dwIDCtl, CDCONTROLSTATEF* pdwState)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetControlState(DWORD dwIDCtl, CDCONTROLSTATEF dwState)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetEditBoxText(DWORD dwIDCtl, WCHAR** ppszText)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetEditBoxText(DWORD dwIDCtl, LPCWSTR pszText)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetCheckButtonState(DWORD dwIDCtl, BOOL* pbChecked)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetCheckButtonState(DWORD dwIDCtl, BOOL bChecked)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::AddControlItem(DWORD dwIDCtl, DWORD dwIDItem, LPCWSTR pszLabel)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::RemoveControlItem(DWORD dwIDCtl, DWORD dwIDItem)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::RemoveAllControlItems(DWORD dwIDCtl)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetControlItemState(DWORD dwIDCtl, DWORD dwIDItem, CDCONTROLSTATEF* pdwState)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetControlItemState(DWORD dwIDCtl, DWORD dwIDItem, CDCONTROLSTATEF dwState)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetSelectedControlItem(DWORD dwIDCtl, DWORD* pdwIDItem)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetSelectedControlItem(DWORD dwIDCtl, DWORD dwIDItem)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::StartVisualGroup(DWORD dwIDCtl, LPCWSTR pszLabel)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::EndVisualGroup(void)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::MakeProminent(DWORD dwIDCtl)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetControlItemText(DWORD dwIDCtl, DWORD dwIDItem, LPCWSTR pszLabel)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::Show(HWND hwndOwner)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetFileTypes(UINT cFileTypes, const COMDLG_FILTERSPEC* rgFilterSpec)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetFileTypeIndex(UINT iFileType)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetFileTypeIndex(UINT* piFileType)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::Advise(IFileDialogEvents* pfde, DWORD* pdwCookie)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::Unadvise(DWORD dwCookie)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetOptions(FILEOPENDIALOGOPTIONS fos)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetOptions(FILEOPENDIALOGOPTIONS* pfos)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetDefaultFolder(IShellItem* psi)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetFolder(IShellItem* psi)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetFolder(IShellItem** ppsi)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetCurrentSelection(IShellItem** ppsi)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetFileName(LPCWSTR pszName)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetFileName(LPWSTR* pszName)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetTitle(LPCWSTR pszTitle)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetOkButtonLabel(LPCWSTR pszText)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetFileNameLabel(LPCWSTR pszLabel)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetResult(IShellItem** ppsi)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::AddPlace(IShellItem* psi, FDAP fdap)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetDefaultExtension(LPCWSTR pszDefaultExtension)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::Close(HRESULT hr)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetClientGuid(REFGUID guid)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::ClearClientData(void)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetFilter(IShellItemFilter* pFilter)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetCancelButtonLabel(LPCWSTR pszLabel)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetNavigationRoot(IShellItem* psi)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::HideControlsForHostedPickerProviderApp(void)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::EnableControlsForHostedPickerProviderApp(void)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetPrivateOptions(unsigned long*)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetPrivateOptions(unsigned long)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetPersistenceKey(unsigned short const*)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::HasPlaces(void)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::EnumPlaces(int, _GUID const&, void**)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::EnumControls(void**)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetPersistRegkey(unsigned short**)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetSavePropertyStore(IPropertyStore**, IPropertyDescriptionList**)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetSaveExtension(unsigned short**)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetFileTypeControl(void**)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetFileNameControl(void**)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetFileProtectionControl(void**)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetFolderPrivate(IShellItem*, int)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetCustomControlAreaHeight(unsigned int)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetDialogState(unsigned long, unsigned long*)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetAppControlsModule(void*)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetUserEditedSaveProperties(void)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::ShouldShowStandardNavigationRoots(void)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetNavigationRoot(_GUID const&, void**)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::ShouldShowFileProtectionControl(int*)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetCurrentDialogView(_GUID const&, void**)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetSaveDialogEditBoxTextAndFileType(int, unsigned short const*)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::MoveFocusFromBrowser(int)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::EnableOkButton(int)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::InitEnterpriseId(unsigned short const*)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::AdviseFirst(IFileDialogEvents*, unsigned long*)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::HandleTab(void)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetSaveAsItem(IShellItem* psi)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetProperties(IPropertyStore* pStore)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::SetCollectedProperties(IPropertyDescriptionList* pList, BOOL fAppendDefault)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::GetProperties(IPropertyStore** ppStore)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesSaveDialog::ApplyProperties(IShellItem* psi, IPropertyStore* pStore, HWND hwnd, IFileOperationProgressSink* pSink)
{
	return E_NOTIMPL;
}
