#pragma once
#include "resource.h"       // simboli principali

#include "oaidl.h"
#include "ocidl.h"
#include "shobjidl.h"

class FilesDialogEvents : public IFileDialogEvents
{
	IFileDialogEvents* _evt;
	IFileDialog* _cust;

public:
	FilesDialogEvents(IFileDialogEvents* evt, IFileDialog *cust);

	// Ereditato tramite IFileDialogEvents
	HRESULT __stdcall QueryInterface(REFIID riid, void** ppvObject) override;
	ULONG __stdcall AddRef(void) override;
	ULONG __stdcall Release(void) override;
	HRESULT __stdcall OnFileOk(IFileDialog* pfd) override;
	HRESULT __stdcall OnFolderChanging(IFileDialog* pfd, IShellItem* psiFolder) override;
	HRESULT __stdcall OnFolderChange(IFileDialog* pfd) override;
	HRESULT __stdcall OnSelectionChange(IFileDialog* pfd) override;
	HRESULT __stdcall OnShareViolation(IFileDialog* pfd, IShellItem* psi, FDE_SHAREVIOLATION_RESPONSE* pResponse) override;
	HRESULT __stdcall OnTypeChange(IFileDialog* pfd) override;
	HRESULT __stdcall OnOverwrite(IFileDialog* pfd, IShellItem* psi, FDE_OVERWRITE_RESPONSE* pResponse) override;
};
