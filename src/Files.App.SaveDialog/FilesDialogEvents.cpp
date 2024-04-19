#include "pch.h"
#include "FilesDialogEvents.h"
#include <iostream>

using std::cout;
using std::wcout;
using std::endl;

FilesDialogEvents::FilesDialogEvents(IFileDialogEvents* evt, IFileDialog* cust)
{
	_evt = evt;
	_cust = cust;
}

HRESULT __stdcall FilesDialogEvents::QueryInterface(REFIID riid, void** ppvObject)
{
	HRESULT res = _evt->QueryInterface(riid, ppvObject);
	OLECHAR* guidString;
	(void)StringFromCLSID(riid, &guidString);
	std::wcout << L"Event: QueryInterface: " << guidString << L" = " << res << std::endl;
	::CoTaskMemFree(guidString);
	return res;
}

ULONG __stdcall FilesDialogEvents::AddRef(void)
{
	cout << "Event: AddRef" << endl;
	return _evt->AddRef();
}

ULONG __stdcall FilesDialogEvents::Release(void)
{
	cout << "Event: Release" << endl;
	return _evt->Release();
}

HRESULT __stdcall FilesDialogEvents::OnFileOk(IFileDialog* pfd)
{
	cout << "Event: PRE OnFileOk" << endl;
	HRESULT res = _evt->OnFileOk(_cust);
	cout << "Event: OnFileOk = " << res << endl;
	return res;
}

HRESULT __stdcall FilesDialogEvents::OnFolderChanging(IFileDialog* pfd, IShellItem* psiFolder)
{
	HRESULT res = _evt->OnFolderChanging(_cust, psiFolder);
	cout << "Event: OnFolderChanging = " << res << endl;
	return res;
}

HRESULT __stdcall FilesDialogEvents::OnFolderChange(IFileDialog* pfd)
{
	HRESULT res = _evt->OnFolderChange(_cust);
	cout << "Event: OnFolderChange = " << res << endl;
	return res;
}

HRESULT __stdcall FilesDialogEvents::OnSelectionChange(IFileDialog* pfd)
{
	HRESULT res = _evt->OnSelectionChange(_cust);
	cout << "Event: OnSelectionChange = " << res << endl;
	return res;
}

HRESULT __stdcall FilesDialogEvents::OnShareViolation(IFileDialog* pfd, IShellItem* psi, FDE_SHAREVIOLATION_RESPONSE* pResponse)
{
	cout << "Event: OnShareViolation" << endl;
	return E_NOTIMPL;
}

HRESULT __stdcall FilesDialogEvents::OnTypeChange(IFileDialog* pfd)
{
	HRESULT res = _evt->OnTypeChange(_cust);
	cout << "Event: OnTypeChange = " << res << endl;
	return res;
}

HRESULT __stdcall FilesDialogEvents::OnOverwrite(IFileDialog* pfd, IShellItem* psi, FDE_OVERWRITE_RESPONSE* pResponse)
{
	HRESULT res = _evt->OnOverwrite(_cust, psi, pResponse);
	cout << "Event: OnOverwrite = " << res << ", " << *pResponse << endl;
	return res;
}
