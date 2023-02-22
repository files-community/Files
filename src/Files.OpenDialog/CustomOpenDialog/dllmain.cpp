// Copyright (c) 2023 Files Community
// Licensed under the MIT license.

// Abstract:
// - DllMain implementation

#include "pch.h"
#include "framework.h"
#include "resource.h"
#include "CustomOpenDialog_i.h"
#include "dllmain.h"

CCustomOpenDialogModule _AtlModule;

// DLL entry point
extern "C" BOOL WINAPI DllMain(HINSTANCE hInstance, DWORD dwReason, LPVOID lpReserved)
{
	hInstance;
	return _AtlModule.DllMain(dwReason, lpReserved);
}
