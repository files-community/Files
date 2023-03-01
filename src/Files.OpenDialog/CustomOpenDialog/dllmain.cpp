// Copyright (c) 2023 Files
// Licensed under the MIT License. See the LICENSE.

// Abstract:
//  DllMain implementation.

#include "pch.h"
#include "framework.h"
#include "resource.h"
#include "CustomOpenDialog_i.h"
#include "dllmain.h"

CCustomOpenDialogModule _AtlModule;

// DLL entry point
EXTERN_C BOOL WINAPI DllMain(HINSTANCE hInstance, DWORD dwReason, LPVOID lpReserved)
{
	return _AtlModule.DllMain(dwReason, lpReserved);
}
