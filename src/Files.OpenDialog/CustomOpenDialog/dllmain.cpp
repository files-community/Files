// dllmain.cpp: implementazione di DllMain.

#include "pch.h"
#include "framework.h"
#include "resource.h"
#include "CustomOpenDialog_i.h"
#include "dllmain.h"

CCustomOpenDialogModule _AtlModule;

// Punto di ingresso DLL
extern "C" BOOL WINAPI DllMain(HINSTANCE hInstance, DWORD dwReason, LPVOID lpReserved)
{
	hInstance;
	return _AtlModule.DllMain(dwReason, lpReserved);
}
