#pragma once
#include "MessageHandler.h"

#define SCRATCH_QCM_FIRST 1
#define SCRATCH_QCM_LAST  0x7FFF

class MsgHandler_ContextMenu : public MessageHandler
{
private:
    HRESULT GetUIObjectOfFile(HWND hwnd, LPCWSTR pszPath, REFIID riid, void** ppv);

public:
    IContextMenu2* g_pcm2 = NULL;
    IContextMenu3* g_pcm3 = NULL;
    void ShowContextMenuForFile(LPCWSTR filePath, UINT menuFlags, HINSTANCE hInstance);

    IAsyncOperation<bool> ParseArgumentsAsync(AppServiceManager const& manager, AppServiceRequestReceivedEventArgs const& args);
};
