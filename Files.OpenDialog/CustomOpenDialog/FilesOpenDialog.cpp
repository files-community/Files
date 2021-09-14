// FilesOpenDialog.cpp: implementazione di CFilesOpenDialog

#include "pch.h"
#include "FilesOpenDialog.h"
#include <shlobj.h>
#include <iostream>
#include <fstream>
#include <cstdio>
#include <locale>
#include <codecvt>

//#define SYTEMDLG

using std::cout;
using std::wcout;
using std::endl;

// CFilesOpenDialog

CComPtr<IFileOpenDialog> GetSystemDialog()
{
	HINSTANCE lib = CoLoadLibrary(L"C:\\Windows\\System32\\comdlg32.dll", false);
	BOOL(WINAPI * dllGetClassObject)(REFCLSID, REFIID, LPVOID*) =
		(BOOL(WINAPI*)(REFCLSID, REFIID, LPVOID*))GetProcAddress(lib, "DllGetClassObject");
	CComPtr<IClassFactory> pClassFactory;
	dllGetClassObject(CLSID_FileOpenDialog, IID_IClassFactory, (void**)&pClassFactory);
	CComPtr<IFileOpenDialog> systemDialog;
	pClassFactory->CreateInstance(NULL, IID_IFileOpenDialog, (void**)&systemDialog);
	CoFreeLibrary(lib);
	return systemDialog;
}

CFilesOpenDialog::CFilesOpenDialog()
{
	_fos = FOS_FILEMUSTEXIST | FOS_PATHMUSTEXIST;
	_systemDialog = nullptr;

	PWSTR pszPath = NULL;
	HRESULT hr = SHGetKnownFolderPath(FOLDERID_Desktop, 0, NULL, &pszPath);
	if (SUCCEEDED(hr))
	{
		FILE* stream;
		TCHAR debugPath[MAX_PATH];
		wsprintf(debugPath, L"%s\\%s", pszPath, L"open_dialog.txt");
		_wfreopen_s(&stream, debugPath, L"w", stdout);
		CoTaskMemFree(pszPath);
	}
	cout << "Create" << endl;

	TCHAR tempPath[MAX_PATH];
	GetTempPath(MAX_PATH, tempPath);
	TCHAR tempName[MAX_PATH];
	GetTempFileName(tempPath, L"fsd", 0, tempName);
	_outputPath = tempName;

	(void)SHGetKnownFolderItem(FOLDERID_Documents, KF_FLAG_DEFAULT_PATH, NULL, IID_IShellItem, (void**)&_initFolder);
	hr = _initFolder->GetDisplayName(SIGDN_NORMALDISPLAY, &pszPath);
	if (SUCCEEDED(hr))
	{
		wcout << L"_outputPath: " << _outputPath << L", _initFolder: " << pszPath << endl;
	}

#ifdef  SYTEMDLG
	_systemDialog = GetSystemDialog();
#endif
}

void CFilesOpenDialog::FinalRelease()
{
	if (_systemDialog)
	{
		_systemDialog.Release();
	}
	if (_initFolder)
	{
		_initFolder->Release();
	}
}

HRESULT __stdcall CFilesOpenDialog::Show(HWND hwndOwner)
{
#ifdef  SYTEMDLG
	return _systemDialog->Show(hwndOwner);
#endif
	cout << "Show, hwndOwner: " << hwndOwner << endl;

	SHELLEXECUTEINFO ShExecInfo = { 0 };
	ShExecInfo.cbSize = sizeof(SHELLEXECUTEINFO);
	ShExecInfo.fMask = SEE_MASK_NOCLOSEPROCESS;
	ShExecInfo.lpFile = L"files.exe";
	PWSTR pszPath = NULL;
	if (SUCCEEDED(_initFolder->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING, &pszPath)))
	{
		TCHAR args[1024];
		wsprintf(args, L"-directory %s -outputpath %s", pszPath, _outputPath.c_str());
		wcout << L"Invoking: " << args << endl;
		ShExecInfo.lpParameters = args;
		CoTaskMemFree(pszPath);
	}
	ShExecInfo.nShow = SW_SHOW;
	ShellExecuteEx(&ShExecInfo);
	if (ShExecInfo.hProcess)
	{
		WaitForSingleObject(ShExecInfo.hProcess, INFINITE);
		CloseHandle(ShExecInfo.hProcess);
	}

	if (hwndOwner)
	{
		SetForegroundWindow(hwndOwner);
	}

	std::ifstream file(_outputPath);
	if (file.good())
	{
		std::string str;
		std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>> converter;
		while (std::getline(file, str))
		{
			std::wstring wide = converter.from_bytes(str);
			_selectedItems.push_back(wide);
		}
	}
	DeleteFile(_outputPath.c_str());

	if (!_selectedItems.empty())
	{
		//dialogEvents?.OnFileOk(this);
	}
	return !_selectedItems.empty() ? S_OK : HRESULT_FROM_WIN32(ERROR_CANCELLED);
}

HRESULT __stdcall CFilesOpenDialog::SetFileTypes(UINT cFileTypes, const COMDLG_FILTERSPEC* rgFilterSpec)
{
#ifdef SYSTEMDIALOG
	return systemDialog->SetFileTypes(cFileTypes, rgFilterSpec);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesOpenDialog::SetFileTypeIndex(UINT iFileType)
{
#ifdef SYSTEMDIALOG
	return systemDialog->SetFileTypeIndex(iFileType);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesOpenDialog::GetFileTypeIndex(UINT* piFileType)
{
#ifdef SYSTEMDIALOG
	return systemDialog->GetFileTypeIndex(piFileType);
#endif
	* piFileType = 1;
	return S_OK;
}

HRESULT __stdcall CFilesOpenDialog::Advise(IFileDialogEvents* pfde, DWORD* pdwCookie)
{
#ifdef SYSTEMDIALOG
	return systemDialog->Advise(pfde, pdwCookie);
#endif
	* pdwCookie = 0;
	return S_OK;
}

HRESULT __stdcall CFilesOpenDialog::Unadvise(DWORD dwCookie)
{
#ifdef SYSTEMDIALOG
	return systemDialog->Unadvise(dwCookie);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesOpenDialog::SetOptions(FILEOPENDIALOGOPTIONS fos)
{
#ifdef SYSTEMDIALOG
	return systemDialog->SetOptions(fos);
#endif
	_fos = fos;
	return S_OK;
}

HRESULT __stdcall CFilesOpenDialog::GetOptions(FILEOPENDIALOGOPTIONS* pfos)
{
#ifdef SYSTEMDIALOG
	return systemDialog->GetOptions(fos);
#endif
	* pfos = _fos;
	return S_OK;
}

HRESULT __stdcall CFilesOpenDialog::SetDefaultFolder(IShellItem* psi)
{
#ifdef SYSTEMDIALOG
	return systemDialog->SetDefaultFolder(psi);
#endif
	_initFolder = psi;
	return S_OK;
}

HRESULT __stdcall CFilesOpenDialog::SetFolder(IShellItem* psi)
{
#ifdef SYSTEMDIALOG
	return systemDialog->SetFolder(psi);
#endif
	_initFolder = psi;
	return S_OK;
}

HRESULT __stdcall CFilesOpenDialog::GetFolder(IShellItem** ppsi)
{
#ifdef SYSTEMDIALOG
	return systemDialog->GetFolder(ppsi);
#endif
	* ppsi = NULL;
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::GetCurrentSelection(IShellItem** ppsi)
{
#ifdef SYSTEMDIALOG
	return systemDialog->GetCurrentSelection(ppsi);
#endif
	return GetResult(ppsi);
}

HRESULT __stdcall CFilesOpenDialog::SetFileName(LPCWSTR pszName)
{
#ifdef SYSTEMDIALOG
	return systemDialog->SetFileName(pszName);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesOpenDialog::GetFileName(LPWSTR* pszName)
{
#ifdef SYSTEMDIALOG
	return systemDialog->GetFileName(pszName);
#endif
	SHStrDupW(L"", pszName);
	if (!_selectedItems.empty())
	{
		SHStrDupW(_selectedItems[0].c_str(), pszName);
	}
	return S_OK;
}

HRESULT __stdcall CFilesOpenDialog::SetTitle(LPCWSTR pszTitle)
{
#ifdef SYSTEMDIALOG
	return systemDialog->SetTitle(pszTitle);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesOpenDialog::SetOkButtonLabel(LPCWSTR pszText)
{
#ifdef SYSTEMDIALOG
	return systemDialog->SetOkButtonLabel(pszText);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesOpenDialog::SetFileNameLabel(LPCWSTR pszLabel)
{
#ifdef SYSTEMDIALOG
	return systemDialog->SetFileNameLabel(pszLabel);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesOpenDialog::GetResult(IShellItem** ppsi)
{
#if SYSTEMDIALOG
	return systemDialog->GetResult(ppsi);
#endif
	if (!_selectedItems.empty())
	{
		SHCreateItemFromParsingName(_selectedItems[0].c_str(), NULL, IID_IShellItem, (void**)ppsi);
		return S_OK;
	}
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::AddPlace(IShellItem* psi, FDAP fdap)
{
#if SYSTEMDIALOG
	return systemDialog->AddPlace(psi, fdap);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesOpenDialog::SetDefaultExtension(LPCWSTR pszDefaultExtension)
{
#if SYSTEMDIALOG
	return systemDialog->SetDefaultExtension(pszDefaultExtension);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesOpenDialog::Close(HRESULT hr)
{
#if SYSTEMDIALOG
	return systemDialog->Close(hr);
#endif
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::SetClientGuid(REFGUID guid)
{
#if SYSTEMDIALOG
	return systemDialog->SetClientGuid(guid);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesOpenDialog::ClearClientData(void)
{
#if SYSTEMDIALOG
	return systemDialog->ClearClientData();
#endif
	return S_OK;
}

HRESULT __stdcall CFilesOpenDialog::SetFilter(IShellItemFilter* pFilter)
{
#if SYSTEMDIALOG
	return systemDialog->SetFilter(pFilter);
#endif
	return S_OK;
}

HRESULT __stdcall CFilesOpenDialog::GetResults(IShellItemArray** ppenum)
{
#if SYSTEMDIALOG
	return systemDialog->GetResults(ppenum);
#endif
	if (!_selectedItems.empty())
	{
		std::vector<PIDLIST_ABSOLUTE> pidls;
		IShellItem* psi;
		PIDLIST_ABSOLUTE pidl;
		for (std::wstring ipath : _selectedItems)
		{
			if (SUCCEEDED(SHCreateItemFromParsingName(ipath.c_str(), NULL, IID_IShellItem, (void**)&psi)))
			{
				if (SUCCEEDED(SHGetIDListFromObject(psi, &pidl)))
				{
					pidls.push_back(ILClone(pidl));
				}
				psi->Release();
			}
		}

		HRESULT hr = SHCreateShellItemArrayFromIDLists((UINT)pidls.size(), (LPCITEMIDLIST*)&pidls[0], ppenum);
		for (PIDLIST_ABSOLUTE item : pidls)
		{
			CoTaskMemFree(item);
		}
		return hr;
	}
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::GetSelectedItems(IShellItemArray** ppsai)
{
#if SYSTEMDIALOG
	return systemDialog->GetSelectedItems(ppsai);
#endif
	return GetResults(ppsai);
}

HRESULT __stdcall CFilesOpenDialog::EnableOpenDropDown(DWORD dwIDCtl)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::AddMenu(DWORD dwIDCtl, LPCWSTR pszLabel)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::AddPushButton(DWORD dwIDCtl, LPCWSTR pszLabel)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::AddComboBox(DWORD dwIDCtl)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::AddRadioButtonList(DWORD dwIDCtl)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::AddCheckButton(DWORD dwIDCtl, LPCWSTR pszLabel, BOOL bChecked)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::AddEditBox(DWORD dwIDCtl, LPCWSTR pszText)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::AddSeparator(DWORD dwIDCtl)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::AddText(DWORD dwIDCtl, LPCWSTR pszText)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::SetControlLabel(DWORD dwIDCtl, LPCWSTR pszLabel)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::GetControlState(DWORD dwIDCtl, CDCONTROLSTATEF* pdwState)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::SetControlState(DWORD dwIDCtl, CDCONTROLSTATEF dwState)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::GetEditBoxText(DWORD dwIDCtl, WCHAR** ppszText)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::SetEditBoxText(DWORD dwIDCtl, LPCWSTR pszText)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::GetCheckButtonState(DWORD dwIDCtl, BOOL* pbChecked)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::SetCheckButtonState(DWORD dwIDCtl, BOOL bChecked)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::AddControlItem(DWORD dwIDCtl, DWORD dwIDItem, LPCWSTR pszLabel)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::RemoveControlItem(DWORD dwIDCtl, DWORD dwIDItem)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::RemoveAllControlItems(DWORD dwIDCtl)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::GetControlItemState(DWORD dwIDCtl, DWORD dwIDItem, CDCONTROLSTATEF* pdwState)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::SetControlItemState(DWORD dwIDCtl, DWORD dwIDItem, CDCONTROLSTATEF dwState)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::GetSelectedControlItem(DWORD dwIDCtl, DWORD* pdwIDItem)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::SetSelectedControlItem(DWORD dwIDCtl, DWORD dwIDItem)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::StartVisualGroup(DWORD dwIDCtl, LPCWSTR pszLabel)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::EndVisualGroup(void)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::MakeProminent(DWORD dwIDCtl)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::SetControlItemText(DWORD dwIDCtl, DWORD dwIDItem, LPCWSTR pszLabel)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::SetCancelButtonLabel(LPCWSTR pszLabel)
{
	return E_NOTIMPL;
}

HRESULT __stdcall CFilesOpenDialog::SetNavigationRoot(IShellItem* psi)
{
	return E_NOTIMPL;
}
