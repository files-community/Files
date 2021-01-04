#pragma once
#define STRICT_TYPED_ITEMIDS

#pragma comment (lib, "Shell32.lib")
#pragma comment (lib, "Shlwapi.lib")

#include <shlobj.h>
#include <shellapi.h>
#include <shlwapi.h>
#include <windows.h>

#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.Storage.h>
#include <winrt/Windows.ApplicationModel.h>
#include <winrt/Windows.ApplicationModel.AppService.h>

#define DEBUGPRINTF(...) { wchar_t dbgstr[1024]; swprintf(dbgstr, __VA_ARGS__); OutputDebugString(dbgstr); }
