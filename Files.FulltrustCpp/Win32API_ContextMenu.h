#pragma once
#pragma comment (lib, "Shell32.lib")
#pragma comment (lib, "Shlwapi.lib")

#include <shlobj.h>
#include <shellapi.h>
#include <shlwapi.h>
#include <windows.h>

#define SCRATCH_QCM_FIRST 1
#define SCRATCH_QCM_LAST  0x7FFF

class Win32API_ContextMenu
{
private:
    HRESULT GetUIObjectOfFile(HWND hwnd, LPCWSTR pszPath, REFIID riid, void** ppv);

public:
    IContextMenu2* g_pcm2;
    IContextMenu3* g_pcm3;
    void ShowContextMenuForFile(LPCWSTR filePath, UINT menuFlags, HINSTANCE hInstance);
};
