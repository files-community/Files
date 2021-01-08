#pragma once
#define STRICT_TYPED_ITEMIDS
#define _SILENCE_CXX17_CODECVT_HEADER_DEPRECATION_WARNING

#pragma comment (lib, "Shell32.lib")
#pragma comment (lib, "Shlwapi.lib")
#pragma comment (lib, "Gdi32.lib")
#pragma comment (lib, "Gdiplus.lib")
#pragma comment (lib, "Kernel32.lib")
#pragma comment (lib, "Propsys.lib")

#include <shlobj.h>
#include <shellapi.h>
#include <shlwapi.h>
#include <windows.h>
#include <gdiplus.h>
#include "CommonControls.h"

#include <nlohmann/json.hpp>

#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.Storage.h>
#include <winrt/Windows.ApplicationModel.h>
#include <winrt/Windows.ApplicationModel.AppService.h>

#define DEBUGPRINTF(...) { wchar_t dbgstr[1024]; swprintf(dbgstr, __VA_ARGS__); OutputDebugString(dbgstr); }
