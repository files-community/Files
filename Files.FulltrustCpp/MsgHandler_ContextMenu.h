#pragma once
#include "MessageHandler.h"
#include "AppServiceManager.h"

#define SCRATCH_QCM_FIRST 1
#define SCRATCH_QCM_LAST  0x7FFF

using json = nlohmann::json;

struct MenuArgs
{
    bool ExtendedMenu;
    bool ShowOpenMenu;
    std::vector<std::wstring> FileList;
};

struct Win32ContextMenuItem
{
    std::string IconBase64;
    int ID;
    std::string Label;
    std::string CommandString;
    UINT Type;
    std::vector<Win32ContextMenuItem> SubItems;

    NLOHMANN_DEFINE_TYPE_INTRUSIVE(Win32ContextMenuItem, IconBase64, ID, Label, CommandString, Type, SubItems)
};

struct Win32ContextMenu
{    
    IContextMenu2* g_pcm2 = NULL;
    IContextMenu3* g_pcm3 = NULL;
    IContextMenu* cMenu = NULL;
    HMENU hMenu;

    std::vector<Win32ContextMenuItem> Items;

    ~Win32ContextMenu()
    {
        if (hMenu)
        {
            DestroyMenu(hMenu);
        }
        if (cMenu)
        {
            cMenu->Release();
        }
        if (this->g_pcm2)
        {
            g_pcm2->Release();
            g_pcm2 = NULL;
        }
        if (this->g_pcm3)
        {
            g_pcm3->Release();
            g_pcm3 = NULL;
        }
    }

    NLOHMANN_DEFINE_TYPE_INTRUSIVE(Win32ContextMenu, Items)
};

class MsgHandler_ContextMenu : public MessageHandler
{
private:
    HRESULT GetUIObjectOfFile(HWND hwnd, std::vector<std::wstring> const& fileList, REFIID riid, void** ppv);

    HWND hiddenWindow;
    std::thread windowThread;

    void CreateHiddenWindow(HINSTANCE hInstance);

    void EnumMenuItems(IContextMenu* cMenu, HMENU hMenu, std::vector<Win32ContextMenuItem>& menuItemsResult, MenuArgs* menuArgs);

    static std::vector<std::string> FilteredItems;

public:
    Win32ContextMenu* LoadedContextMenu = NULL;

    void LoadContextMenuForFile(MenuArgs* args);

    IAsyncOperation<bool> ParseArgumentsAsync(AppServiceManager const& manager, AppServiceRequestReceivedEventArgs const& args);

    void InvokeCommand(int menuId);
    void InvokeCommand(std::string menuVerb);

    MsgHandler_ContextMenu(HINSTANCE hInstance);
    ~MsgHandler_ContextMenu();
};
