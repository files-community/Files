#pragma once
#include "MessageHandler.h"

#define SCRATCH_QCM_FIRST 1
#define SCRATCH_QCM_LAST  0x7FFF

struct MenuArgs
{
    bool ExtendedMenu;
    bool ShowOpenMenu;
    std::vector<std::wstring> FileList;
};

class MsgHandler_ContextMenu : public MessageHandler
{
private:
    HRESULT GetUIObjectOfFile(HWND hwnd, LPCWSTR pszPath, REFIID riid, void** ppv);

    HWND hiddenWindow;
    std::thread windowThread;

    void CreateHiddenWindow(HINSTANCE hInstance);
    int clickedItem;

public:
    IContextMenu2* g_pcm2 = NULL;
    IContextMenu3* g_pcm3 = NULL;
    void ShowContextMenuForFile(MenuArgs* args);

    IAsyncOperation<bool> ParseArgumentsAsync(AppServiceManager const& manager, AppServiceRequestReceivedEventArgs const& args);

    MsgHandler_ContextMenu(HINSTANCE hInstance);
    ~MsgHandler_ContextMenu();
};
