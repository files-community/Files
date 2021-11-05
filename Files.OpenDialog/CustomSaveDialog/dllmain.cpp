// dllmain.cpp: implementazione di DllMain.

#include "pch.h"
#include "framework.h"
#include "resource.h"
#include "CustomSaveDialog_i.h"
#include "dllmain.h"

CCustomSaveDialogModule _AtlModule;

// Punto di ingresso DLL
extern "C" BOOL WINAPI DllMain(HINSTANCE hInstance, DWORD dwReason, LPVOID lpReserved)
{
	hInstance;
	return _AtlModule.DllMain(dwReason, lpReserved);
}
