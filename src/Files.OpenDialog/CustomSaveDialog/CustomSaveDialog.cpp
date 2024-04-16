// CustomSaveDialog.cpp: implementazione delle esportazioni DLL.


#include "pch.h"
#include "framework.h"
#include "resource.h"
#include "CustomSaveDialog_i.h"
#include "dllmain.h"


using namespace ATL;

// Utilizzato per determinare se la DLL può essere scaricata da OLE.
_Use_decl_annotations_
STDAPI DllCanUnloadNow(void)
{
	return _AtlModule.DllCanUnloadNow();
}

// Restituisce una class factory per creare un oggetto del tipo richiesto.
_Use_decl_annotations_
STDAPI DllGetClassObject(_In_ REFCLSID rclsid, _In_ REFIID riid, _Outptr_ LPVOID* ppv)
{
	return _AtlModule.DllGetClassObject(rclsid, riid, ppv);
}

// DllRegisterServer: aggiunge voci al Registro di sistema.
_Use_decl_annotations_
STDAPI DllRegisterServer(void)
{
	// registra gli oggetti, le librerie dei tipi e tutte le interfacce della libreria dei tipi
	HRESULT hr = _AtlModule.DllRegisterServer();
	return hr;
}

// DllUnregisterServer: rimuove voci dal Registro di sistema.
_Use_decl_annotations_
STDAPI DllUnregisterServer(void)
{
	HRESULT hr = _AtlModule.DllUnregisterServer();
	return hr;
}

// DllInstall: aggiunge/rimuove voci nel Registro di sistema per ogni utente di ciascun computer.
STDAPI DllInstall(BOOL bInstall, _In_opt_  LPCWSTR pszCmdLine)
{
	HRESULT hr = E_FAIL;
	static const wchar_t szUserSwitch[] = L"user";

	if (pszCmdLine != nullptr)
	{
		if (_wcsnicmp(pszCmdLine, szUserSwitch, _countof(szUserSwitch)) == 0)
		{
			ATL::AtlSetPerUserRegistration(true);
		}
	}

	if (bInstall)
	{
		hr = DllRegisterServer();
		if (FAILED(hr))
		{
			DllUnregisterServer();
		}
	}
	else
	{
		hr = DllUnregisterServer();
	}

	return hr;
}


